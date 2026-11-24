using System;

namespace Pedidos.Web.ViewModels
{
    /// <summary>Datos seguros para mostrar un error: nunca incluye excepciones, rutas ni cadenas de conexión.</summary>
    public class ErrorViewModel
    {
        public int Codigo { get; set; }
        public string Titulo { get; set; }
        public string Mensaje { get; set; }

        /// <summary>Si el error es sobre una orden, permite volver a cargarla.</summary>
        public Guid? OrdenId { get; set; }

        /// <summary>Código de correlación: coincide con el registro del log, sin revelar detalles.</summary>
        public string ErrorId { get; set; }
    }
}
