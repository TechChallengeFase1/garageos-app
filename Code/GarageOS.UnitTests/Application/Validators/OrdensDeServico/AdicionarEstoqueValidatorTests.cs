using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Validators.OrdensDeServico;
using Xunit;

namespace GarageOS.UnitTests.Application.Validators.OrdensDeServico;

public class AdicionarEstoqueValidatorTests
{
    private readonly AdicionarEstoqueValidator _validator;

    public AdicionarEstoqueValidatorTests()
    {
        _validator = new AdicionarEstoqueValidator();
    }

    [Fact]
    public async Task Validate_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = Guid.NewGuid(),
            Quantidade = 5
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ComEstoqueIdVazio_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = Guid.Empty,
            Quantidade = 5
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle();
        resultado.Errors.First().ErrorMessage.Should().Be("EstoqueId é obrigatório.");
    }

    [Fact]
    public async Task Validate_ComQuantidadeZero_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = Guid.NewGuid(),
            Quantidade = 0
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle();
        resultado.Errors.First().ErrorMessage.Should().Be("Quantidade deve ser maior que zero.");
    }

    [Fact]
    public async Task Validate_ComQuantidadeNegativa_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = Guid.NewGuid(),
            Quantidade = -5
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle();
        resultado.Errors.First().ErrorMessage.Should().Be("Quantidade deve ser maior que zero.");
    }

    [Fact]
    public async Task Validate_ComQuantidadePositiva_NaoDeveRetornarErro()
    {
        // Arrange
        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = Guid.NewGuid(),
            Quantidade = 1
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.Errors.FirstOrDefault(e => e.PropertyName == nameof(request.Quantidade)).Should().BeNull();
    }

    [Fact]
    public async Task Validate_ComAmbosCamposInvalidos_DevefalharComDoisErros()
    {
        // Arrange
        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = Guid.Empty,
            Quantidade = 0
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Validate_ComQuantidadeGrande_DevePassarNaValidacao()
    {
        // Arrange
        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = Guid.NewGuid(),
            Quantidade = 1000000
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }
}
