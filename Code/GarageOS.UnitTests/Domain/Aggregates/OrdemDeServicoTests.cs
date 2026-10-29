using FluentAssertions;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;

namespace GarageOS.UnitTests.Domain.Aggregates;

public class OrdemDeServicoTests
{
    private static OrdemDeServico NovaOS(bool comItens = true) => new(
        "OS-2026-00001",
        Guid.NewGuid(),
        Guid.NewGuid(),
        comItens ? [(Guid.NewGuid(), "Troca de óleo", 120m)] : [],
        comItens ? [(Guid.NewGuid(), "Filtro", 40m, 2)] : [],
        "corr-1");

    private static OrdemDeServico OSEm(StatusOrdemDeServico status)
    {
        var os = NovaOS();
        var caminho = new[]
        {
            StatusOrdemDeServico.EmDiagnostico, StatusOrdemDeServico.AguardandoAprovacao,
            StatusOrdemDeServico.AguardandoPagamento, StatusOrdemDeServico.EmExecucao,
            StatusOrdemDeServico.Finalizada, StatusOrdemDeServico.Entregue
        };
        if (status == StatusOrdemDeServico.Cancelada)
        {
            os.AlterarStatus(StatusOrdemDeServico.Cancelada, "Teste");
            return os;
        }
        foreach (var passo in caminho)
        {
            if (os.Status == status) break;
            os.AlterarStatus(passo, "Teste");
        }
        return os;
    }

    [Fact]
    public void Construtor_DeveAbrirEmRecebidaComItensEHistorico()
    {
        // No monólito a abertura completa terminava em EmDiagnostico.
        var os = NovaOS();

        os.Status.Should().Be(StatusOrdemDeServico.Recebida);
        os.Servicos.Should().ContainSingle(s => s.Nome == "Troca de óleo" && s.PrecoUnitario == 120m);
        os.Pecas.Should().ContainSingle(p => p.Quantidade == 2 && p.Subtotal == 80m);
        os.ValorEstimado.Should().Be(200m);
        os.Historico.Should().ContainSingle()
            .Which.Should().Match<HistoricoStatusOS>(h => h.StatusAnterior == null
                && h.StatusNovo == StatusOrdemDeServico.Recebida && h.CorrelationId == "corr-1");
    }

    [Theory]
    [InlineData("", false, false)]
    [InlineData("OS-1", true, false)]
    [InlineData("OS-1", false, true)]
    public void Construtor_DadosObrigatorios_DeveLancar(string numero, bool semCliente, bool semVeiculo)
    {
        var acao = () => new OrdemDeServico(numero, semCliente ? Guid.Empty : Guid.NewGuid(),
            semVeiculo ? Guid.Empty : Guid.NewGuid(), [], []);

        acao.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(StatusOrdemDeServico.Recebida, StatusOrdemDeServico.EmDiagnostico, true)]
    [InlineData(StatusOrdemDeServico.Recebida, StatusOrdemDeServico.Cancelada, true)]
    [InlineData(StatusOrdemDeServico.Recebida, StatusOrdemDeServico.EmExecucao, false)]
    [InlineData(StatusOrdemDeServico.EmDiagnostico, StatusOrdemDeServico.AguardandoAprovacao, true)]
    [InlineData(StatusOrdemDeServico.EmDiagnostico, StatusOrdemDeServico.Finalizada, false)]
    [InlineData(StatusOrdemDeServico.AguardandoAprovacao, StatusOrdemDeServico.AguardandoPagamento, true)]
    [InlineData(StatusOrdemDeServico.AguardandoAprovacao, StatusOrdemDeServico.EmExecucao, false)]
    [InlineData(StatusOrdemDeServico.AguardandoPagamento, StatusOrdemDeServico.EmExecucao, true)]
    [InlineData(StatusOrdemDeServico.AguardandoPagamento, StatusOrdemDeServico.Cancelada, true)]
    [InlineData(StatusOrdemDeServico.EmExecucao, StatusOrdemDeServico.Finalizada, true)]
    [InlineData(StatusOrdemDeServico.EmExecucao, StatusOrdemDeServico.Entregue, false)]
    [InlineData(StatusOrdemDeServico.Finalizada, StatusOrdemDeServico.Entregue, true)]
    [InlineData(StatusOrdemDeServico.Finalizada, StatusOrdemDeServico.Cancelada, false)]
    [InlineData(StatusOrdemDeServico.Entregue, StatusOrdemDeServico.Cancelada, false)]
    [InlineData(StatusOrdemDeServico.Cancelada, StatusOrdemDeServico.Recebida, false)]
    public void TransicaoPermitida_DeveSeguirATabela(StatusOrdemDeServico de, StatusOrdemDeServico para, bool esperado)
    {
        OrdemDeServico.TransicaoPermitida(de, para).Should().Be(esperado);
    }

    [Fact]
    public void AlterarStatus_CaminhoCompleto_DeveRegistrarCadaPassoNoHistorico()
    {
        var os = OSEm(StatusOrdemDeServico.Entregue);

        os.Status.Should().Be(StatusOrdemDeServico.Entregue);
        os.FinalizadaEm.Should().NotBeNull();
        os.Historico.Select(h => h.StatusNovo).Should().Equal(
            StatusOrdemDeServico.Recebida, StatusOrdemDeServico.EmDiagnostico, StatusOrdemDeServico.AguardandoAprovacao,
            StatusOrdemDeServico.AguardandoPagamento, StatusOrdemDeServico.EmExecucao,
            StatusOrdemDeServico.Finalizada, StatusOrdemDeServico.Entregue);
    }

    [Fact]
    public void AlterarStatus_TransicaoInvalida_DeveLancarENaoMudarNada()
    {
        // OrdemDeServicoStatusInvalidoException existia desde a Fase 1 e nunca era lançada.
        var os = NovaOS();

        var acao = () => os.AlterarStatus(StatusOrdemDeServico.Finalizada, "Manual");

        acao.Should().Throw<OrdemDeServicoStatusInvalidoException>();
        os.Status.Should().Be(StatusOrdemDeServico.Recebida);
        os.Historico.Should().HaveCount(1);
    }

    [Fact]
    public void AlterarStatus_Cancelada_DeveMarcarFinalizacao()
    {
        var os = OSEm(StatusOrdemDeServico.AguardandoPagamento);

        os.AlterarStatus(StatusOrdemDeServico.Cancelada, "Saga", "msg-9");

        os.Status.Should().Be(StatusOrdemDeServico.Cancelada);
        os.FinalizadaEm.Should().NotBeNull();
        os.Historico.Last().Should().Match<HistoricoStatusOS>(h =>
            h.Origem == "Saga" && h.CorrelationId == "msg-9" && h.StatusAnterior == StatusOrdemDeServico.AguardandoPagamento);
    }

    [Fact]
    public void AdicionarServico_EmRecebida_DeveIniciarDiagnostico()
    {
        var os = NovaOS(comItens: false);

        os.AdicionarServico(Guid.NewGuid(), "Alinhamento", 80m);

        os.Status.Should().Be(StatusOrdemDeServico.EmDiagnostico);
        os.Historico.Last().Origem.Should().Be(OrdemDeServico.OrigemSistema);
    }

    [Fact]
    public void AdicionarPeca_EmDiagnostico_DeveManterStatus()
    {
        var os = OSEm(StatusOrdemDeServico.EmDiagnostico);

        os.AdicionarPeca(Guid.NewGuid(), "Pastilha", 90m, 1);

        os.Status.Should().Be(StatusOrdemDeServico.EmDiagnostico);
        os.Pecas.Should().HaveCount(2);
        os.Historico.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(StatusOrdemDeServico.AguardandoAprovacao)]
    [InlineData(StatusOrdemDeServico.EmExecucao)]
    [InlineData(StatusOrdemDeServico.Cancelada)]
    public void AdicionarItens_DepoisDoDiagnostico_DeveLancar(StatusOrdemDeServico status)
    {
        var os = OSEm(status);

        var servico = () => os.AdicionarServico(Guid.NewGuid(), "Lavagem", 30m);
        var peca = () => os.AdicionarPeca(Guid.NewGuid(), "Lâmpada", 15m, 1);

        servico.Should().Throw<OrdemDeServicoStatusInvalidoException>();
        peca.Should().Throw<OrdemDeServicoStatusInvalidoException>();
    }

    [Theory]
    [InlineData(true, "Nome", 1, 1)]
    [InlineData(false, "", 1, 1)]
    [InlineData(false, "Nome", -1, 1)]
    [InlineData(false, "Nome", 1, 0)]
    public void Peca_Invalida_DeveLancar(bool semEstoque, string nome, decimal valor, int quantidade)
    {
        var acao = () => new OrdemDeServicoPeca(Guid.NewGuid(), semEstoque ? Guid.Empty : Guid.NewGuid(), nome, valor, quantidade);

        acao.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(true, "Nome", 1)]
    [InlineData(false, "", 1)]
    [InlineData(false, "Nome", -1)]
    public void Servico_Invalido_DeveLancar(bool semServico, string nome, decimal preco)
    {
        var acao = () => new OrdemDeServicoServico(Guid.NewGuid(), semServico ? Guid.Empty : Guid.NewGuid(), nome, preco);

        acao.Should().Throw<ArgumentException>();
    }
}
