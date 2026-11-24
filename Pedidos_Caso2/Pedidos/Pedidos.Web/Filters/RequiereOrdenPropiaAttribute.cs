using Pedidos.Application.Comun;
using Pedidos.Application.Contractos;
using Pedidos.Web.Extensions;
using Serilog;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Pedidos.Web.Filters
{
    /// <summary>
    /// Filtro de autorización para rutas que reciben el identificador de una orden: antes de ejecutar
    /// la acción (cargar la vista o cambiar la orden) pregunta al servicio de Application si el usuario
    /// autenticado es el dueño de la orden (o personal interno). Responde 404 si no existe y 403 si no
    /// le pertenece. No reemplaza a [Authorize] ni a las reglas del servicio: es una capa adicional.
    /// </summary>
    public class RequiereOrdenPropiaAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            var http = filterContext.HttpContext;

            if (http.User == null || !http.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new HttpUnauthorizedResult();
                return;
            }

            var solicitud = http.Request;
            var candidatos = new[]
            {
                Convert.ToString(filterContext.RouteData.Values["id"]),
                Convert.ToString(filterContext.RouteData.Values["OrdenId"]),
                solicitud.Form["OrdenId"],
                solicitud.Form["id"],
                solicitud.QueryString["id"]
            }
            .Where(valor => !string.IsNullOrWhiteSpace(valor))
            .Distinct()
            .ToList();

            if (candidatos.Count == 0)
            {
                filterContext.Result = RespuestaError.Vista(http.Response, 400, "Solicitud no válida",
                    "Falta el identificador de la orden.");
                return;
            }

            var servicio = DependencyResolver.Current.GetService<IOrdenService>();

            foreach (var candidato in candidatos)
            {
                Guid ordenId;
                if (!Guid.TryParse(candidato, out ordenId))
                {
                    filterContext.Result = RespuestaError.Vista(http.Response, 400, "Solicitud no válida",
                        "El identificador de la orden no es válido.");
                    return;
                }

                var acceso = servicio.VerificarAcceso(ordenId);
                if (!acceso.EsExitoso)
                {
                    if (acceso.TipoError == TipoError.Prohibido)
                    {
                        // Intento de acceso a una orden ajena: queda registrado (sin datos personales).
                        Log.Warning(
                            "ACCESO_DENEGADO OrdenId={OrdenId} Controlador={Controlador} Accion={Accion} FechaUtc={FechaUtc:o}",
                            ordenId,
                            filterContext.RouteData.Values["controller"],
                            filterContext.RouteData.Values["action"],
                            DateTime.UtcNow);
                    }

                    filterContext.Result = RespuestaError.DesdeResultado(http.Response, acceso, ordenId);
                    return;
                }
            }
        }
    }
}
