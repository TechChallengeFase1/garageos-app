using FluentValidation;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Enums;

namespace GarageOS.Application.Validators.OrdensDeServico;

public class AlterarStatusValidator : AbstractValidator<AlterarStatusRequest>
{
    public AlterarStatusValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => status == StatusOrdemDeServico.Finalizada || status == StatusOrdemDeServico.Entregue)
            .WithMessage("Apenas os status 'Finalizada' e 'Entregue' podem ser definidos manualmente.");
    }
}
