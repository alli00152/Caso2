using Microsoft.AspNet.Identity;
using Pedidos.Web.Extensions;
using Serilog;
using System;
using System.Web;
using System.Web.Mvc;

namespace Pedidos.Web.Filters
{
    /// <summary>
    /// Filtro de excepciones global. Registra la excepción (con un código de correlación) y devuelve
    /// la vista Error segura: nunca muestra el stack trace, rutas internas ni cadenas de conexión,
    /// tampoco en desarrollo. Los resultados previsibles (NotFound, Validation, Conflict) NO pasan por
    /// aquí: se traducen en el controlador.
    /// </summary>
    public class ManejarExcepcionAttribute : FilterAttribute, IExceptionFilter
    {
        public void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
            {
                return;
            }

            var http = filterContext.HttpContext;
            var excepcion = filterContext.Exception;
            var errorId = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            var usuarioId = http.User != null && http.User.Identity != null && http.User.Identity.IsAuthenticated
                ? http.User.Identity.GetUserId()
                : "anonimo";
            var controlador = filterContext.RouteData.Values["controller"];
            var accion = filterContext.RouteData.Values["action"];

            int codigo;
            string titulo;
            string mensaje;

            if (excepcion is HttpAntiForgeryException || excepcion is HttpRequestValidationException)
            {
                codigo = 400;
                titulo = "Solicitud no válida";
                mensaje = "La solicitud no es válida o la página expiró. Vuelva a cargar la página e intente de nuevo.";

                Log.Warning(
                    "SOLICITUD_RECHAZADA ErrorId={ErrorId} Tipo={Tipo} Usuario={UsuarioId} " +
                    "Controlador={Controlador} Accion={Accion} FechaUtc={FechaUtc:o}",
                    errorId, excepcion.GetType().Name, usuarioId, controlador, accion, DateTime.UtcNow);
            }
            else
            {
                codigo = 500;
                titulo = "Ocurrió un error inesperado";
                mensaje = "No fue posible completar la solicitud. Intente nuevamente; si el problema continúa, " +
                          "informe el código de error indicado.";

                // El detalle técnico solo va al log, nunca a la vista.
                Log.Error(excepcion,
                    "ERROR_NO_CONTROLADO ErrorId={ErrorId} Usuario={UsuarioId} " +
                    "Controlador={Controlador} Accion={Accion} FechaUtc={FechaUtc:o}",
                    errorId, usuarioId, controlador, accion, DateTime.UtcNow);
            }

            filterContext.ExceptionHandled = true;
            http.Response.Clear();
            filterContext.Result = RespuestaError.Vista(http.Response, codigo, titulo, mensaje, null, errorId);
        }
    }
}
