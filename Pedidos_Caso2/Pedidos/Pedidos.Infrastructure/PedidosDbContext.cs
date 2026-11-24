using Microsoft.AspNet.Identity.EntityFramework;
using Pedidos.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;

namespace Pedidos.Infrastructure
{
    /// <summary>
    /// Un único contexto para el modelo de órdenes y las tablas de Identity, de modo que una sola
    /// migración (AgregarIdentityYPropietarioOrden) cree las claves foráneas entre ambos.
    /// </summary>
    public class PedidosDbContext : IdentityDbContext<ApplicationUser>
    {
        public PedidosDbContext() : base("PedidosConnection", throwIfV1Schema: false) { }

        public DbSet<Orden> Ordenes { get; set; }
        public DbSet<DetalleOrden> DetallesOrden { get; set; }

        protected override void OnModelCreating(DbModelBuilder builder)
        {
            // Primero la configuración de Identity.
            base.OnModelCreating(builder);

            builder.Entity<ApplicationUser>().Property(x => x.NombreCompleto).HasMaxLength(120);

            // ---- Orden
            builder.Entity<Orden>().ToTable("Ordenes").HasKey(x => x.Id);
            builder.Entity<Orden>().Property(x => x.ClienteNombre).IsRequired().HasMaxLength(150);
            builder.Entity<Orden>().Property(x => x.DireccionEntrega).IsRequired().HasMaxLength(250);

            // Índice para las consultas por propietario.
            builder.Entity<Orden>().Property(x => x.ApplicationUserId)
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnAnnotation("Index",
                    new IndexAnnotation(new IndexAttribute("IX_Ordenes_ApplicationUserId")));

            // Índice para las consultas por número de rastreo. La UNICIDAD (cuando existe valor) se
            // refuerza en la base de datos con un índice único filtrado (ver README, migración
            // UnicidadRastreoFiltrada), porque EF6 no genera índices filtrados.
            builder.Entity<Orden>().Property(x => x.NumeroRastreo)
                .HasMaxLength(50)
                .HasColumnAnnotation("Index",
                    new IndexAnnotation(new IndexAttribute("IX_Ordenes_NumeroRastreo")));

            // ---- Detalle
            builder.Entity<DetalleOrden>().ToTable("DetallesOrden").HasKey(x => x.Id);
            builder.Entity<DetalleOrden>().Property(x => x.Producto).IsRequired().HasMaxLength(100);

            // ---- Relaciones (claves foráneas)
            builder.Entity<Orden>()
                .HasMany(o => o.Detalles)
                .WithRequired()
                .HasForeignKey(d => d.OrdenId)
                .WillCascadeOnDelete(true);

            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Ordenes)
                .WithRequired()
                .HasForeignKey(o => o.ApplicationUserId)
                .WillCascadeOnDelete(false);
        }
    }
}
