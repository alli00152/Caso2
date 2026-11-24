using System;

namespace Pedidos.Domain.Entities
{
    /// <summary>Línea de producto/cantidad de una orden.</summary>
    public class DetalleOrden
    {
        public const int CantidadMinima = 1;
        public const int CantidadMaxima = 99;

        protected DetalleOrden() { }

        public DetalleOrden(Guid ordenId, string producto, int cantidad)
        {
            if (string.IsNullOrWhiteSpace(producto))
                throw new ArgumentException("El producto es obligatorio.", nameof(producto));
            if (cantidad < CantidadMinima || cantidad > CantidadMaxima)
                throw new ArgumentOutOfRangeException(nameof(cantidad),
                    $"La cantidad debe estar entre {CantidadMinima} y {CantidadMaxima}.");

            Id = Guid.NewGuid();
            OrdenId = ordenId;
            Producto = producto.Trim();
            Cantidad = cantidad;
        }

        public Guid Id { get; private set; }
        public Guid OrdenId { get; private set; }
        public string Producto { get; private set; }
        public int Cantidad { get; private set; }
    }
}
