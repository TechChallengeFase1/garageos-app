using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Validators.OrdensDeServico;
using Xunit;

namespace GarageOS.UnitTests.Application.Validators.OrdensDeServico;

public class AdicionarServicoValidatorTests
{
    private readonly AdicionarServicoValidator _validator;

    public AdicionarServicoValidatorTests()
    {
        _validator = new AdicionarServicoValidator();
    }

    [Fact]
    public async Task Validate_ComServicoIdValido_DevePassarNaValidacao()
    {
        // Arrange
        var request = new AdicionarServicoRequest
        {
            ServicoId = Guid.NewGuid()
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ComServicoIdVazio_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AdicionarServicoRequest
        {
            ServicoId = Guid.Empty
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle();
        resultado.Errors.First().ErrorMessage.Should().Be("ServicoId é obrigatório.");
    }

    [Fact]
    public async Task Validate_ComServicoIdValido_NaoDeveRetornarErro()
    {
        // Arrange
        var servicoId = Guid.NewGuid();
        var request = new AdicionarServicoRequest
        {
            ServicoId = servicoId
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.Errors.FirstOrDefault(e => e.PropertyName == nameof(request.ServicoId)).Should().BeNull();
    }
}
