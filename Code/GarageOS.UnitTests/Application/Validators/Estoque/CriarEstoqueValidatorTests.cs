using FluentAssertions;
using GarageOS.Application.DTOs.Estoques;
using GarageOS.Application.Validators.Estoques;

namespace GarageOS.UnitTests.Application.Validators.Estoque;

public class CriarEstoqueValidatorTests
{
    private readonly CriarEstoqueValidator _validator = new();

    [Fact]
    public async Task Validate_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 10,
            Valor = 150.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Goodyear"
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
        var request = new CriarEstoqueRequest
        {
            Nome = "",
            Quantidade = 10,
            Valor = 150.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Goodyear"
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
        var request = new CriarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = -5,
            Valor = 150.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Goodyear"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Quantidade));
    }

    [Fact]
    public async Task Validate_ComQuantidadeZero_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 0,
            Valor = 150.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Goodyear"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50.00)]
    public async Task Validate_ComValorInvalido_DevefalharNaValidacao(decimal valor)
    {
        // Arrange
        var request = new CriarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 10,
            Valor = valor,
            DataEntrada = DateTime.Now,
            Fornecedor = "Goodyear"
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
        var request = new CriarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 10,
            Valor = 150.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = ""
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Fornecedor));
    }

    [Fact]
    public async Task Validate_ComQuantidadeAlta_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 999999,
            Valor = 150.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Goodyear"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
