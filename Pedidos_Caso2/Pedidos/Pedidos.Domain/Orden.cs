using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Pedidos.Domain.Enums;

namespace Pedidos.Domain.Entities
{
    /// <summary>
    /// Orden de compra. El Domain controla los invariantes: datos obligatorios,
    /// detalles válidos y transiciones de estado permitidas.
    /// </summary>
    public class Orden
    {
        // Mapa de transiciones válidas: Creada -> Confirmada -> En preparación -> Enviada -> Entregada.
        // Cancelar solo es posible antes del envío.
        private static readonly Dictionary<EstadoOrden, EstadoOrden[]> Transiciones =
            new Dictionary<EstadoOrden, EstadoOrden[]>
            {
                { EstadoOrden.Creada,        new[] { EstadoOrden.Confirmada, EstadoOrden.Cancelada } },
                { EstadoOrden.Confirmada,    new[] { EstadoOrden.EnPreparacion, EstadoOrden.Cancelada } },
                { EstadoOrden.EnPreparacion, new[] { EstadoOrden.Enviada, EstadoOrden.Cancelada } },
                { EstadoOrden.Enviada,       new[] { EstadoOrden.Entregada } },
                { EstadoOrden.Entregada,     new EstadoOrden[0] },
                { EstadoOrden.Cancelada,     new EstadoOrden[0] }
            };

        protected Orden()
        {
            Detalles = new List<DetalleOrden>();
        }

        public Orden(string clienteNombre, string direccionEntrega, string applicationUserId)
        {
            if (string.IsNullOrWhiteSpace(clienteNombre))
                throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(clienteNombre));
            if (string.IsNullOrWhiteSpace(direccionEntrega))
                throw new ArgumentException("La dirección de entrega es obligatoria.", nameof(direccionEntrega));
            if (string.IsNullOrWhiteSpace(applicationUserId))
                throw new ArgumentException("La orden debe tener un usuario propietario.", nameof(applicationUserId));

            Id = Guid.NewGuid();
            ClienteNombre = clienteNombre.Trim();
            DireccionEntrega = direccionEntrega.Trim();
            ApplicationUserId = applicationUserId;
            Estado = EstadoOrden.Creada;
            FechaCreacion = DateTime.UtcNow;
            Detalles = new List<DetalleOrden>();
        }

        /// <summary>Identificador público no consecutivo (Guid).</summary>
        public Guid Id { get; private set; }
        public string ClienteNombre { get; private set; }
        public string DireccionEntrega { get; private set; }

        /// <summary>Usuario propietario de la orden (clave foránea a AspNetUsers).</summary>
        public string ApplicationUserId { get; private set; }

        public EstadoOrden Estado { get; private set; }
        public string NumeroRastreo { get; private set; }
        public DateTime FechaCreacion { get; private set; }
        public DateTime? FechaEnvio { get; private set; }
        public DateTime? FechaEntrega { get; private set; }

        /// <summary>Token de concurrencia optimista (rowversion de SQL Server).</summary>
        [Timestamp]
        public byte[] RowVersion { get; private set; }

        public ICollection<DetalleOrden> Detalles { get; private set; }

        public void AgregarDetalle(string producto, int cantidad)
        {
            Detalles.Add(new DetalleOrden(Id, producto, cantidad));
        }

        public static bool EsTransicionValida(EstadoOrden origen, EstadoOrden destino)
        {
            EstadoOrden[] permitidos;
            return Transiciones.TryGetValue(origen, out permitidos) && permitidos.Contains(destino);
        }

        public bool PuedeTransicionarA(EstadoOrden destino)
        {
            return EsTransicionValida(Estado, destino);
        }

        public IReadOnlyCollection<EstadoOrden> ObtenerTransicionesPermitidas()
        {
            EstadoOrden[] permitidos;
            return Transiciones.TryGetValue(Estado, out permitidos) ? permitidos : new EstadoOrden[0];
        }

        public void Confirmar()
        {
            CambiarA(EstadoOrden.Confirmada);
        }

        public void IniciarPreparacion()
        {
            CambiarA(EstadoOrden.EnPreparacion);
        }

        public void Enviar(string rastreo)
        {
            if (string.IsNullOrWhiteSpace(rastreo))
                throw new ArgumentException("El número de rastreo es obligatorio.", nameof(rastreo));

            CambiarA(EstadoOrden.Enviada);
            NumeroRastreo = rastreo;
            FechaEnvio = DateTime.UtcNow;
        }

        public void Entregar()
        {
            CambiarA(EstadoOrden.Entregada);
            FechaEntrega = DateTime.UtcNow;
        }

        public void Cancelar()
        {
            CambiarA(EstadoOrden.Cancelada);
        }

        private void CambiarA(EstadoOrden destino)
        {
            if (!PuedeTransicionarA(destino))
                throw new InvalidOperationException(
                    $"Transición no permitida de {Estado} a {destino}.");

            Estado = destino;
        }
    }
}
