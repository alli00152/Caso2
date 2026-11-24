using System;

namespace Pedidos.Application.Requests
{
    public class EnviarOrdenRequest
    {
        public Guid OrdenId { get; set; }
        public string NumeroRastreo { get; set; }

        /// <summary>Versión de la orden con la que el usuario abrió el formulario.</summary>
        public byte[] RowVersion { get; set; }
    }
}
