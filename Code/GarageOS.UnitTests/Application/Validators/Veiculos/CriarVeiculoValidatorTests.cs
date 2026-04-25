using FluentAssertions;
using GarageOS.Application.DTOs.Veiculos;
using GarageOS.Application.Validators.Veiculos;

namespace GarageOS.UnitTests.Application.Validators.Veiculos;

public class CriarVeiculoValidatorTests
{
    private readonly CriarVeiculoValidator _validator = new();

    [Fact]
    public async Task Validate_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Toyota",
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = "ABC1234",
            AnoVeiculo = 2022,
            PrecoVeiculo = 95000.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ComMarcaVazia_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "",
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = "ABC1234",
            AnoVeiculo = 2022,
            PrecoVeiculo = 95000.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.MarcaVeiculo));
    }

    [Fact]
    public async Task Validate_ComMarcaMuitoLonga_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = new string('a', 101),
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = "ABC1234",
            AnoVeiculo = 2022,
            PrecoVeiculo = 95000.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.MarcaVeiculo));
    }

    [Fact]
    public async Task Validate_ComPlacaDiferenteDeSeteCaracteres_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Toyota",
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = "ABC12",
            AnoVeiculo = 2022,
            PrecoVeiculo = 95000.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.PlacaVeiculo));
    }

    [Fact]
    public async Task Validate_ComAnoMuitoAntigo_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Ford",
            ModeloVeiculo = "Model T",
            PlacaVeiculo = "ABC1234",
            AnoVeiculo = 1899,
            PrecoVeiculo = 50000.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.AnoVeiculo));
    }

    [Fact]
    public async Task Validate_ComAnoFuturo_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Toyota",
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = "ABC1234",
            AnoVeiculo = DateTime.Now.Year + 1,
            PrecoVeiculo = 95000.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.AnoVeiculo));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1000.00)]
    public async Task Validate_ComPrecoZeroOuNegativo_DevefalharNaValidacao(decimal preco)
    {
        // Arrange
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Toyota",
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = "ABC1234",
            AnoVeiculo = 2022,
            PrecoVeiculo = preco
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.PrecoVeiculo));
    }

    [Fact]
    public async Task Validate_ComPlacaMercosul_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Toyota",
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = "ABC1A23",
            AnoVeiculo = 2022,
            PrecoVeiculo = 95000.00m
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
