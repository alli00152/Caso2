using FluentValidation.Mvc;
using Pedidos.Infrastructure;
using Serilog;
using System.Data.Entity;
using System.Security.Claims;
using System.Web;
using System.Web.Helpers;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Pedidos.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            ConfigurarLogging();

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            // La base se crea y actualiza con migraciones (Update-Database), no automáticamente.
            Database.SetInitializer<PedidosDbContext>(null);

            // Necesario para que el token antifalsificación funcione con identidades basadas en claims.
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

            // Integración de FluentValidation con MVC 5.
            FluentValidationModelValidatorProvider.Configure();

            AutofacConfig.RegistrarDependencias();

            Log.Information("APLICACION_INICIADA");
        }

        protected void Application_End()
        {
            Log.CloseAndFlush();
        }

        /// <summary>Serilog a consola y a archivo diario (App_Data/logs).</summary>
        private static void ConfigurarLogging()
        {
            var rutaArchivo = HostingEnvironment.MapPath("~/App_Data/logs/pedidos-.log");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .WriteTo.File(rutaArchivo, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
                .CreateLogger();
        }
    }
}
