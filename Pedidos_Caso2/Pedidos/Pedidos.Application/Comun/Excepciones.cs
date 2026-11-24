using System;

namespace Pedidos.Application.Comun
{
    /// <summary>
    /// La orden fue modificada por otra persona después de cargarla (RowVersion distinto).
    /// La lanza el repositorio y el servicio la traduce a un Resultado de tipo Conflicto.
    /// </summary>
    public class ConcurrenciaException : Exception
    {
        public ConcurrenciaException(Exception interna)
            : base("La orden fue modificada por otra persona.", interna) { }
    }

    /// <summary>Se violó una restricción de unicidad en la base de datos (p. ej., número de rastreo).</summary>
    public class ViolacionUnicidadException : Exception
    {
        public ViolacionUnicidadException(Exception interna)
            : base("Se violó una restricción de unicidad.", interna) { }
    }
}
