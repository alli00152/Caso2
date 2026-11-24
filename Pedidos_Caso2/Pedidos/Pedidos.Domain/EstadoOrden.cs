using System;

namespace Pedidos.Domain.Enums
{
    public enum EstadoOrden {
        Creada = 1,
        Confirmada,
        EnPreparacion,
        Enviada,
        Entregada,
        Cancelada
    }
}

