using FluentValidation;
using GarageOS.Application.DTOs.OrdensDeServico;

namespace GarageOS.Application.Validators.OrdensDeServico;

public class AbrirOrdemDeServicoCompletaValidator : AbstractValidator<AbrirOrdemDeServicoCompletaRequest>
{
    public AbrirOrdemDeServicoCompletaValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("ClienteId é obrigatório.");

        RuleFor(x => x.VeiculoId)
            .NotEmpty().WithMessage("VeiculoId é obrigatório.");

        RuleFor(x => x.ServicosIds)
            .NotEmpty().WithMessage("Ao menos um serviço é obrigatório.");

        RuleForEach(x => x.ServicosIds)
            .NotEmpty().WithMessage("ServicoId é obrigatório.");

        RuleFor(x => x.Pecas)
            .NotNull().WithMessage("O campo Pecas é obrigatório.");

        RuleForEach(x => x.Pecas)
            .ChildRules(peca =>
            {
                peca.RuleFor(p => p.EstoqueId)
                    .NotEmpty().WithMessage("EstoqueId é obrigatório.");

                peca.RuleFor(p => p.Quantidade)
                    .GreaterThan(0).WithMessage("Quantidade deve ser maior que zero.");
            });
    }
}
