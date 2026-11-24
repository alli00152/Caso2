using Autofac;
using Autofac.Integration.Mvc;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Pedidos.Application.Contractos;
using Pedidos.Application.Mappings;
using Pedidos.Application.Requests;
using Pedidos.Application.Servicios;
using Pedidos.Application.Validadores;
using Pedidos.Infrastructure;
using Pedidos.Web.Identity;
using Pedidos.Web.Mappings;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Pedidos.Web
{
    /// <summary>
    /// Registro de dependencias. Pedidos.Web referencia Infrastructure solamente para esto.
    /// </summary>
    public static class AutofacConfig
    {
        public static void RegistrarDependencias()
        {
            var builder = new ContainerBuilder();

            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            // Permite inyectar HttpContextBase (usado por UsuarioActual).
            builder.RegisterModule<AutofacWebTypesModule>();

            // Un DbContext por solicitud, compartido por repositorios y por Identity.
            builder.RegisterType<PedidosDbContext>().InstancePerRequest();

            // AutoMapper: los perfiles (Application y Web) se registran en Autofac; la configuración
            // se construye con ellos y se valida al resolverse por primera vez (AssertConfigurationIsValid).
            builder.RegisterType<ApplicationMappingProfile>().As<Profile>();
            builder.RegisterType<WebMappingProfile>().As<Profile>();
            builder.Register(c => AutoMapperConfig.Crear(c.Resolve<IEnumerable<Profile>>()))
                .AsSelf().SingleInstance();
            builder.Register(c => c.Resolve<MapperConfiguration>().CreateMapper())
                .As<IMapper>().InstancePerLifetimeScope();

            // ASP.NET Identity
            builder.Register(c => new UserStore<ApplicationUser>(c.Resolve<PedidosDbContext>()))
                .As<IUserStore<ApplicationUser>>()
                .InstancePerRequest();

            builder.Register(c => CrearUserManager(c.Resolve<IUserStore<ApplicationUser>>()))
                .InstancePerRequest();

            builder.Register(c => new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(c.Resolve<PedidosDbContext>())))
                .InstancePerRequest();

            builder.RegisterType<UsuarioActual>().As<IUsuarioActual>().InstancePerRequest();

            // Repositorio y servicio
            builder.RegisterType<OrdenRepository>().As<IOrdenRepository>().InstancePerRequest();
            builder.RegisterType<OrdenServicio>().As<IOrdenService>().InstancePerRequest();

            // Validadores (FluentValidation, definidos en Application)
            builder.RegisterType<CrearOrdenRequestValidator>().As<IValidator<CrearOrdenRequest>>().InstancePerRequest();
            builder.RegisterType<EnviarOrdenRequestValidator>().As<IValidator<EnviarOrdenRequest>>().InstancePerRequest();
            builder.RegisterType<CambiarEstadoOrdenRequestValidator>().As<IValidator<CambiarEstadoOrdenRequest>>().InstancePerRequest();

            var contenedor = builder.Build();

            // Resolver la configuración aquí ejecuta AssertConfigurationIsValid() al iniciar la aplicación.
            contenedor.Resolve<MapperConfiguration>();

            DependencyResolver.SetResolver(new AutofacDependencyResolver(contenedor));
        }

        private static UserManager<ApplicationUser> CrearUserManager(IUserStore<ApplicationUser> almacen)
        {
            var manager = new UserManager<ApplicationUser>(almacen);

            manager.UserValidator = new UserValidator<ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Política de contraseñas: la valida Identity (la aplicación nunca calcula hashes).
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 8,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true
            };

            return manager;
        }
    }
}
