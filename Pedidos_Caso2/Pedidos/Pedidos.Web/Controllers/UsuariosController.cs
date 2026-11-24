using AutoMapper;
using Microsoft.AspNet.Identity;
using Pedidos.Application.Comun;
using Pedidos.Infrastructure;
using Pedidos.Web.Extensions;
using Pedidos.Web.Filters;
using Pedidos.Web.Models;
using Pedidos.Web.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Pedidos.Web.Controllers
{
    /// <summary>Administración de usuarios y roles: solo el rol Administrador.</summary>
    [Authorize(Roles = Roles.Administrador)]
    [RegistrarAuditoria]
    public class UsuariosController : BaseController
    {
        private static readonly string[] RolesPermitidos = { Roles.Administrador, Roles.Operador, Roles.Cliente };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public UsuariosController(UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        [HttpGet]
        public ActionResult Index()
        {
            var modelo = new List<UsuarioListadoViewModel>();
            foreach (var usuario in _userManager.Users.OrderBy(u => u.Email).ToList())
            {
                var fila = _mapper.Map<UsuarioListadoViewModel>(usuario);
                fila.Rol = string.Join(", ", _userManager.GetRoles(usuario.Id));
                modelo.Add(fila);
            }

            return View(modelo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CambiarRol(CambiarRolFormModel model)
        {
            if (!ModelState.IsValid || !RolesPermitidos.Contains(model.Rol))
            {
                return SolicitudInvalida("El rol solicitado no es válido.");
            }

            var usuario = _userManager.FindById(model.UserId);
            if (usuario == null)
            {
                return ErrorDe(Resultado.Fallido(TipoError.NoEncontrado, "El usuario no existe."));
            }

            // Evita que el único administrador se quite a sí mismo el acceso por accidente.
            if (usuario.Id == User.Identity.GetUserId())
            {
                return ErrorDe(Resultado.Fallido(TipoError.Conflicto, "No puede cambiar su propio rol."));
            }

            var actuales = _userManager.GetRoles(usuario.Id);
            if (actuales.Count > 0)
            {
                _userManager.RemoveFromRoles(usuario.Id, actuales.ToArray());
            }

            _userManager.AddToRole(usuario.Id, model.Rol);

            TempData["Exito"] = "El rol del usuario se actualizó correctamente.";
            return RedirectToAction("Index");
        }

        private ActionResult SolicitudInvalida(string mensaje)
        {
            return ErrorDe(Resultado.Fallido(TipoError.Validacion, mensaje));
        }
    }
}
