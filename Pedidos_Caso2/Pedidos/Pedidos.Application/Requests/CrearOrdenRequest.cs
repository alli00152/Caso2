using System.Collections.Generic;

namespace Pedidos.Application.Requests
{
    public class CrearOrdenRequest
    {
        public CrearOrdenRequest()
        {
            Detalles = new List<DetalleOrdenRequest>();
        }

        public string ClienteNombre { get; set; }
        public string DireccionEntrega { get; set; }
        public List<DetalleOrdenRequest> Detalles { get; set; }
    }

    public class DetalleOrdenRequest
    {
        public string Producto { get; set; }
        public int Cantidad { get; set; }
    }
}
