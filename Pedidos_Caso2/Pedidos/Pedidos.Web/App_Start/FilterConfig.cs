using Pedidos.Web.Filters;
using System.Web.Mvc;

namespace Pedidos.Web
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            
            filters.Add(new ManejarExcepcionAttribute());

         
            filters.Add(new AuthorizeAttribute());
        }
    }
}