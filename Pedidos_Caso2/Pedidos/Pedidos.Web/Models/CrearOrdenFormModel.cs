using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pedidos.Web.Models
{
    /// <summary>
    /// Datos del formulario de creación. Las anotaciones solo ayudan al cliente (validación en el navegador);
    /// la fuente de verdad son los validadores FluentValidation de la capa Application.
    /// No incluye propietario ni estado: eso nunca se recibe del formulario (over-posting).
    /// </summary>
    public class CrearOrdenFormModel
    {
        public CrearOrdenFormModel()
        {
            Detalles = new List<DetalleOrdenFormModel> { new DetalleOrdenFormModel() };
        }

        [Required(ErrorMessage = "El cliente es requerido.")]
        [StringLength(120, MinimumLength = 3, ErrorMessage = "El cliente debe tener entre 3 y 120 caracteres.")]
        [Display(Name = "Cliente")]
        public string ClienteNombre { get; set; }

        [Required(ErrorMessage = "La dirección de entrega es requerida.")]
        [StringLength(250, MinimumLength = 10, ErrorMessage = "La dirección debe tener entre 10 y 250 caracteres.")]
        [Display(Name = "Dirección de entrega")]
        public string DireccionEntrega { get; set; }

        public List<DetalleOrdenFormModel> Detalles { get; set; }

        /// <summary>Garantiza que la vista muestre al menos una fila al volver a mostrar el formulario.</summary>
        public void AsegurarFilaInicial()
        {
            if (Detalles == null || Detalles.Count == 0)
            {
                Detalles = new List<DetalleOrdenFormModel> { new DetalleOrdenFormModel() };
            }
        }
    }

    public class DetalleOrdenFormModel
    {
        public DetalleOrdenFormModel()
        {
            Cantidad = 1;
        }

        [Required(ErrorMessage = "El producto es requerido.")]
        [StringLength(100, ErrorMessage = "El producto no puede exceder 100 caracteres.")]
        [Display(Name = "Producto")]
        public string Producto { get; set; }

        [Range(1, 99, ErrorMessage = "La cantidad debe estar entre 1 y 99.")]
        [Display(Name = "Cantidad")]
        public int Cantidad { get; set; }
    }
}
