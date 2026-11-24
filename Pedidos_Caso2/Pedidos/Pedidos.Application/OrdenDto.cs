using System;
using System.Collections.Generic;

namespace Pedidos.Application
{
    public class OrdenDto
    {
        public Guid Id { get; set; }
        public string ClienteNombre { get; set; }
        public string DireccionEntrega { get; set; }
        public string Estado { get; set; }
        public string NumeroRastreo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public byte[] RowVersion { get; set; }
        public List<DetalleOrdenDto> Detalles { get; set; }

        /// <summary>Estados a los que el Domain permite pasar desde el estado actual.</summary>
        public List<string> TransicionesPermitidas { get; set; }
    }

    public class DetalleOrdenDto
    {
        public string Producto { get; set; }
        public int Cantidad { get; set; }
    }
}
