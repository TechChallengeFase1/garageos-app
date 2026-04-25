using FluentAssertions;
using GarageOS.Application.DTOs.Servicos;
using GarageOS.Application.Validators.Servicos;

namespace GarageOS.UnitTests.Application.Validators.Servicos;

public class CriarServicoValidatorTests
{
    private readonly CriarServicoValidator _validator = new();

    [Fact]
    public async Task Validate_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarServicoRequest
        {
            NomeServico = "Troca de Óleo",
            Preco = 150.00m
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
        var request = new CriarServicoRequest
        {
            NomeServico = "",
            Preco = 150.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.NomeServico));
    }

    [Fact]
    public async Task Validate_ComNomeMuitoLongo_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarServicoRequest
        {
            NomeServico = new string('a', 101),
            Preco = 150.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.NomeServico));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100.00)]
    [InlineData(-0.01)]
    public async Task Validate_ComPrecoZeroOuNegativo_DevefalharNaValidacao(decimal preco)
    {
        // Arrange
        var request = new CriarServicoRequest
        {
            NomeServico = "Troca de Óleo",
            Preco = preco
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Preco));
    }

    [Fact]
    public async Task Validate_ComPrecoMuitoAlto_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarServicoRequest
        {
            NomeServico = "Serviço Premium",
            Preco = 999999.99m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ComPrecoMuintoPequeno_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarServicoRequest
        {
            NomeServico = "Serviço",
            Preco = 0.01m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
