using Pedidos.Application.Comun;
using Pedidos.Web.Extensions;
using System;
using System.Web.Mvc;

namespace Pedidos.Web.Controllers
{
    public abstract class BaseController : Controller
    {
        /// <summary>Traduce un resultado previsible a la vista de error con su código HTTP (400/403/404/409).</summary>
        protected ActionResult ErrorDe(Resultado resultado, Guid? ordenId = null)
        {
            return RespuestaError.DesdeResultado(Response, resultado, ordenId);
        }

        /// <summary>
        /// POST inválido: se conserva el modelo y se vuelve a mostrar la misma vista (sin redirect,
        /// para no perder los errores) con código 400.
        /// </summary>
        protected ActionResult VistaInvalida(object modelo)
        {
            Response.StatusCode = 400;
            Response.TrySkipIisCustomErrors = true;
            return View(modelo);
        }
    }
}
