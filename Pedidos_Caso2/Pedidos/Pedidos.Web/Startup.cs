using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;

[assembly: OwinStartup(typeof(Pedidos.Web.Startup))]

namespace Pedidos.Web
{
    /// <summary>Configura la autenticación por cookie de ASP.NET Identity.</summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Ingresar"),
                CookieHttpOnly = true,
                Provider = new CookieAuthenticationProvider
                {
                    // Un usuario YA autenticado que no tiene el rol requerido recibe la página de
                    // acceso denegado (403) y no se le vuelve a pedir que inicie sesión.
                    OnApplyRedirect = contexto =>
                    {
                        var autenticado = contexto.Request.User != null &&
                                          contexto.Request.User.Identity != null &&
                                          contexto.Request.User.Identity.IsAuthenticated;

                        if (autenticado)
                        {
                            contexto.Response.Redirect(
                                contexto.Request.PathBase.Add(new PathString("/Error/Prohibido")).Value);
                        }
                        else
                        {
                            contexto.Response.Redirect(contexto.RedirectUri);
                        }
                    }
                }
            });
        }
    }
}
