using FluentValidation;
using GarageOS.Application.DTOs.Clientes;

namespace GarageOS.Application.Validators.Clientes;

public class CriarClienteValidator : AbstractValidator<CriarClienteRequest>
{
    public CriarClienteValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150).WithMessage("Nome deve ter no máximo 150 caracteres.");

        RuleFor(x => x.Documento)
            .NotEmpty().WithMessage("Documento é obrigatório.")
            .Must(doc => new string(doc.Where(char.IsDigit).ToArray()).Length is 11 or 14)
            .WithMessage("Documento deve ser um CPF (11 dígitos) ou CNPJ (14 dígitos) válido.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-mail é obrigatório.")
            .EmailAddress().WithMessage("E-mail inválido.")
            .MaximumLength(200).WithMessage("E-mail deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Telefone)
            .NotEmpty().WithMessage("Telefone é obrigatório.")
            .MaximumLength(20).WithMessage("Telefone deve ter no máximo 20 caracteres.");

        RuleFor(x => x.Logradouro)
            .NotEmpty().WithMessage("Logradouro é obrigatório.")
            .MaximumLength(200).WithMessage("Logradouro deve ter no máximo 200 caracteres.");

        RuleFor(x => x.Numero)
            .NotEmpty().WithMessage("Número é obrigatório.")
            .MaximumLength(10).WithMessage("Número deve ter no máximo 10 caracteres.");

        RuleFor(x => x.Complemento)
            .MaximumLength(100).WithMessage("Complemento deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Bairro)
            .NotEmpty().WithMessage("Bairro é obrigatório.")
            .MaximumLength(100).WithMessage("Bairro deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Cidade)
            .NotEmpty().WithMessage("Cidade é obrigatória.")
            .MaximumLength(100).WithMessage("Cidade deve ter no máximo 100 caracteres.");

        RuleFor(x => x.Estado)
            .NotEmpty().WithMessage("Estado é obrigatório.")
            .Length(2).WithMessage("Estado deve ter 2 caracteres (ex: SP, RJ).");

        RuleFor(x => x.Cep)
            .NotEmpty().WithMessage("CEP é obrigatório.")
            .Must(cep => new string(cep.Where(char.IsDigit).ToArray()).Length == 8)
            .WithMessage("CEP deve conter 8 dígitos.");
    }
}