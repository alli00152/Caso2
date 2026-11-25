using AutoMapper;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Pedidos.Application.Comun;
using Pedidos.Infrastructure;
using Pedidos.Web.Extensions;
using Pedidos.Web.Models;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Pedidos.Web.Controllers
{
    
    public class AccountController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMapper _mapper;

        public AccountController(UserManager<ApplicationUser> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        private IAuthenticationManager Autenticacion
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        // ---------------------------------------------------------------- Inicio (pantalla de bienvenida)

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Inicio()
        {
            // Quien ya inició sesión va directo al listado; los demás eligen ingresar o registrarse.
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Ordenes");
            }

            return View();
        }

        // ---------------------------------------------------------------- Registro

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Registro()
        {
            return View(new RegistroFormModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Registro(RegistroFormModel model)
        {
            if (!ModelState.IsValid)
            {
                return VistaInvalida(model);
            }

            var usuario = _mapper.Map<ApplicationUser>(model);
            var resultado = await _userManager.CreateAsync(usuario, model.Password);

            if (!resultado.Succeeded)
            {
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }

                return VistaInvalida(model);
            }

            // Todo usuario que se registra por su cuenta es Cliente. Administrador y Operador los asigna un administrador.
            await _userManager.AddToRoleAsync(usuario.Id, Roles.Cliente);
            await IniciarSesionAsync(usuario, false);

            TempData["Exito"] = "Su cuenta fue creada correctamente.";
            return RedirectToAction("Index", "Ordenes");
        }

        // ---------------------------------------------------------------- Inicio de sesión

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Ingresar(string returnUrl)
        {
            // Un usuario ya autenticado llega aquí con returnUrl cuando le falta el rol: acceso denegado.
            if (User.Identity.IsAuthenticated && !string.IsNullOrEmpty(returnUrl))
            {
                return RespuestaError.Prohibido(Response);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginFormModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Ingresar(LoginFormModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return VistaInvalida(model);
            }

            var usuario = await _userManager.FindAsync(model.Email, model.Password);
            if (usuario == null)
            {
                // Mensaje genérico: no revela si el correo existe.
                ModelState.AddModelError(string.Empty, "El correo o la contraseña no son correctos.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            await IniciarSesionAsync(usuario, model.Recordarme);

            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Ordenes");
        }

        // ---------------------------------------------------------------- Cierre de sesión (POST)

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult Salir()
        {
            Autenticacion.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            TempData["Exito"] = "Sesión cerrada correctamente.";
            return RedirectToAction("Inicio");
        }

        private async Task IniciarSesionAsync(ApplicationUser usuario, bool persistente)
        {
            Autenticacion.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            var identidad = await _userManager.CreateIdentityAsync(usuario, DefaultAuthenticationTypes.ApplicationCookie);
            Autenticacion.SignIn(new AuthenticationProperties { IsPersistent = persistente }, identidad);
        }
    }
}
