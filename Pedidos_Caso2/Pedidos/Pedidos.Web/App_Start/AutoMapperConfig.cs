using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;

namespace Pedidos.Web
{
    /// <summary>
    /// Crea la configuración de AutoMapper a partir de los perfiles que Autofac tiene registrados
    /// (ver AutofacConfig). Centraliza los perfiles y valida la configuración al iniciar.
    /// </summary>
    public static class AutoMapperConfig
    {
        public static MapperConfiguration Crear(IEnumerable<Profile> perfiles)
        {
            var configuracion = new MapperConfiguration(config =>
            {
                foreach (var perfil in perfiles)
                {
                    config.AddProfile(perfil);
                }
            }, NullLoggerFactory.Instance); // AutoMapper 16 exige un ILoggerFactory (mismo patrón que HeroVerseMatrix).

            // Falla al iniciar si algún mapeo quedó incompleto (en vez de fallar en tiempo de ejecución).
            configuracion.AssertConfigurationIsValid();
            return configuracion;
        }
    }
}
