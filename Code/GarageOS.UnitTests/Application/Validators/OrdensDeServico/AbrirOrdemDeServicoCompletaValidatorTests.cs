using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Validators.OrdensDeServico;
using Xunit;

namespace GarageOS.UnitTests.Application.Validators.OrdensDeServico;

public class AbrirOrdemDeServicoCompletaValidatorTests
{
    private readonly AbrirOrdemDeServicoCompletaValidator _validator;

    public AbrirOrdemDeServicoCompletaValidatorTests()
    {
        _validator = new AbrirOrdemDeServicoCompletaValidator();
    }

    private static AbrirOrdemDeServicoCompletaRequest CriarRequestValido()
    {
        return new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = Guid.NewGuid(),
            VeiculoId = Guid.NewGuid(),
            ServicosIds = [Guid.NewGuid()],
            Pecas = []
        };
    }

    [Fact]
    public async Task Validate_ComDadosValidosEPecasVazia_DevePassarNaValidacao()
    {
        // Arrange (AC1 - Pecas como lista vazia explícita deve ser aceita)
        var request = CriarRequestValido();

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeTrue();
        resultado.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ComServicosIdsVazio_DeveFalharComMensagemDeServicoObrigatorio()
    {
        // Arrange (AC4)
        var request = CriarRequestValido();
        request.ServicosIds = [];

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage == "Ao menos um serviço é obrigatório.");
    }

    [Fact]
    public async Task Validate_ComPecasNula_DeveFalharNaValidacao()
    {
        // Arrange (AC3 - campo Pecas ausente/null deve ser rejeitado)
        var request = CriarRequestValido();
        request.Pecas = null;

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage == "O campo Pecas é obrigatório.");
    }

    [Fact]
    public async Task Validate_ComPecasVaziaNaoNula_DevePassarNaValidacao()
    {
        // Arrange (AC3 - [] deve passar na regra NotNull, distinto de null)
        var request = CriarRequestValido();
        request.Pecas = [];

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ComQuantidadeMenorOuIgualAZero_DeveFalharComMensagemDeQuantidade()
    {
        // Arrange (AC8 - quantidade <= 0)
        var request = CriarRequestValido();
        request.Pecas = [new PecaRequest { EstoqueId = Guid.NewGuid(), Quantidade = 0 }];

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage == "Quantidade deve ser maior que zero.");
    }

    [Fact]
    public async Task Validate_ComEstoqueIdVazioEmPeca_DeveFalharComMensagemDeEstoqueObrigatorio()
    {
        // Arrange (AC8 - EstoqueId ausente/vazio)
        var request = CriarRequestValido();
        request.Pecas = [new PecaRequest { EstoqueId = Guid.Empty, Quantidade = 1 }];

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage == "EstoqueId é obrigatório.");
    }

    [Fact]
    public async Task Validate_ComClienteIdVazio_DeveFalharComMensagemDeClienteObrigatorio()
    {
        // Arrange
        var request = CriarRequestValido();
        request.ClienteId = Guid.Empty;

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage == "ClienteId é obrigatório.");
    }

    [Fact]
    public async Task Validate_ComVeiculoIdVazio_DeveFalharComMensagemDeVeiculoObrigatorio()
    {
        // Arrange
        var request = CriarRequestValido();
        request.VeiculoId = Guid.Empty;

        // Act
        var resultado = await _validator.ValidateAsync(request);

        // Assert
        resultado.IsValid.Should().BeFalse();
        resultado.Errors.Should().Contain(e => e.ErrorMessage == "VeiculoId é obrigatório.");
    }
}
