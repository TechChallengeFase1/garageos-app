using FluentValidation;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Enums;

namespace GarageOS.Application.Validators.OrdensDeServico;

public class AlterarStatusServicoNaOSValidator : AbstractValidator<AlterarStatusServicoNaOSRequest>
{
    public AlterarStatusServicoNaOSValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s == StatusExecucaoServico.Iniciado || s == StatusExecucaoServico.Finalizado)
            .WithMessage("O status deve ser 'Iniciado' ou 'Finalizado'.");
    }
}
