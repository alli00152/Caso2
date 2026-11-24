using System;

namespace Pedidos.Web.Models
{
    /// <summary>
    /// Modelo específico para los cambios de estado (confirmar, preparar, entregar, cancelar).
    /// El estado destino lo define la acción del controlador, no el formulario.
    /// </summary>
    public class CambiarEstadoFormModel
    {
        public Guid OrdenId { get; set; }
        public string RowVersion { get; set; }
    }
}
