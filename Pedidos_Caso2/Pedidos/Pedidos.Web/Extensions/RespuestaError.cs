using Pedidos.Application.Comun;
using Pedidos.Web.ViewModels;
using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Pedidos.Web.Extensions
{
    /// <summary>
    /// Traduce los resultados previsibles (Result Pattern) a respuestas HTTP coherentes:
    /// 400 entrada inválida, 403 acceso denegado, 404 no existe, 409 conflicto y 500 solo para fallos inesperados.
    /// </summary>
    public static class RespuestaError
    {
        public static int CodigoHttp(TipoError tipo)
        {
            switch (tipo)
            {
                case TipoError.Validacion: return 400;
                case TipoError.Prohibido: return 403;
                case TipoError.NoEncontrado: return 404;
                case TipoError.Conflicto: return 409;
                default: return 500;
            }
        }

        public static ViewResult DesdeResultado(HttpResponseBase respuesta, Resultado resultado, Guid? ordenId = null)
        {
            var codigo = CodigoHttp(resultado.TipoError);
            var mensaje = resultado.Mensaje;

            if (resultado.TipoError == TipoError.Validacion && resultado.Errores.Count > 0)
            {
                mensaje = string.Join(" ", resultado.Errores.Select(e => e.Mensaje));
            }

            return Vista(respuesta, codigo, TituloPara(codigo), mensaje, ordenId);
        }

        public static ViewResult Prohibido(HttpResponseBase respuesta)
        {
            return Vista(respuesta, 403, TituloPara(403), "No tiene permiso para realizar esta operación.");
        }

        public static ViewResult Vista(
            HttpResponseBase respuesta, int codigo, string titulo, string mensaje,
            Guid? ordenId = null, string errorId = null)
        {
            respuesta.StatusCode = codigo;
            respuesta.TrySkipIisCustomErrors = true;

            var modelo = new ErrorViewModel
            {
                Codigo = codigo,
                Titulo = titulo,
                Mensaje = mensaje,
                OrdenId = ordenId,
                ErrorId = errorId
            };

            return new ViewResult
            {
                ViewName = "Error",
                ViewData = new ViewDataDictionary(modelo)
            };
        }

        private static string TituloPara(int codigo)
        {
            switch (codigo)
            {
                case 400: return "Solicitud no válida";
                case 403: return "Acceso denegado";
                case 404: return "No encontrado";
                case 409: return "Conflicto";
                default: return "Ocurrió un error";
            }
        }
    }
}
