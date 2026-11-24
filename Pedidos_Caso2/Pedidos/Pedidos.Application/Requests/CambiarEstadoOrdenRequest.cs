using Pedidos.Domain.Enums;
using System;

namespace Pedidos.Application.Requests
{
    public class CambiarEstadoOrdenRequest
    {
        public Guid OrdenId { get; set; }
        public EstadoOrden EstadoSolicitado { get; set; }

        /// <summary>Versión de la orden con la que el usuario abrió la pantalla.</summary>
        public byte[] RowVersion { get; set; }
    }
}
