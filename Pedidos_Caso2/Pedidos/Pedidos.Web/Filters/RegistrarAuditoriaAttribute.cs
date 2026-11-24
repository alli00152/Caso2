using Microsoft.AspNet.Identity;
using Serilog;
using System;
using System.Diagnostics;
using System.Web.Mvc;

namespace Pedidos.Web.Filters
{
    /// <summary>
    /// Registra una línea de auditoría por acción: usuario, controlador, acción, OrdenId (si aplica),
    /// resultado, duración y fecha UTC. NO registra contraseñas, tokens, cookies, cadenas de conexión
    /// ni datos personales (no se registran los valores de los formularios ni la dirección).
    /// </summary>
    public class RegistrarAuditoriaAttribute : ActionFilterAttribute
    {
        private const string ClaveCronometro = "Pedidos.Auditoria.Cronometro";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            filterContext.HttpContext.Items[ClaveCronometro] = Stopwatch.StartNew();
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            var http = filterContext.HttpContext;
            var cronometro = http.Items[ClaveCronometro] as Stopwatch;
            if (cronometro != null)
            {
                cronometro.Stop();
            }

            var usuarioId = http.User != null && http.User.Identity != null && http.User.Identity.IsAuthenticated
                ? http.User.Identity.GetUserId()
                : "anonimo";

            var fallo = filterContext.Exception != null ||
                        http.Response.StatusCode >= 400 ||
                        filterContext.Controller.TempData.ContainsKey("Error");

            Log.Information(
                "AUDITORIA Usuario={UsuarioId} Controlador={Controlador} Accion={Accion} OrdenId={OrdenId} " +
                "Resultado={Resultado} DuracionMs={DuracionMs} FechaUtc={FechaUtc:o}",
                usuarioId,
                filterContext.RouteData.Values["controller"],
                filterContext.RouteData.Values["action"],
                ObtenerOrdenId(filterContext),
                fallo ? "Fallido" : "Exitoso",
                cronometro != null ? cronometro.ElapsedMilliseconds : -1,
                DateTime.UtcNow);
        }

        private static Guid? ObtenerOrdenId(ActionExecutedContext contexto)
        {
            var solicitud = contexto.HttpContext.Request;
            var candidatos = new[]
            {
                Convert.ToString(contexto.RouteData.Values["id"]),
                solicitud.Form["OrdenId"],
                solicitud.QueryString["id"]
            };

            foreach (var candidato in candidatos)
            {
                Guid id;
                if (!string.IsNullOrWhiteSpace(candidato) && Guid.TryParse(candidato, out id))
                {
                    return id;
                }
            }

            return null;
        }
    }
}
