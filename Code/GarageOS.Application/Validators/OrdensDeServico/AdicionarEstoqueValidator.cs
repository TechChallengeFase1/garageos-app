using FluentValidation;
using GarageOS.Application.DTOs.OrdensDeServico;

namespace GarageOS.Application.Validators.OrdensDeServico;

public class AdicionarEstoqueValidator : AbstractValidator<AdicionarEstoqueRequest>
{
    public AdicionarEstoqueValidator()
    {
        RuleFor(x => x.EstoqueId)
            .NotEmpty().WithMessage("EstoqueId é obrigatório.");

        RuleFor(x => x.Quantidade)
            .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");
    }
}
