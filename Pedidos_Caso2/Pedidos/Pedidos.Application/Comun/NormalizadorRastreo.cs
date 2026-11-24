using System.Linq;

namespace Pedidos.Application.Comun
{
    /// <summary>Normaliza el número de rastreo: sin espacios y en mayúsculas.</summary>
    public static class NormalizadorRastreo
    {
        public const int LongitudMinima = 6;
        public const int LongitudMaxima = 50;

        public static string Normalizar(string numeroRastreo)
        {
            if (string.IsNullOrWhiteSpace(numeroRastreo))
            {
                return string.Empty;
            }

            var sinEspacios = new string(numeroRastreo.Where(c => !char.IsWhiteSpace(c)).ToArray());
            return sinEspacios.ToUpperInvariant();
        }
    }
}
