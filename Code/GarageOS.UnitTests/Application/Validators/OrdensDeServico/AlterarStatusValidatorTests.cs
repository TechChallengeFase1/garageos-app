using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Validators.OrdensDeServico;
using GarageOS.Domain.Enums;
using Xunit;

namespace GarageOS.UnitTests.Application.Validators.OrdensDeServico;

public class AlterarStatusValidatorTests
{
    private readonly AlterarStatusValidator _validator;

    public AlterarStatusValidatorTests()
    {
        _validator = new AlterarStatusValidator();
    }

    [Fact]
    public async Task Validate_ComStatusFinalizada_DevePassarNaValidacao()
    {
        // Arrange
        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Finalizada
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ComStatusEntregue_DevePassarNaValidacao()
    {
        // Arrange
        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Entregue
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ComStatusRecebida_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Recebida
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Validate_ComStatusEmDiagnostico_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.EmDiagnostico
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Validate_ComStatusAguardandoAprovacao_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.AguardandoAprovacao
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Validate_ComStatusEmExecucao_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.EmExecucao
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Validate_ComStatusNaoPermitido_DeveRetornarMensagemEspecifica()
    {
        // Arrange
        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Recebida
        };

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.Errors.First().ErrorMessage.Should()
            .Be("Apenas os status 'Finalizada' e 'Entregue' podem ser definidos manualmente.");
    }

    [Fact]
    public async Task Validate_ComFinalizadaEEntregue_AmbosDevemPassar()
    {
        // Arrange & Act & Assert
        var requestFinalizada = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Finalizada
        };
        var resultadoFinalizada = await _validator.ValidateAsync(requestFinalizada);

        var requestEntregue = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Entregue
        };
        var resultadoEntregue = await _validator.ValidateAsync(requestEntregue);

        resultadoFinalizada.IsValid.Should().BeTrue();
        resultadoEntregue.IsValid.Should().BeTrue();
    }
}
