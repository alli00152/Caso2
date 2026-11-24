using Pedidos.Web.Filters;
using System.Web.Mvc;

namespace Pedidos.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            // Global: toda excepción no controlada se registra y se muestra la vista Error segura.
            // RegistrarAuditoria se aplica de forma selectiva en OrdenesController y UsuariosController,
            // y RequiereOrdenPropia solo en las acciones que reciben el identificador de una orden.
            filters.Add(new ManejarExcepcionAttribute());
        }
    }
}
