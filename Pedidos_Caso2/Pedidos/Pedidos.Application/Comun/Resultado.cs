using System.Collections.Generic;
using System.Linq;

namespace Pedidos.Application.Comun
{
    /// <summary>
    /// Tipos de fallo previsibles. El controlador los traduce a códigos HTTP:
    /// Validacion=400, Prohibido=403, NoEncontrado=404, Conflicto=409.
    /// Las excepciones inesperadas NO pasan por aquí: las atiende el filtro global.
    /// </summary>
    public enum TipoError
    {
        Validacion = 1,
        NoEncontrado = 2,
        Conflicto = 3,
        Prohibido = 4
    }

    /// <summary>Error de validación asociado a una propiedad del request.</summary>
    public sealed class ErrorValidacion
    {
        public ErrorValidacion(string propiedad, string mensaje)
        {
            Propiedad = propiedad ?? string.Empty;
            Mensaje = mensaje;
        }

        public string Propiedad { get; private set; }
        public string Mensaje { get; private set; }
    }

    public class Resultado
    {
        private static readonly IReadOnlyList<ErrorValidacion> SinErrores = new ErrorValidacion[0];

        protected Resultado(bool esExitoso, TipoError tipoError, string mensaje, IReadOnlyList<ErrorValidacion> errores)
        {
            EsExitoso = esExitoso;
            TipoError = tipoError;
            Mensaje = mensaje;
            Errores = errores ?? SinErrores;
        }

        public bool EsExitoso { get; private set; }
        public TipoError TipoError { get; private set; }
        public string Mensaje { get; private set; }
        public IReadOnlyList<ErrorValidacion> Errores { get; private set; }

        public static Resultado Exitoso()
        {
            return new Resultado(true, default(TipoError), string.Empty, null);
        }

        public static Resultado Fallido(TipoError tipoError, string mensaje)
        {
            return new Resultado(false, tipoError, mensaje, null);
        }

        public static Resultado FallidoValidacion(IEnumerable<ErrorValidacion> errores)
        {
            return new Resultado(false, TipoError.Validacion,
                "La solicitud contiene datos no válidos.", errores.ToList());
        }
    }

    public sealed class Resultado<T> : Resultado
    {
        private Resultado(bool esExitoso, TipoError tipoError, string mensaje, IReadOnlyList<ErrorValidacion> errores, T valor)
            : base(esExitoso, tipoError, mensaje, errores)
        {
            Valor = valor;
        }

        public T Valor { get; private set; }

        public static Resultado<T> Exitoso(T valor)
        {
            return new Resultado<T>(true, default(TipoError), string.Empty, null, valor);
        }

        public new static Resultado<T> Fallido(TipoError tipoError, string mensaje)
        {
            return new Resultado<T>(false, tipoError, mensaje, null, default(T));
        }

        public new static Resultado<T> FallidoValidacion(IEnumerable<ErrorValidacion> errores)
        {
            return new Resultado<T>(false, TipoError.Validacion,
                "La solicitud contiene datos no válidos.", errores.ToList(), default(T));
        }
    }
}
