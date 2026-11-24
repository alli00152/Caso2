using Microsoft.AspNet.Identity;
using Pedidos.Application.Comun;
using Pedidos.Application.Contractos;
using System.Web;

namespace Pedidos.Web.Identity
{
    /// <summary>Implementa IUsuarioActual con el usuario autenticado de la solicitud HTTP.</summary>
    public class UsuarioActual : IUsuarioActual
    {
        private readonly HttpContextBase _httpContext;

        public UsuarioActual(HttpContextBase httpContext)
        {
            _httpContext = httpContext;
        }

        public bool EstaAutenticado
        {
            get
            {
                var usuario = _httpContext.User;
                return usuario != null && usuario.Identity != null && usuario.Identity.IsAuthenticated;
            }
        }

        public string Id
        {
            get { return EstaAutenticado ? _httpContext.User.Identity.GetUserId() : null; }
        }

        public bool EsAdministrador
        {
            get { return EstaAutenticado && _httpContext.User.IsInRole(Roles.Administrador); }
        }

        public bool EsOperador
        {
            get { return EstaAutenticado && _httpContext.User.IsInRole(Roles.Operador); }
        }
    }
}
