using Pedidos.Application.Comun;
using Pedidos.Application.Requests;
using System;
using System.Collections.Generic;

namespace Pedidos.Application.Contractos
{
    public interface IOrdenService
    {
        Resultado<Guid> Crear(CrearOrdenRequest request);

        Resultado<OrdenDto> ObtenerPorId(Guid ordenId);

        /// <summary>Verifica que el usuario actual pueda acceder a la orden (existe y es suya o es personal interno).</summary>
        Resultado VerificarAcceso(Guid ordenId);

        /// <summary>Personal interno: todas las órdenes. Cliente: solo las propias.</summary>
        IReadOnlyCollection<OrdenDto> ObtenerListado();

        /// <summary>Consulta por número de rastreo o por identificador público (Guid) de la orden.</summary>
        Resultado<OrdenDto> ConsultarRastreo(string criterio);

        /// <summary>Confirmar, preparar, entregar o cancelar. El envío usa <see cref="Enviar"/>.</summary>
        Resultado CambiarEstado(CambiarEstadoOrdenRequest request);

        Resultado Enviar(EnviarOrdenRequest request);
    }
}
