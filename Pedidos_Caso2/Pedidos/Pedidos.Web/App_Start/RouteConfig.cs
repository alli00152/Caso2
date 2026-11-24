using System.Web.Mvc;
using System.Web.Routing;

namespace Pedidos.Web
{
    public class RouteConfig
    {
        private const string PatronGuid = @"[0-9a-fA-F]{8}-([0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}";

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Consulta pública de rastreo: /Rastreo/{numero o Guid}. La restricción limita los caracteres permitidos.
            routes.MapRoute(
                name: "Rastreo",
                url: "Rastreo/{criterio}",
                defaults: new { controller = "Ordenes", action = "Rastreo", criterio = UrlParameter.Optional },
                constraints: new { criterio = @"[A-Za-z0-9\-]{0,100}" }
            );

            // Órdenes: el id es un Guid no consecutivo (nunca un entero secuencial).
            routes.MapRoute(
                name: "Ordenes",
                url: "Ordenes/{action}/{id}",
                defaults: new { controller = "Ordenes", action = "Index", id = UrlParameter.Optional },
                constraints: new { id = "(" + PatronGuid + ")?" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Ordenes", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
