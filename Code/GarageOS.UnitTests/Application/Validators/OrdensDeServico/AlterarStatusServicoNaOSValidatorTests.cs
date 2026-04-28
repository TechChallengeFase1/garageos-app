using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Validators.OrdensDeServico;
using GarageOS.Domain.Enums;
using Xunit;

namespace GarageOS.UnitTests.Application.Validators.OrdensDeServico;

public class AlterarStatusServicoNaOSValidatorTests
{
    private readonly AlterarStatusServicoNaOSValidator _validator = new();

    [Fact]
    public async Task Validate_ComStatusIniciado_DevePassarNaValidacao()
    {
        var request = new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado };

        var resultado = await _validator.ValidateAsync(request);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ComStatusFinalizado_DevePassarNaValidacao()
    {
        var request = new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Finalizado };

        var resultado = await _validator.ValidateAsync(request);

        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ComStatusCriada_DeveFalharNaValidacao()
    {
        var request = new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Criada };

        var resultado = await _validator.ValidateAsync(request);

        resultado.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_ComStatusCriada_DeveTerMensagemCorreta()
    {
        var request = new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Criada };

        var resultado = await _validator.ValidateAsync(request);

        resultado.Errors.Should().ContainSingle(e =>
            e.ErrorMessage.Contains("Iniciado") && e.ErrorMessage.Contains("Finalizado"));
    }
}
