using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Validators.OrdensDeServico;
using GarageOS.Domain.Enums;

namespace GarageOS.UnitTests.Application.Validators.OrdensDeServico;

public class ValidatorsDaOSTests
{
    [Fact]
    public void Abrir_Valido_DevePassar()
    {
        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = Guid.NewGuid(),
            VeiculoId = Guid.NewGuid(),
            ServicosIds = [Guid.NewGuid()],
            Pecas = []
        };

        new AbrirOrdemDeServicoCompletaValidator().Validate(request).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Abrir_Invalido_DeveApontarCadaErro()
    {
        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ServicosIds = [],
            Pecas = [new PecaRequest { EstoqueId = Guid.Empty, Quantidade = 0 }]
        };

        var erros = new AbrirOrdemDeServicoCompletaValidator().Validate(request).Errors.Select(e => e.ErrorMessage);

        erros.Should().Contain([
            "ClienteId é obrigatório.", "VeiculoId é obrigatório.", "Ao menos um serviço é obrigatório.",
            "EstoqueId é obrigatório.", "Quantidade deve ser maior que zero."
        ]);
    }

    [Fact]
    public void Abrir_SemListaDePecas_DeveFalhar()
    {
        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = Guid.NewGuid(), VeiculoId = Guid.NewGuid(), ServicosIds = [Guid.NewGuid()], Pecas = null
        };

        new AbrirOrdemDeServicoCompletaValidator().Validate(request).IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    public void AdicionarPeca_DeveExigirQuantidade(int quantidade, bool valido)
    {
        var request = new AdicionarPecaRequest { EstoqueId = Guid.NewGuid(), Quantidade = quantidade };

        new AdicionarPecaValidator().Validate(request).IsValid.Should().Be(valido);
    }

    [Theory]
    [InlineData(StatusOrdemDeServico.Entregue, true)]
    [InlineData(StatusOrdemDeServico.Cancelada, false)]
    [InlineData(StatusOrdemDeServico.AguardandoPagamento, false)]
    [InlineData(StatusOrdemDeServico.Recebida, false)]
    public void AlterarStatus_ManualSoAEntrega(StatusOrdemDeServico status, bool valido)
    {
        new AlterarStatusValidator().Validate(new AlterarStatusRequest { Status = status }).IsValid.Should().Be(valido);
    }
}
