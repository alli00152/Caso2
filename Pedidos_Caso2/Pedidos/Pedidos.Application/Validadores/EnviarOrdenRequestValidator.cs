using FluentValidation;
using Pedidos.Application.Comun;
using Pedidos.Application.Requests;
using System;
using System.Linq;

namespace Pedidos.Application.Validadores
{
    public class EnviarOrdenRequestValidator : AbstractValidator<EnviarOrdenRequest>
    {
        public EnviarOrdenRequestValidator()
        {
            RuleFor(x => x.OrdenId)
                .NotEqual(Guid.Empty).WithMessage("El identificador de la orden no es válido.");

            RuleFor(x => x.NumeroRastreo)
                .Cascade(CascadeMode.StopOnFirstFailure)
                .NotEmpty().WithMessage("El número de rastreo es requerido.")
                .Must(LongitudValida)
                .WithMessage("El número de rastreo debe tener entre 6 y 50 caracteres.")
                .Must(CaracteresValidos)
                .WithMessage("El número de rastreo solo puede contener letras, números y guiones.");

            RuleFor(x => x.RowVersion)
                .NotEmpty().WithMessage("No se pudo verificar la versión de la orden. Vuelva a cargarla.");
        }

        private static bool LongitudValida(string numero)
        {
            var normalizado = NormalizadorRastreo.Normalizar(numero);
            // Si está vacío, ya lo informa la regla NotEmpty.
            return normalizado.Length == 0 ||
                   (normalizado.Length >= NormalizadorRastreo.LongitudMinima &&
                    normalizado.Length <= NormalizadorRastreo.LongitudMaxima);
        }

        private static bool CaracteresValidos(string numero)
        {
            return NormalizadorRastreo.Normalizar(numero).All(c => char.IsLetterOrDigit(c) || c == '-');
        }
    }
}
