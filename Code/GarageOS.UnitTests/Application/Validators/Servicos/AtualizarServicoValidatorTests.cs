using FluentAssertions;
using GarageOS.Application.DTOs.Servicos;
using GarageOS.Application.Validators.Servicos;

namespace GarageOS.UnitTests.Application.Validators.Servicos;

public class AtualizarServicoValidatorTests
{
    private readonly AtualizarServicoValidator _validator = new();

    [Fact]
    public async Task Validate_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var request = new AtualizarServicoRequest
        {
            NomeServico = "Revisão Completa",
            Preco = 500.00m
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
        var request = new AtualizarServicoRequest
        {
            NomeServico = "",
            Preco = 500.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.NomeServico));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50.00)]
    public async Task Validate_ComPrecoInvalido_DevefalharNaValidacao(decimal preco)
    {
        // Arrange
        var request = new AtualizarServicoRequest
        {
            NomeServico = "Serviço",
            Preco = preco
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Preco));
    }

    [Fact]
    public async Task Validate_ComNomeNoLimiteMaximo_DevePassarNaValidacao()
    {
        // Arrange
        var request = new AtualizarServicoRequest
        {
            NomeServico = new string('a', 100),
            Preco = 100.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
