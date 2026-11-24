using FluentValidation;
using Pedidos.Application.Requests;
using Pedidos.Domain.Entities;

namespace Pedidos.Application.Validadores
{
    public class CrearOrdenRequestValidator : AbstractValidator<CrearOrdenRequest>
    {
        public CrearOrdenRequestValidator()
        {
            RuleFor(x => x.ClienteNombre)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithMessage("El cliente es requerido.")
                .Must(v => v == null || (v.Trim().Length >= 3 && v.Trim().Length <= 120))
                .WithMessage("El cliente debe tener entre 3 y 120 caracteres.");

            RuleFor(x => x.DireccionEntrega)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithMessage("La dirección de entrega es requerida.")
                .Must(v => v == null || (v.Trim().Length >= 10 && v.Trim().Length <= 250))
                .WithMessage("La dirección debe tener entre 10 y 250 caracteres.");

            RuleFor(x => x.Detalles)
                .NotEmpty().WithMessage("La orden debe incluir al menos un detalle.");

            RuleForEach(x => x.Detalles).SetValidator(new DetalleOrdenRequestValidator());
        }
    }

    public class DetalleOrdenRequestValidator : AbstractValidator<DetalleOrdenRequest>
    {
        public DetalleOrdenRequestValidator()
        {
            RuleFor(x => x.Producto)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithMessage("El producto es requerido.")
                .MaximumLength(100).WithMessage("El producto no puede exceder 100 caracteres.");

            RuleFor(x => x.Cantidad)
                .InclusiveBetween(DetalleOrden.CantidadMinima, DetalleOrden.CantidadMaxima)
                .WithMessage("La cantidad debe estar entre 1 y 99.");
        }
    }
}
