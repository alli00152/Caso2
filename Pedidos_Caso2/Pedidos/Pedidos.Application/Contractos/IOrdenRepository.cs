using Pedidos.Domain.Entities;
using System;
using System.Collections.Generic;

namespace Pedidos.Application.Contractos
{
    public interface IOrdenRepository
    {
        /// <summary>Obtiene la orden (con sus detalles) con seguimiento, para poder modificarla.</summary>
        Orden ObtenerPorId(Guid id);

        /// <summary>Busca por número de rastreo ya normalizado.</summary>
        Orden ObtenerPorRastreo(string numeroRastreoNormalizado);

        IReadOnlyCollection<Orden> ObtenerTodas();

        /// <summary>Filtra en la consulta SQL por usuario propietario (no es un filtro visual).</summary>
        IReadOnlyCollection<Orden> ObtenerPorPropietario(string applicationUserId);

        bool ExisteRastreo(string numeroRastreoNormalizado, Guid? excluirOrdenId);

        void Agregar(Orden orden);

        /// <summary>
        /// Indica la versión (RowVersion) con la que el usuario cargó la orden, para detectar
        /// modificaciones concurrentes al guardar.
        /// </summary>
        void EstablecerVersionOriginal(Orden orden, byte[] rowVersion);

        /// <summary>
        /// Guarda de forma atómica. Lanza ConcurrenciaException o ViolacionUnicidadException.
        /// </summary>
        void GuardarCambios();
    }
}
