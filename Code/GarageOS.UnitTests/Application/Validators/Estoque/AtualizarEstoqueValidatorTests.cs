using FluentAssertions;
using GarageOS.Application.DTOs.Estoques;
using GarageOS.Application.Validators.Estoques;

namespace GarageOS.UnitTests.Application.Validators.Estoque;

public class AtualizarEstoqueValidatorTests
{
    private readonly AtualizarEstoqueValidator _validator = new();

    [Fact]
    public async Task Validate_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var request = new AtualizarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 20,
            Valor = 200.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Pirelli"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ComNomeVazio_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AtualizarEstoqueRequest
        {
            Nome = "",
            Quantidade = 20,
            Valor = 200.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Pirelli"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Nome));
    }

    [Fact]
    public async Task Validate_ComQuantidadeNegativa_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AtualizarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = -10,
            Valor = 200.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Pirelli"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Quantidade));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100.00)]
    public async Task Validate_ComValorInvalido_DevefalharNaValidacao(decimal valor)
    {
        // Arrange
        var request = new AtualizarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 20,
            Valor = valor,
            DataEntrada = DateTime.Now,
            Fornecedor = "Pirelli"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Valor));
    }

    [Fact]
    public async Task Validate_ComFornecedorVazio_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AtualizarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 20,
            Valor = 200.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = ""
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Fornecedor));
    }
}
