using Pedidos.Application.Comun;
using System.Web.Mvc;

namespace Pedidos.Web.Extensions
{
    public static class ResultadoExtensions
    {
        /// <summary>
        /// Copia los errores de validación de la capa Application (FluentValidation) al ModelState
        /// para mostrarlos junto a cada campo. Los nombres de propiedad de los Request coinciden con
        /// los de los FormModel (p. ej. "Detalles[0].Producto").
        /// </summary>
        public static void AgregarAModelState(this Resultado resultado, ModelStateDictionary modelState)
        {
            foreach (var error in resultado.Errores)
            {
                modelState.AddModelError(error.Propiedad, error.Mensaje);
            }
        }
    }
}
