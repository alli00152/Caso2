using FluentValidation;
using Pedidos.Application.Requests;
using System;

namespace Pedidos.Application.Validadores
{
    /// <summary>
    /// Valida únicamente la forma de la solicitud. Que la transición sea permitida
    /// es una regla de dominio/servicio y NO se valida aquí.
    /// </summary>
    public class CambiarEstadoOrdenRequestValidator : AbstractValidator<CambiarEstadoOrdenRequest>
    {
        public CambiarEstadoOrdenRequestValidator()
        {
            RuleFor(x => x.OrdenId)
                .NotEqual(Guid.Empty).WithMessage("El identificador de la orden no es válido.");

            RuleFor(x => x.EstadoSolicitado)
                .IsInEnum().WithMessage("El estado solicitado no es válido.");

            RuleFor(x => x.RowVersion)
                .NotEmpty().WithMessage("No se pudo verificar la versión de la orden. Vuelva a cargarla.");
        }
    }
}
