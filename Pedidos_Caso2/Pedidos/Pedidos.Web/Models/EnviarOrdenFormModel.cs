using System;
using System.ComponentModel.DataAnnotations;

namespace Pedidos.Web.Models
{
    public class EnviarOrdenFormModel
    {
        public Guid OrdenId { get; set; }

        [Required(ErrorMessage = "El número de rastreo es requerido.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "El número de rastreo debe tener entre 6 y 50 caracteres.")]
        [Display(Name = "Número de rastreo")]
        public string NumeroRastreo { get; set; }

        /// <summary>Versión de la orden al abrir el formulario (detección de modificaciones concurrentes).</summary>
        public string RowVersion { get; set; }

        // Solo informativos para mostrar el formulario; el servicio no los usa.
        public string ClienteNombre { get; set; }
        public string DireccionEntrega { get; set; }
    }
}
