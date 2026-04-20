using FluentValidation;
using GarageOS.Application.DTOs.Servicos;

namespace GarageOS.Application.Validators.Servicos;

public class AtualizarServicoValidator : AbstractValidator<AtualizarServicoRequest>
{
    public AtualizarServicoValidator()
    {
        RuleFor(x => x.NomeServico)
            .NotEmpty().WithMessage("Nome do serviço é obrigatório.")
            .MaximumLength(100).WithMessage("Nome do serviço deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Preco)
            .GreaterThan(0).WithMessage("Preço deve ser maior que zero.");
    }
}
