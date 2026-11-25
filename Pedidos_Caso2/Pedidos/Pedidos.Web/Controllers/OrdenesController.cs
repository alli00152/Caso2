using AutoMapper;
using Microsoft.AspNet.Identity;
using Pedidos.Application.Comun;
using Pedidos.Application.Contractos;
using Pedidos.Application.Requests;
using Pedidos.Domain.Enums;
using Pedidos.Web.Extensions;
using Pedidos.Web.Filters;
using Pedidos.Web.Models;
using Pedidos.Web.ViewModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Pedidos.Web.Controllers
{
    /// <summary>
    /// Todas las acciones exigen autenticación. El controlador solo coordina: no accede a DbContext
    /// ni contiene reglas de negocio (viven en Application/Domain).
    /// </summary>
    [Authorize]
    [RegistrarAuditoria]
    public class OrdenesController : BaseController
    {
        private readonly IOrdenService _service;
        private readonly IMapper _mapper;

        public OrdenesController(IOrdenService service, IMapper mapper)
        {
            _service = service;
            _mapper = mapper;
        }

        // ---------------------------------------------------------------- Listado y detalle

        [HttpGet]
        public ActionResult Index()
        {
            // El servicio ya devuelve solo las órdenes propias si el usuario es Cliente.
            var ordenes = _service.ObtenerListado();
            return View(_mapper.Map<List<OrdenListadoItemViewModel>>(ordenes));
        }

        [HttpGet]
        [RequiereOrdenPropia]
        public ActionResult Detalle(Guid? id)
        {
            if (id == null || id == Guid.Empty)
            {
                return RespuestaError.Vista(Response, 400, "Solicitud no válida", "El identificador de la orden es obligatorio.");
            }

            var resultado = _service.ObtenerPorId(id.Value);
            if (!resultado.EsExitoso)
            {
                return ErrorDe(resultado, id);
            }

            return View(_mapper.Map<OrdenDetalleViewModel>(resultado.Valor));
        }

        // ---------------------------------------------------------------- Crear

        [HttpGet]
        public ActionResult Crear()
        {
            return View(new CrearOrdenFormModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Crear(CrearOrdenFormModel model)
        {
            model = model ?? new CrearOrdenFormModel();

            if (!ModelState.IsValid)
            {
                model.AsegurarFilaInicial();
                return VistaInvalida(model);
            }

            var resultado = _service.Crear(_mapper.Map<CrearOrdenRequest>(model));

            if (!resultado.EsExitoso)
            {
                if (resultado.TipoError == TipoError.Validacion)
                {
                    // Errores de FluentValidation junto a cada campo; misma vista, sin redirect.
                    resultado.AgregarAModelState(ModelState);
                    model.AsegurarFilaInicial();
                    return VistaInvalida(model);
                }

                return ErrorDe(resultado);
            }

            // PRG: POST -> Redirect -> GET; el mensaje viaja en TempData.
            TempData["Exito"] = "La orden se registró correctamente.";
            return RedirectToAction("Detalle", new { id = resultado.Valor });
        }

        // ---------------------------------------------------------------- Cambios de estado (solo POST)

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdministradorOOperador)]
        [RequiereOrdenPropia]
        public ActionResult Confirmar(CambiarEstadoFormModel model)
        {
            return CambiarEstado(model, EstadoOrden.Confirmada, "La orden fue confirmada.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdministradorOOperador)]
        [RequiereOrdenPropia]
        public ActionResult IniciarPreparacion(CambiarEstadoFormModel model)
        {
            return CambiarEstado(model, EstadoOrden.EnPreparacion, "La orden está en preparación.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdministradorOOperador)]
        [RequiereOrdenPropia]
        public ActionResult ConfirmarEntrega(CambiarEstadoFormModel model)
        {
            return CambiarEstado(model, EstadoOrden.Entregada, "La orden se marcó como entregada.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.Administrador)]
        [RequiereOrdenPropia]
        public ActionResult Cancelar(CambiarEstadoFormModel model)
        {
            return CambiarEstado(model, EstadoOrden.Cancelada, "La orden fue cancelada.");
        }

        // ---------------------------------------------------------------- Enviar (rastreo)

        [HttpGet]
        [Authorize(Roles = Roles.AdministradorOOperador)]
        [RequiereOrdenPropia]
        public ActionResult Enviar(Guid? id)
        {
            if (id == null || id == Guid.Empty)
            {
                return RespuestaError.Vista(Response, 400, "Solicitud no válida", "El identificador de la orden es obligatorio.");
            }

            var resultado = _service.ObtenerPorId(id.Value);
            if (!resultado.EsExitoso)
            {
                return ErrorDe(resultado, id);
            }

            return View(_mapper.Map<EnviarOrdenFormModel>(resultado.Valor));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.AdministradorOOperador)]
        [RequiereOrdenPropia]
        public ActionResult Enviar(EnviarOrdenFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return VistaInvalida(model);
            }

            var resultado = _service.Enviar(_mapper.Map<EnviarOrdenRequest>(model));

            if (!resultado.EsExitoso)
            {
                if (resultado.TipoError == TipoError.Validacion)
                {
                    resultado.AgregarAModelState(ModelState);
                    return VistaInvalida(model);
                }

                return ErrorDe(resultado, model.OrdenId);
            }

            RegistrarCambioDeEstado(model.OrdenId, EstadoOrden.Enviada);
            TempData["Exito"] = "La orden fue enviada y el número de rastreo quedó registrado.";
            return RedirectToAction("Detalle", new { id = model.OrdenId });
        }

        // ---------------------------------------------------------------- Rastreo

        [HttpGet]
        public ActionResult Rastreo(string criterio)
        {
            var modelo = new RastreoViewModel { Criterio = criterio };

            if (string.IsNullOrWhiteSpace(criterio))
            {
                return View(modelo);
            }

            var resultado = _service.ConsultarRastreo(criterio);
            if (!resultado.EsExitoso)
            {
                // Se queda en la misma pantalla con el mensaje y el código HTTP correspondiente.
                Response.StatusCode = RespuestaError.CodigoHttp(resultado.TipoError);
                Response.TrySkipIisCustomErrors = true;
                ModelState.AddModelError("criterio", resultado.Mensaje);
                return View(modelo);
            }

            var encontrado = _mapper.Map<RastreoViewModel>(resultado.Valor);
            encontrado.Criterio = criterio;
            return View(encontrado);
        }

        // ---------------------------------------------------------------- Auxiliares

        // Evento de auditoría del cambio de estado: solo identificadores, estado y fecha UTC
        // (nunca la dirección, el número de rastreo ni otros datos personales).
        private void RegistrarCambioDeEstado(Guid ordenId, EstadoOrden estadoNuevo)
        {
            Log.Information(
                "CAMBIO_ESTADO OrdenId={OrdenId} EstadoNuevo={EstadoNuevo} Usuario={UsuarioId} FechaUtc={FechaUtc:o}",
                ordenId, estadoNuevo, User.Identity.GetUserId(), DateTime.UtcNow);
        }

        private ActionResult CambiarEstado(CambiarEstadoFormModel model, EstadoOrden destino, string mensajeExito)
        {
            if (model == null || model.OrdenId == Guid.Empty)
            {
                return RespuestaError.Vista(Response, 400, "Solicitud no válida", "El identificador de la orden es obligatorio.");
            }

            var solicitud = _mapper.Map<CambiarEstadoOrdenRequest>(model);
            solicitud.EstadoSolicitado = destino;

            var resultado = _service.CambiarEstado(solicitud);
            if (!resultado.EsExitoso)
            {
                // 409 para transición o concurrencia en conflicto; 403/404/400 según el caso.
                return ErrorDe(resultado, model.OrdenId);
            }

            RegistrarCambioDeEstado(model.OrdenId, destino);
            TempData["Exito"] = mensajeExito;
            return RedirectToAction("Detalle", new { id = model.OrdenId });
        }
    }
}
