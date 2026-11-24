namespace Pedidos.Application.Comun
{
    /// <summary>Nombres de los roles de la aplicación (constantes para usarlas en [Authorize]).</summary>
    public static class Roles
    {
        public const string Administrador = "Administrador";
        public const string Operador = "Operador";
        public const string Cliente = "Cliente";

        /// <summary>Roles con acceso al personal interno (listas separadas por coma para [Authorize]).</summary>
        public const string AdministradorOOperador = Administrador + "," + Operador;
    }
}
