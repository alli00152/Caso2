using Microsoft.AspNet.Identity.EntityFramework;
using Pedidos.Domain.Entities;
using System.Collections.Generic;

namespace Pedidos.Infrastructure
{
    /// <summary>
    /// Usuario de ASP.NET Identity. Identity administra la contraseña (hash, sal, validación);
    /// la aplicación nunca la guarda ni la calcula manualmente.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public ApplicationUser()
        {
            Ordenes = new List<Orden>();
        }

        public string NombreCompleto { get; set; }

        /// <summary>Órdenes de las que este usuario es propietario (FK Orden.ApplicationUserId).</summary>
        public ICollection<Orden> Ordenes { get; private set; }
    }
}
