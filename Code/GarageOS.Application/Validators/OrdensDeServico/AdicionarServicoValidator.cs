using FluentValidation;
using GarageOS.Application.DTOs.OrdensDeServico;

namespace GarageOS.Application.Validators.OrdensDeServico;

public class AdicionarServicoValidator : AbstractValidator<AdicionarServicoRequest>
{
    public AdicionarServicoValidator()
    {
        RuleFor(x => x.ServicoId)
            .NotEmpty().WithMessage("ServicoId é obrigatório.");
    }
}
