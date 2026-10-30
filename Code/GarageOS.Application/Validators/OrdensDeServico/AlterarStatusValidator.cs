using FluentValidation;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Enums;

namespace GarageOS.Application.Validators.OrdensDeServico;

public class AlterarStatusValidator : AbstractValidator<AlterarStatusRequest>
{
    public AlterarStatusValidator()
    {
        // Se a transição é permitida a partir do status atual, quem decide é a
        // própria OS. Aqui só se barra o que nunca é destino de uma mudança.
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Status inválido.")
            .NotEqual(StatusOrdemDeServico.Recebida).WithMessage("Uma OS não volta para 'Recebida'.");
    }
}
