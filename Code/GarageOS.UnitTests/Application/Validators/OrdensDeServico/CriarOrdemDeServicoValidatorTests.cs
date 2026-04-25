using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Validators.OrdensDeServico;
using Xunit;

namespace GarageOS.UnitTests.Application.Validators.OrdensDeServico;

public class CriarOrdemDeServicoValidatorTests
{
    private readonly CriarOrdemDeServicoValidator _validator;

    public CriarOrdemDeServicoValidatorTests()
    {
        _validator = new CriarOrdemDeServicoValidator();
    }

    [Fact]
    public async Task Validate_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = Guid.NewGuid(),
            VeiculoId = Guid.NewGuid()
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ComClienteIdVazio_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = Guid.Empty,
            VeiculoId = Guid.NewGuid()
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle();
        resultado.Errors.First().ErrorMessage.Should().Be("ClienteId é obrigatório.");
    }

    [Fact]
    public async Task Validate_ComVeiculoIdVazio_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = Guid.NewGuid(),
            VeiculoId = Guid.Empty
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle();
        resultado.Errors.First().ErrorMessage.Should().Be("VeiculoId é obrigatório.");
    }

    [Fact]
    public async Task Validate_ComAmbosCamposVazios_DevefalharComDoisErros()
    {
        // Arrange
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = Guid.Empty,
            VeiculoId = Guid.Empty
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Validate_ComClienteIdValido_NaoDeveRetornarErro()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = clienteId,
            VeiculoId = Guid.NewGuid()
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.Errors.FirstOrDefault(e => e.PropertyName == nameof(request.ClienteId)).Should().BeNull();
    }
}
