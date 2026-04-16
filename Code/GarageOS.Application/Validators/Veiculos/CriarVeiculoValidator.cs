using FluentValidation;
using GarageOS.Application.DTOs.Veiculos;

namespace GarageOS.Application.Validators.Veiculos;

public class CriarVeiculoValidator : AbstractValidator<CriarVeiculoRequest>
{
    public CriarVeiculoValidator()
    {
        RuleFor(x => x.MarcaVeiculo)
            .NotEmpty().WithMessage("Marca é obrigatória")
            .MaximumLength(100).WithMessage("Marca deve ter no máximo 100 caracteres");

        RuleFor(x => x.ModeloVeiculo)
            .NotEmpty().WithMessage("Modelo é obrigatório")
            .MaximumLength(100).WithMessage("Modelo deve ter no máximo 100 caracteres");

        RuleFor(x => x.PlacaVeiculo)
            .NotEmpty().WithMessage("Placa é obrigatória")
            .Length(7).WithMessage("Placa deve ter 7 caracteres");

        RuleFor(x => x.AnoVeiculo)
            .InclusiveBetween(1900, DateTime.Now.Year)
            .WithMessage("Ano inválido");

        RuleFor(x => x.PrecoVeiculo)
            .GreaterThan(0)
            .WithMessage("Preço deve ser maior que zero");
    }
}