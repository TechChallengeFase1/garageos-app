using FluentAssertions;
using GarageOS.Application.Abstractions;
using GarageOS.Contracts.V1;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Repositories;
using GarageOS.Infrastructure.Saga;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace GarageOS.UnitTests.Infrastructure.Saga;

/// <summary>A saga da OS no test harness do MassTransit, com transporte em memória</summary>
/// <remarks>
/// Os outros serviços são simulados publicando os eventos que eles publicariam.
/// A OS real (agregado de domínio) é usada, com o repositório em memória, para
/// conferir que a saga leva o status até onde deveria.
/// </remarks>
public class OrdemDeServicoSagaTests : IAsyncLifetime
{
    private readonly Mock<IOrdemDeServicoRepository> _repositorio = new();
    private readonly Mock<IMetricasDeNegocio> _metricas = new();
    private readonly Dictionary<Guid, OrdemDeServico> _ordens = [];

    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private ISagaStateMachineTestHarness<OrdemDeServicoStateMachine, OrdemDeServicoSagaState> _saga = null!;
    private OrdemDeServicoStateMachine _maquina = null!;

    public async Task InitializeAsync()
    {
        _repositorio.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => _ordens.GetValueOrDefault(id));

        _provider = new ServiceCollection()
            .AddSingleton(_repositorio.Object)
            .AddSingleton(_metricas.Object)
            .Configure<SagaOptions>(o => { o.PrazoAprovacaoMinutos = 5; o.PrazoPagamentoMinutos = 5; })
            .AddMassTransitTestHarness(x =>
            {
                x.AddDelayedMessageScheduler();
                x.AddSagaStateMachine<OrdemDeServicoStateMachine, OrdemDeServicoSagaState>().InMemoryRepository();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseDelayedMessageScheduler();
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        _harness.TestTimeout = TimeSpan.FromSeconds(10);
        await _harness.Start();
        _saga = _harness.GetSagaStateMachineHarness<OrdemDeServicoStateMachine, OrdemDeServicoSagaState>();
        _maquina = _saga.StateMachine;
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    private OrdemDeServico NovaOS(bool comPecas)
    {
        var os = new OrdemDeServico("OS-2026-00077", Guid.NewGuid(), Guid.NewGuid(),
            [(Guid.NewGuid(), "Revisão", 200m)],
            comPecas ? [(Guid.NewGuid(), "Filtro", 30m, 2)] : []);
        _ordens[os.Id] = os;
        return os;
    }

    private async Task PedirOrcamento(OrdemDeServico os)
    {
        await _harness.Bus.Publish(new OrdemDeServicoAberta(os.Id, os.NumeroOS, os.ClienteId));
        await _harness.Bus.Publish(new OrcamentoSolicitado(os.Id, os.NumeroOS, os.ClienteId,
            os.Servicos.Select(s => new ItemDoPedido("Servico", s.ServicoId, s.Nome, 1, s.PrecoUnitario))
                .Concat(os.Pecas.Select(p => new ItemDoPedido("Peca", p.EstoqueId, p.Nome, p.Quantidade, p.ValorUnitario))).ToList(),
            os.Pecas.Select(p => new PecaDoPedido(p.EstoqueId, p.Quantidade)).ToList(),
            os.Servicos.Select(s => new ServicoDoPedido(s.ServicoId, s.Nome)).ToList()));
    }

    private async Task Etapa(Guid id, Func<OrdemDeServicoStateMachine, State> estado)
    {
        var achou = await _saga.Exists(id, estado);
        achou.Should().NotBeNull($"a saga devia chegar a {estado(_maquina).Name}");
    }

    private async Task<Guid> AteAguardandoAprovacao(OrdemDeServico os)
    {
        await PedirOrcamento(os);
        await Etapa(os.Id, m => m.AguardandoOrcamento);
        (await _harness.Published.Any<GerarOrcamento>(m => m.Context.Message.OrdemDeServicoId == os.Id)).Should().BeTrue();

        var orcamentoId = Guid.NewGuid();
        await _harness.Bus.Publish(new OrcamentoEnviado(os.Id, orcamentoId, 260m));
        await Etapa(os.Id, m => m.AguardandoAprovacao);
        return orcamentoId;
    }

    private async Task<(Guid Orcamento, Guid Pagamento)> AteEmExecucao(OrdemDeServico os)
    {
        var orcamentoId = await AteAguardandoAprovacao(os);
        await _harness.Bus.Publish(new OrcamentoAprovado(os.Id, orcamentoId));
        await Etapa(os.Id, m => m.ReservandoPecas);
        await _harness.Bus.Publish(new PecasReservadas(os.Id));
        await Etapa(os.Id, m => m.AguardandoPagamento);
        var pagamentoId = Guid.NewGuid();
        await _harness.Bus.Publish(new PagamentoConfirmado(os.Id, orcamentoId, pagamentoId));
        await Etapa(os.Id, m => m.EmExecucao);
        return (orcamentoId, pagamentoId);
    }

    [Fact]
    public async Task CaminhoFeliz_ComPecas_DeveLevarAOSAteFinalizada()
    {
        var os = NovaOS(comPecas: true);

        await AteEmExecucao(os);
        (await _harness.Published.Any<ReservarPecas>(m => m.Context.Message.Pecas.Count == 1)).Should().BeTrue();
        (await _harness.Published.Any<SolicitarPagamento>()).Should().BeTrue();
        (await _harness.Published.Any<IniciarExecucao>(m => m.Context.Message.Servicos.Single().Nome == "Revisão")).Should().BeTrue();

        await _harness.Bus.Publish(new ExecucaoFinalizada(os.Id));
        await Etapa(os.Id, m => m.Finalizada);

        os.Status.Should().Be(StatusOrdemDeServico.Finalizada);
        os.Historico.Where(h => h.Origem == "Saga").Select(h => h.StatusNovo).Should().Equal(
            StatusOrdemDeServico.EmDiagnostico, StatusOrdemDeServico.AguardandoAprovacao, StatusOrdemDeServico.AguardandoPagamento,
            StatusOrdemDeServico.EmExecucao, StatusOrdemDeServico.Finalizada);
    }

    [Fact]
    public async Task SemPecas_DevePularAReserva()
    {
        var os = NovaOS(comPecas: false);
        var orcamentoId = await AteAguardandoAprovacao(os);

        await _harness.Bus.Publish(new OrcamentoAprovado(os.Id, orcamentoId));

        await Etapa(os.Id, m => m.AguardandoPagamento);
        (await _harness.Published.Any<ReservarPecas>()).Should().BeFalse();
        os.Status.Should().Be(StatusOrdemDeServico.AguardandoPagamento);
    }

    [Fact]
    public async Task OrcamentoRecusado_DeveCancelarSemCompensacao()
    {
        var os = NovaOS(comPecas: true);
        var orcamentoId = await AteAguardandoAprovacao(os);

        await _harness.Bus.Publish(new OrcamentoRecusado(os.Id, orcamentoId));

        await Etapa(os.Id, m => m.Cancelada);
        os.Status.Should().Be(StatusOrdemDeServico.Cancelada);
        (await _harness.Published.Any<CancelarOrcamento>()).Should().BeFalse();
        _metricas.Verify(m => m.SagaCompensou(os.NumeroOS, "Orçamento recusado pelo cliente", "nenhuma"), Times.Once);
    }

    [Fact]
    public async Task PecasIndisponiveis_DeveCancelarOOrcamento()
    {
        var os = NovaOS(comPecas: true);
        var orcamentoId = await AteAguardandoAprovacao(os);
        await _harness.Bus.Publish(new OrcamentoAprovado(os.Id, orcamentoId));
        await Etapa(os.Id, m => m.ReservandoPecas);

        await _harness.Bus.Publish(new PecasIndisponiveis(os.Id, "Filtro sem estoque"));

        await Etapa(os.Id, m => m.Cancelada);
        (await _harness.Published.Any<CancelarOrcamento>(m => m.Context.Message.OrdemDeServicoId == os.Id)).Should().BeTrue();
        (await _harness.Published.Any<LiberarReservaPecas>()).Should().BeFalse();
        os.Status.Should().Be(StatusOrdemDeServico.Cancelada);
    }

    [Fact]
    public async Task PagamentoRecusado_DeveLiberarReservaECancelarOrcamento()
    {
        var os = NovaOS(comPecas: true);
        var orcamentoId = await AteAguardandoAprovacao(os);
        await _harness.Bus.Publish(new OrcamentoAprovado(os.Id, orcamentoId));
        await _harness.Bus.Publish(new PecasReservadas(os.Id));
        await Etapa(os.Id, m => m.AguardandoPagamento);

        await _harness.Bus.Publish(new PagamentoRecusado(os.Id, orcamentoId, Guid.NewGuid(), "Cartão recusado"));

        await Etapa(os.Id, m => m.Cancelada);
        (await _harness.Published.Any<LiberarReservaPecas>()).Should().BeTrue();
        (await _harness.Published.Any<CancelarOrcamento>()).Should().BeTrue();
        (await _harness.Published.Any<EstornarPagamento>()).Should().BeFalse();
        os.Status.Should().Be(StatusOrdemDeServico.Cancelada);
    }

    [Fact]
    public async Task ExecucaoFalhou_DeveEstornarLiberarECancelar()
    {
        var os = NovaOS(comPecas: true);
        var (_, pagamentoId) = await AteEmExecucao(os);

        await _harness.Bus.Publish(new ExecucaoFalhou(os.Id, "Motor travado"));

        await Etapa(os.Id, m => m.Cancelada);
        (await _harness.Published.Any<EstornarPagamento>(m => m.Context.Message.PagamentoId == pagamentoId)).Should().BeTrue();
        (await _harness.Published.Any<LiberarReservaPecas>()).Should().BeTrue();
        (await _harness.Published.Any<CancelarOrcamento>()).Should().BeTrue();
        os.Status.Should().Be(StatusOrdemDeServico.Cancelada);
        _metricas.Verify(m => m.SagaCompensou(os.NumeroOS, "Execução falhou: Motor travado",
            "EstornarPagamento, LiberarReservaPecas, CancelarOrcamento"), Times.Once);
    }

    [Fact]
    public async Task PrazoDeAprovacao_DeveCancelarSozinho()
    {
        var os = NovaOS(comPecas: false);
        await AteAguardandoAprovacao(os);

        // O agendador entregaria esta mensagem quando o prazo vencesse; o teste
        // a entrega direto, sem depender de relógio.
        await _harness.Bus.Publish(new PrazoAprovacaoExpirado(os.Id));

        await Etapa(os.Id, m => m.Cancelada);

        (await _harness.Published.Any<CancelarOrcamento>(m => m.Context.Message.Motivo == "Prazo de aprovação expirado")).Should().BeTrue();
        os.Status.Should().Be(StatusOrdemDeServico.Cancelada);
    }

    [Fact]
    public async Task PrazoDePagamento_DeveLiberarReservaECancelar()
    {
        var os = NovaOS(comPecas: true);
        var orcamentoId = await AteAguardandoAprovacao(os);
        await _harness.Bus.Publish(new OrcamentoAprovado(os.Id, orcamentoId));
        await _harness.Bus.Publish(new PecasReservadas(os.Id));
        await Etapa(os.Id, m => m.AguardandoPagamento);

        await _harness.Bus.Publish(new PrazoPagamentoExpirado(os.Id));

        await Etapa(os.Id, m => m.Cancelada);
        (await _harness.Published.Any<LiberarReservaPecas>()).Should().BeTrue();
        (await _harness.Published.Any<CancelarOrcamento>(m => m.Context.Message.Motivo == "Prazo de pagamento expirado")).Should().BeTrue();
    }

    [Fact]
    public async Task PagamentoConfirmadoDepoisDoCancelamento_DeveEstornar()
    {
        var os = NovaOS(comPecas: false);
        var orcamentoId = await AteAguardandoAprovacao(os);
        await _harness.Bus.Publish(new OrcamentoRecusado(os.Id, orcamentoId));
        await Etapa(os.Id, m => m.Cancelada);
        var pagamentoId = Guid.NewGuid();

        await _harness.Bus.Publish(new PagamentoConfirmado(os.Id, orcamentoId, pagamentoId));

        (await _harness.Published.Any<EstornarPagamento>(m => m.Context.Message.PagamentoId == pagamentoId)).Should().BeTrue();
    }

    [Fact]
    public async Task EventoRepetido_NaoDeveAvancarDuasVezesNemQuebrar()
    {
        var os = NovaOS(comPecas: false);
        var orcamentoId = await AteAguardandoAprovacao(os);

        await _harness.Bus.Publish(new OrcamentoEnviado(os.Id, orcamentoId, 260m));
        await _harness.Bus.Publish(new OrcamentoAprovado(os.Id, orcamentoId));
        await Etapa(os.Id, m => m.AguardandoPagamento);
        await _harness.Bus.Publish(new OrcamentoAprovado(os.Id, orcamentoId));

        (await _harness.Published.SelectAsync<SolicitarPagamento>().Count()).Should().Be(1);
        (await _harness.Consumed.Any<Fault>()).Should().BeFalse();
        os.Historico.Count(h => h.StatusNovo == StatusOrdemDeServico.AguardandoAprovacao).Should().Be(1);
    }

    [Fact]
    public async Task PedidoDeOrcamentoAntesDaAbertura_DeveAbrirASagaAssimMesmo()
    {
        var os = NovaOS(comPecas: false);

        await _harness.Bus.Publish(new OrcamentoSolicitado(os.Id, os.NumeroOS, os.ClienteId,
            [new ItemDoPedido("Servico", Guid.NewGuid(), "Revisão", 1, 200m)], [], [new ServicoDoPedido(Guid.NewGuid(), "Revisão")]));

        await Etapa(os.Id, m => m.AguardandoOrcamento);
        await _harness.Bus.Publish(new OrdemDeServicoAberta(os.Id, os.NumeroOS, os.ClienteId));
        await Etapa(os.Id, m => m.AguardandoOrcamento);
    }

    [Fact]
    public async Task EventoDeOSSemSaga_DeveSerDescartado()
    {
        await _harness.Bus.Publish(new PecasReservadas(Guid.NewGuid()));

        (await _harness.Consumed.Any<PecasReservadas>()).Should().BeTrue();
        (await _harness.Consumed.Any<Fault>()).Should().BeFalse();
    }
}
