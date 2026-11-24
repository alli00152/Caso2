namespace Pedidos.Infrastructure.Migrations
{
    using Microsoft.AspNet.Identity;
    using Microsoft.AspNet.Identity.EntityFramework;
    using Pedidos.Application.Comun;
    using System;
    using System.Configuration;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Pedidos.Infrastructure.PedidosDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }

        /// <summary>
        /// Crea los roles y, si están configuradas, las cuentas iniciales de Administrador y Operador.
        /// Las credenciales NO están en el código: se leen de appSettings (archivo Secrets.config,
        /// que no se incluye en el repositorio). Si no hay configuración, solo se crean los roles y
        /// las cuentas las crea quien evalúa. Las contraseñas las procesa siempre Identity.
        /// </summary>
        protected override void Seed(Pedidos.Infrastructure.PedidosDbContext context)
        {
            // Si la migración de Identity todavía no se aplicó (p. ej. al aplicar solo CrearOrdenes
            // antes de generar las nuevas migraciones), las tablas de Identity no existen: no hay nada que sembrar.
            var tablasDeIdentity = context.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM sys.tables WHERE name = 'AspNetRoles'").Single();
            if (tablasDeIdentity == 0)
            {
                return;
            }

            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            foreach (var rol in new[] { Roles.Administrador, Roles.Operador, Roles.Cliente })
            {
                if (!roleManager.RoleExists(rol))
                {
                    roleManager.Create(new IdentityRole(rol));
                }
            }

            var userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));

            CrearUsuarioInicial(userManager, "Seed:AdminEmail", "Seed:AdminPassword",
                "Administrador del sistema", Roles.Administrador);
            CrearUsuarioInicial(userManager, "Seed:OperadorEmail", "Seed:OperadorPassword",
                "Operador del sistema", Roles.Operador);
        }

        private static void CrearUsuarioInicial(
            UserManager<ApplicationUser> userManager,
            string claveCorreo, string clavePassword,
            string nombreCompleto, string rol)
        {
            var correo = ConfigurationManager.AppSettings[claveCorreo];
            var password = ConfigurationManager.AppSettings[clavePassword];

            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            if (userManager.FindByName(correo) != null)
            {
                return;
            }

            var usuario = new ApplicationUser
            {
                UserName = correo,
                Email = correo,
                NombreCompleto = nombreCompleto,
                EmailConfirmed = true
            };

            var resultado = userManager.Create(usuario, password);
            if (!resultado.Succeeded)
            {
                // Se informan los motivos de Identity; nunca se incluye la contraseña.
                throw new InvalidOperationException(
                    "No se pudo crear el usuario inicial (" + rol + "): " + string.Join(" ", resultado.Errors));
            }

            userManager.AddToRole(usuario.Id, rol);
        }
    }
}