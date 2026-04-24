using FluentValidation;
using GarageOS.Application.DTOs.OrdensDeServico;

namespace GarageOS.Application.Validators.OrdensDeServico;

public class CriarOrdemDeServicoValidator : AbstractValidator<CriarOrdemDeServicoRequest>
{
    public CriarOrdemDeServicoValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("ClienteId é obrigatório.");

        RuleFor(x => x.VeiculoId)
            .NotEmpty().WithMessage("VeiculoId é obrigatório.");
    }
}
