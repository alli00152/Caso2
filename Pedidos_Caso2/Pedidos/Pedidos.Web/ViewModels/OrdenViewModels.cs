using System;
using System.Collections.Generic;

namespace Pedidos.Web.ViewModels
{
    /// <summary>Fila del listado: solo lo que la vista necesita.</summary>
    public class OrdenListadoItemViewModel
    {
        public Guid Id { get; set; }
        public string ClienteNombre { get; set; }
        public string Estado { get; set; }
        public string NumeroRastreo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class OrdenDetalleViewModel
    {
        public Guid Id { get; set; }
        public string ClienteNombre { get; set; }
        public string DireccionEntrega { get; set; }
        public string Estado { get; set; }
        public string NumeroRastreo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public string RowVersion { get; set; }
        public List<DetalleOrdenViewModel> Detalles { get; set; }
        public List<string> TransicionesPermitidas { get; set; }
    }

    public class DetalleOrdenViewModel
    {
        public string Producto { get; set; }
        public int Cantidad { get; set; }
    }

    /// <summary>Resultado de la consulta de rastreo (no incluye la dirección de entrega).</summary>
    public class RastreoViewModel
    {
        public string Criterio { get; set; }
        public bool Encontrada { get; set; }
        public Guid Id { get; set; }
        public string Estado { get; set; }
        public string NumeroRastreo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
    }

    public class UsuarioListadoViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string NombreCompleto { get; set; }
        public string Rol { get; set; }
    }
}
