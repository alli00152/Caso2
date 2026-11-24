using Pedidos.Web.Extensions;
using System.Web.Mvc;

namespace Pedidos.Web.Controllers
{
    /// <summary>
    /// Destino de customErrors: mensajes seguros, sin stack traces ni cadenas de conexión.
    /// </summary>
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        // GET /Error
        public ActionResult Index()
        {
            return RespuestaError.Vista(Response, 500, "Ocurrió un error inesperado",
                "No fue posible completar la solicitud. Intente nuevamente más tarde.");
        }

        // GET /Error/NoEncontrado
        public ActionResult NoEncontrado()
        {
            return RespuestaError.Vista(Response, 404, "No encontrado",
                "La página o el registro solicitado no existe.");
        }

        // GET /Error/Prohibido
        public ActionResult Prohibido()
        {
            return RespuestaError.Prohibido(Response);
        }
    }
}
