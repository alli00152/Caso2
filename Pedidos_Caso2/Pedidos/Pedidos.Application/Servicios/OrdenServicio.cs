using AutoMapper;
using FluentValidation;
using FluentValidation.Results;
using Pedidos.Application.Comun;
using Pedidos.Application.Contractos;
using Pedidos.Application.Requests;
using Pedidos.Domain.Entities;
using Pedidos.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pedidos.Application.Servicios
{
    /// <summary>
    /// Casos de uso de órdenes. Aquí se aplican la autorización por rol y por propietario
    /// (en el servidor, no en la vista), las reglas de transición y el manejo de concurrencia.
    /// </summary>
    public class OrdenServicio : IOrdenService
    {
        public const string MensajeConcurrencia =
            "La orden fue modificada por otra persona. Vuelva a cargarla para ver los cambios más recientes.";

        private readonly IOrdenRepository _ordenRepository;
        private readonly IUsuarioActual _usuario;
        private readonly IMapper _mapper;
        private readonly IValidator<CrearOrdenRequest> _validadorCrear;
        private readonly IValidator<EnviarOrdenRequest> _validadorEnviar;
        private readonly IValidator<CambiarEstadoOrdenRequest> _validadorCambioEstado;

        public OrdenServicio(
            IOrdenRepository ordenRepository,
            IUsuarioActual usuario,
            IMapper mapper,
            IValidator<CrearOrdenRequest> validadorCrear,
            IValidator<EnviarOrdenRequest> validadorEnviar,
            IValidator<CambiarEstadoOrdenRequest> validadorCambioEstado)
        {
            _ordenRepository = ordenRepository ?? throw new ArgumentNullException(nameof(ordenRepository));
            _usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _validadorCrear = validadorCrear ?? throw new ArgumentNullException(nameof(validadorCrear));
            _validadorEnviar = validadorEnviar ?? throw new ArgumentNullException(nameof(validadorEnviar));
            _validadorCambioEstado = validadorCambioEstado ?? throw new ArgumentNullException(nameof(validadorCambioEstado));
        }

        // ------------------------------------------------------------------ Crear

        public Resultado<Guid> Crear(CrearOrdenRequest request)
        {
            if (!_usuario.EstaAutenticado)
            {
                return Resultado<Guid>.Fallido(TipoError.Prohibido, "Debe iniciar sesión para registrar una orden.");
            }

            if (request == null)
            {
                return Resultado<Guid>.Fallido(TipoError.Validacion, "La solicitud es obligatoria.");
            }

            var validacion = _validadorCrear.Validate(request);
            if (!validacion.IsValid)
            {
                return Resultado<Guid>.FallidoValidacion(Convertir(validacion));
            }

            // El propietario siempre es el usuario autenticado: nunca viene del formulario.
            var orden = new Orden(request.ClienteNombre, request.DireccionEntrega, _usuario.Id);
            foreach (var detalle in request.Detalles)
            {
                orden.AgregarDetalle(detalle.Producto, detalle.Cantidad);
            }

            // Un solo SaveChanges: la orden y sus detalles se guardan en una única transacción.
            _ordenRepository.Agregar(orden);
            _ordenRepository.GuardarCambios();

            return Resultado<Guid>.Exitoso(orden.Id);
        }

        // ------------------------------------------------------------------ Consultas

        public Resultado<OrdenDto> ObtenerPorId(Guid ordenId)
        {
            if (!_usuario.EstaAutenticado)
            {
                return Resultado<OrdenDto>.Fallido(TipoError.Prohibido, "Debe iniciar sesión.");
            }

            var orden = _ordenRepository.ObtenerPorId(ordenId);
            if (orden == null)
            {
                return Resultado<OrdenDto>.Fallido(TipoError.NoEncontrado, "La orden no existe.");
            }

            if (!PuedeVer(orden))
            {
                return Resultado<OrdenDto>.Fallido(TipoError.Prohibido, "No tiene permiso para ver esta orden.");
            }

            return Resultado<OrdenDto>.Exitoso(Mapear(orden));
        }

        public Resultado VerificarAcceso(Guid ordenId)
        {
            if (!_usuario.EstaAutenticado)
            {
                return Resultado.Fallido(TipoError.Prohibido, "Debe iniciar sesión.");
            }

            var orden = _ordenRepository.ObtenerPorId(ordenId);
            if (orden == null)
            {
                return Resultado.Fallido(TipoError.NoEncontrado, "La orden no existe.");
            }

            return PuedeVer(orden)
                ? Resultado.Exitoso()
                : Resultado.Fallido(TipoError.Prohibido, "No tiene permiso para acceder a esta orden.");
        }

        public IReadOnlyCollection<OrdenDto> ObtenerListado()
        {
            if (!_usuario.EstaAutenticado)
            {
                return new List<OrdenDto>();
            }

            // El filtro por propietario se aplica en el repositorio (consulta SQL), no en la vista.
            var ordenes = EsPersonalInterno()
                ? _ordenRepository.ObtenerTodas()
                : _ordenRepository.ObtenerPorPropietario(_usuario.Id);

            return ordenes.Select(Mapear).ToList();
        }

        public Resultado<OrdenDto> ConsultarRastreo(string criterio)
        {
            if (!_usuario.EstaAutenticado)
            {
                return Resultado<OrdenDto>.Fallido(TipoError.Prohibido, "Debe iniciar sesión.");
            }

            if (string.IsNullOrWhiteSpace(criterio) || criterio.Trim().Length > 100)
            {
                return Resultado<OrdenDto>.Fallido(TipoError.Validacion,
                    "Indique un número de rastreo o el identificador de la orden (máximo 100 caracteres).");
            }

            var texto = criterio.Trim();
            Guid ordenId;
            var orden = Guid.TryParse(texto, out ordenId)
                ? _ordenRepository.ObtenerPorId(ordenId)
                : _ordenRepository.ObtenerPorRastreo(NormalizadorRastreo.Normalizar(texto));

            // Para el rastreo no se distingue "no existe" de "no es suya": así no se confirma
            // la existencia de números de rastreo de otras personas.
            if (orden == null || !PuedeVer(orden))
            {
                return Resultado<OrdenDto>.Fallido(TipoError.NoEncontrado,
                    "No se encontró una orden con ese número de rastreo o identificador.");
            }

            return Resultado<OrdenDto>.Exitoso(Mapear(orden));
        }

        // ------------------------------------------------------------------ Cambios de estado

        public Resultado CambiarEstado(CambiarEstadoOrdenRequest request)
        {
            if (request == null)
            {
                return Resultado.Fallido(TipoError.Validacion, "La solicitud es obligatoria.");
            }

            var validacion = _validadorCambioEstado.Validate(request);
            if (!validacion.IsValid)
            {
                return Resultado.FallidoValidacion(Convertir(validacion));
            }

            if (request.EstadoSolicitado == EstadoOrden.Enviada)
            {
                return Resultado.Fallido(TipoError.Validacion,
                    "El envío requiere un número de rastreo; use la operación de envío.");
            }

            var permiso = VerificarPermisoParaCambiarA(request.EstadoSolicitado);
            if (!permiso.EsExitoso)
            {
                return permiso;
            }

            var orden = _ordenRepository.ObtenerPorId(request.OrdenId);
            if (orden == null)
            {
                return Resultado.Fallido(TipoError.NoEncontrado, "La orden no existe.");
            }

            if (!VersionCoincide(orden, request.RowVersion))
            {
                return Resultado.Fallido(TipoError.Conflicto, MensajeConcurrencia);
            }

            // Regla de dominio: la transición debe ser válida desde el estado actual.
            if (!orden.PuedeTransicionarA(request.EstadoSolicitado))
            {
                return Resultado.Fallido(TipoError.Conflicto,
                    $"No es posible pasar la orden de {orden.Estado} a {request.EstadoSolicitado}.");
            }

            _ordenRepository.EstablecerVersionOriginal(orden, request.RowVersion);
            AplicarTransicion(orden, request.EstadoSolicitado);

            return Guardar();
        }

        public Resultado Enviar(EnviarOrdenRequest request)
        {
            if (request == null)
            {
                return Resultado.Fallido(TipoError.Validacion, "La solicitud es obligatoria.");
            }

            var validacion = _validadorEnviar.Validate(request);
            if (!validacion.IsValid)
            {
                return Resultado.FallidoValidacion(Convertir(validacion));
            }

            var permiso = VerificarPermisoParaCambiarA(EstadoOrden.Enviada);
            if (!permiso.EsExitoso)
            {
                return permiso;
            }

            var orden = _ordenRepository.ObtenerPorId(request.OrdenId);
            if (orden == null)
            {
                return Resultado.Fallido(TipoError.NoEncontrado, "La orden no existe.");
            }

            if (!VersionCoincide(orden, request.RowVersion))
            {
                return Resultado.Fallido(TipoError.Conflicto, MensajeConcurrencia);
            }

            if (!orden.PuedeTransicionarA(EstadoOrden.Enviada))
            {
                return Resultado.Fallido(TipoError.Conflicto,
                    $"No es posible enviar una orden en estado {orden.Estado}.");
            }

            var rastreo = NormalizadorRastreo.Normalizar(request.NumeroRastreo);
            if (_ordenRepository.ExisteRastreo(rastreo, orden.Id))
            {
                return Resultado.Fallido(TipoError.Conflicto,
                    "El número de rastreo ya está asignado a otra orden.");
            }

            _ordenRepository.EstablecerVersionOriginal(orden, request.RowVersion);
            orden.Enviar(rastreo);

            return Guardar();
        }

        // ------------------------------------------------------------------ Auxiliares

        private bool EsPersonalInterno()
        {
            return _usuario.EsAdministrador || _usuario.EsOperador;
        }

        private bool PuedeVer(Orden orden)
        {
            return EsPersonalInterno() || string.Equals(orden.ApplicationUserId, _usuario.Id, StringComparison.Ordinal);
        }

        /// <summary>Cancelar: solo Administrador. Confirmar/preparar/enviar/entregar: Administrador u Operador.</summary>
        private Resultado VerificarPermisoParaCambiarA(EstadoOrden destino)
        {
            if (!_usuario.EstaAutenticado)
            {
                return Resultado.Fallido(TipoError.Prohibido, "Debe iniciar sesión.");
            }

            var permitido = destino == EstadoOrden.Cancelada
                ? _usuario.EsAdministrador
                : EsPersonalInterno();

            return permitido
                ? Resultado.Exitoso()
                : Resultado.Fallido(TipoError.Prohibido, "No tiene permiso para realizar esta operación.");
        }

        /// <summary>
        /// Compara la versión con la que el usuario abrió la pantalla contra la actual.
        /// Una orden sin versión (aún no persistida) no tiene con qué compararse.
        /// </summary>
        private static bool VersionCoincide(Orden orden, byte[] versionRecibida)
        {
            return orden.RowVersion == null ||
                   (versionRecibida != null && orden.RowVersion.SequenceEqual(versionRecibida));
        }

        private static void AplicarTransicion(Orden orden, EstadoOrden destino)
        {
            switch (destino)
            {
                case EstadoOrden.Confirmada:
                    orden.Confirmar();
                    break;
                case EstadoOrden.EnPreparacion:
                    orden.IniciarPreparacion();
                    break;
                case EstadoOrden.Entregada:
                    orden.Entregar();
                    break;
                case EstadoOrden.Cancelada:
                    orden.Cancelar();
                    break;
                default:
                    throw new InvalidOperationException($"Transición no soportada a {destino}.");
            }
        }

        private Resultado Guardar()
        {
            try
            {
                _ordenRepository.GuardarCambios();
                return Resultado.Exitoso();
            }
            catch (ConcurrenciaException)
            {
                return Resultado.Fallido(TipoError.Conflicto, MensajeConcurrencia);
            }
            catch (ViolacionUnicidadException)
            {
                return Resultado.Fallido(TipoError.Conflicto, "El número de rastreo ya está asignado a otra orden.");
            }
        }

        private OrdenDto Mapear(Orden orden)
        {
            var dto = _mapper.Map<OrdenDto>(orden);
            dto.TransicionesPermitidas = orden.ObtenerTransicionesPermitidas()
                .Select(e => e.ToString())
                .ToList();
            return dto;
        }

        private static IEnumerable<ErrorValidacion> Convertir(ValidationResult resultado)
        {
            return resultado.Errors.Select(e => new ErrorValidacion(e.PropertyName, e.ErrorMessage));
        }
    }
}
