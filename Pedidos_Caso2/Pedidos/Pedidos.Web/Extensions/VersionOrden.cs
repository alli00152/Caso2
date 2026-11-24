using System;

namespace Pedidos.Web.Extensions
{
    /// <summary>Convierte el RowVersion (byte[]) a texto para el formulario y de regreso.</summary>
    public static class VersionOrden
    {
        public static string ATexto(byte[] version)
        {
            return version == null ? string.Empty : Convert.ToBase64String(version);
        }

        /// <summary>Devuelve null si el texto no es válido (el validador lo rechaza como versión faltante).</summary>
        public static byte[] DesdeTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            try
            {
                return Convert.FromBase64String(texto);
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
