namespace Pedidos.Application.Contractos
{
    /// <summary>
    /// Abstrae al usuario autenticado para que Application no dependa de Identity ni de System.Web
    /// (mismo enfoque que IClock: se puede reemplazar en las pruebas).
    /// </summary>
    public interface IUsuarioActual
    {
        bool EstaAutenticado { get; }

        /// <summary>Identificador del usuario (ApplicationUserId).</summary>
        string Id { get; }

        bool EsAdministrador { get; }
        bool EsOperador { get; }
    }
}
