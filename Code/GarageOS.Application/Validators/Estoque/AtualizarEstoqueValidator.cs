using FluentValidation;
using GarageOS.Application.DTOs.Estoques;

namespace GarageOS.Application.Validators.Estoques;

public class AtualizarEstoqueValidator : AbstractValidator<AtualizarEstoqueRequest>
{
    public AtualizarEstoqueValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome do item é obrigatório.")
            .MaximumLength(150).WithMessage("Nome do item deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Quantidade)
            .GreaterThanOrEqualTo(0).WithMessage("Quantidade não pode ser negativa.");

        RuleFor(x => x.Valor)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero.");

        RuleFor(x => x.DataEntrada)
            .NotEmpty().WithMessage("Data de entrada é obrigatória.");

        RuleFor(x => x.Fornecedor)
            .NotEmpty().WithMessage("Fornecedor é obrigatório.")
            .MaximumLength(150).WithMessage("Fornecedor deve ter no máximo 150 caracteres.");
    }
}
