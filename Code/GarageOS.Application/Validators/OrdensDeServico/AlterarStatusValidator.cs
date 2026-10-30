using FluentValidation;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Domain.Enums;

namespace GarageOS.Application.Validators.OrdensDeServico;

public class AlterarStatusValidator : AbstractValidator<AlterarStatusRequest>
{
    public AlterarStatusValidator()
    {
        // Da solicitação do orçamento até a finalização, quem move a OS é a
        // saga. Mudar o status à mão no meio dela deixaria reserva de peças e
        // cobrança sem compensação. Sobra para a equipe a entrega do veículo.
        RuleFor(x => x.Status)
            .Equal(StatusOrdemDeServico.Entregue)
            .WithMessage("Manualmente, só a entrega ('Entregue') pode ser registrada. As demais etapas são conduzidas pela saga.");
    }
}
