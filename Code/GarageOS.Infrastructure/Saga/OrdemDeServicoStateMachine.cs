using System.Text.Json;
using GarageOS.Application.Abstractions;
using GarageOS.Contracts.V1;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Options;

namespace GarageOS.Infrastructure.Saga;

/// <summary>Orquestrador da saga da ordem de serviço (ADR 0005)</summary>
/// <remarks>
/// Fluxo feliz:
///
///   OrcamentoSolicitado  -> GerarOrcamento        (Billing)    AguardandoOrcamento
///   OrcamentoEnviado     -> agenda prazo           OS: AguardandoAprovacao
///   OrcamentoAprovado    -> ReservarPecas          (Execução)   ReservandoPecas
///   PecasReservadas      -> SolicitarPagamento     (Billing)    OS: AguardandoPagamento
///   PagamentoConfirmado  -> IniciarExecucao        (Execução)   OS: EmExecucao
///   ExecucaoFinalizada   ->                                     OS: Finalizada
///
/// Sem peças na OS, a reserva é pulada: aprovado vai direto para o pagamento.
///
/// Compensações (cada passo desfaz só o que já tinha acontecido):
///
///   OrcamentoRecusado        -> nada a desfazer
///   prazo de aprovação       -> CancelarOrcamento
///   PecasIndisponiveis       -> CancelarOrcamento
///   PagamentoRecusado/prazo  -> LiberarReservaPecas, CancelarOrcamento
///   ExecucaoFalhou           -> EstornarPagamento, LiberarReservaPecas, CancelarOrcamento
///   PagamentoConfirmado tarde (OS já cancelada) -> EstornarPagamento
///
/// Em todos, a OS termina em Cancelada com o motivo no estado da saga.
///
/// Idempotência: o inbox do outbox EF descarta a mesma mensagem entregue duas
/// vezes; evento que chega num estado em que não faz sentido (repetido com
/// outro MessageId, fora de ordem) é ignorado em vez de ir para a fila de erro.
/// </remarks>
public class OrdemDeServicoStateMachine : MassTransitStateMachine<OrdemDeServicoSagaState>
{
    private const string Origem = "Saga";

    public State Aberta { get; private set; } = null!;
    public State AguardandoOrcamento { get; private set; } = null!;
    public State AguardandoAprovacao { get; private set; } = null!;
    public State ReservandoPecas { get; private set; } = null!;
    public State AguardandoPagamento { get; private set; } = null!;
    public State EmExecucao { get; private set; } = null!;
    public State Finalizada { get; private set; } = null!;
    public State Cancelada { get; private set; } = null!;

    public Event<OrdemDeServicoAberta> OsAberta { get; private set; } = null!;
    public Event<OrcamentoSolicitado> OrcamentoPedido { get; private set; } = null!;
    public Event<OrcamentoEnviado> OrcamentoEnviado { get; private set; } = null!;
    public Event<OrcamentoAprovado> OrcamentoAprovado { get; private set; } = null!;
    public Event<OrcamentoRecusado> OrcamentoRecusado { get; private set; } = null!;
    public Event<PecasReservadas> PecasReservadas { get; private set; } = null!;
    public Event<PecasIndisponiveis> PecasIndisponiveis { get; private set; } = null!;
    public Event<PagamentoCriado> PagamentoCriado { get; private set; } = null!;
    public Event<PagamentoConfirmado> PagamentoConfirmado { get; private set; } = null!;
    public Event<PagamentoRecusado> PagamentoRecusado { get; private set; } = null!;
    public Event<ExecucaoFinalizada> ExecucaoFinalizada { get; private set; } = null!;
    public Event<ExecucaoFalhou> ExecucaoFalhou { get; private set; } = null!;

    public Schedule<OrdemDeServicoSagaState, PrazoAprovacaoExpirado> PrazoAprovacao { get; private set; } = null!;
    public Schedule<OrdemDeServicoSagaState, PrazoPagamentoExpirado> PrazoPagamento { get; private set; } = null!;

    public OrdemDeServicoStateMachine(IOptions<SagaOptions> opcoes)
    {
        var prazos = opcoes.Value;

        InstanceState(x => x.CurrentState);

        Event(() => OsAberta, e => e.CorrelateById(m => m.Message.OrdemDeServicoId));
        Event(() => OrcamentoPedido, e => e.CorrelateById(m => m.Message.OrdemDeServicoId));
        Externo(() => OrcamentoEnviado);
        Externo(() => OrcamentoAprovado);
        Externo(() => OrcamentoRecusado);
        Externo(() => PecasReservadas);
        Externo(() => PecasIndisponiveis);
        Externo(() => PagamentoCriado);
        Externo(() => PagamentoConfirmado);
        Externo(() => PagamentoRecusado);
        Externo(() => ExecucaoFinalizada);
        Externo(() => ExecucaoFalhou);

        Schedule(() => PrazoAprovacao, x => x.PrazoAprovacaoToken, s =>
        {
            s.Delay = TimeSpan.FromMinutes(prazos.PrazoAprovacaoMinutos);
            s.Received = r => r.CorrelateById(m => m.Message.OrdemDeServicoId);
        });
        Schedule(() => PrazoPagamento, x => x.PrazoPagamentoToken, s =>
        {
            s.Delay = TimeSpan.FromMinutes(prazos.PrazoPagamentoMinutos);
            s.Received = r => r.CorrelateById(m => m.Message.OrdemDeServicoId);
        });

        // Evento fora de hora (repetido, atrasado, fora de ordem) não derruba
        // a mensagem: é ignorado. Os casos que exigem reação têm handler.
        OnUnhandledEvent(x => x.Ignore());

        Initially(
            When(OsAberta)
                .Then(c => Iniciar(c.Saga, c.Message.NumeroOS, c.Message.ClienteId))
                .TransitionTo(Aberta),
            // O SQS não garante ordem: o pedido de orçamento pode chegar antes
            // do evento de abertura. Ele traz o bastante para abrir a saga.
            When(OrcamentoPedido)
                .Then(c => Iniciar(c.Saga, c.Message.NumeroOS, c.Message.ClienteId))
                .Then(c => GuardarPedido(c.Saga, c.Message))
                .Publish(c => PedirOrcamento(c.Message))
                .ThenAsync(c => Etapa(c, "AguardandoOrcamento"))
                .TransitionTo(AguardandoOrcamento));

        During(Aberta,
            When(OrcamentoPedido)
                .Then(c => GuardarPedido(c.Saga, c.Message))
                .Publish(c => PedirOrcamento(c.Message))
                .ThenAsync(c => Etapa(c, "AguardandoOrcamento"))
                .TransitionTo(AguardandoOrcamento));

        During(AguardandoOrcamento,
            When(OrcamentoEnviado)
                .Then(c => { c.Saga.OrcamentoId = c.Message.OrcamentoId; c.Saga.Total = c.Message.Total; })
                .ThenAsync(c => MoverOS(c, StatusOrdemDeServico.EmDiagnostico, StatusOrdemDeServico.AguardandoAprovacao))
                .Schedule(PrazoAprovacao, c => new PrazoAprovacaoExpirado(c.Saga.CorrelationId))
                .ThenAsync(c => Etapa(c, "AguardandoAprovacao"))
                .TransitionTo(AguardandoAprovacao));

        During(AguardandoAprovacao,
            When(OrcamentoAprovado)
                .Unschedule(PrazoAprovacao)
                .IfElse(c => c.Saga.TemPecas,
                    comPecas => comPecas
                        .Publish(c => new ReservarPecas(c.Saga.CorrelationId, Ler<PecaDoPedido>(c.Saga.PecasJson)))
                        .ThenAsync(c => Etapa(c, "ReservandoPecas"))
                        .TransitionTo(ReservandoPecas),
                    semPecas => semPecas
                        .Publish(c => new SolicitarPagamento(c.Saga.CorrelationId, c.Saga.OrcamentoId!.Value))
                        .ThenAsync(c => MoverOS(c, StatusOrdemDeServico.AguardandoPagamento))
                        .Schedule(PrazoPagamento, c => new PrazoPagamentoExpirado(c.Saga.CorrelationId))
                        .ThenAsync(c => Etapa(c, "AguardandoPagamento"))
                        .TransitionTo(AguardandoPagamento)),
            When(OrcamentoRecusado)
                .Unschedule(PrazoAprovacao)
                .ThenAsync(c => Cancelar(c, "Orçamento recusado pelo cliente"))
                .TransitionTo(Cancelada),
            When(PrazoAprovacao.Received)
                .Publish(c => new CancelarOrcamento(c.Saga.CorrelationId, "Prazo de aprovação expirado"))
                .ThenAsync(c => Cancelar(c, "Prazo de aprovação expirado", "CancelarOrcamento"))
                .TransitionTo(Cancelada));

        During(ReservandoPecas,
            When(PecasReservadas)
                .Publish(c => new SolicitarPagamento(c.Saga.CorrelationId, c.Saga.OrcamentoId!.Value))
                .ThenAsync(c => MoverOS(c, StatusOrdemDeServico.AguardandoPagamento))
                .Schedule(PrazoPagamento, c => new PrazoPagamentoExpirado(c.Saga.CorrelationId))
                .ThenAsync(c => Etapa(c, "AguardandoPagamento"))
                .TransitionTo(AguardandoPagamento),
            When(PecasIndisponiveis)
                .Publish(c => new CancelarOrcamento(c.Saga.CorrelationId, $"Peças indisponíveis: {c.Message.Motivo}"))
                .ThenAsync(c => Cancelar(c, $"Peças indisponíveis: {c.Message.Motivo}", "CancelarOrcamento"))
                .TransitionTo(Cancelada));

        During(AguardandoPagamento,
            When(PagamentoCriado)
                .Then(c => c.Saga.PagamentoId = c.Message.PagamentoId),
            When(PagamentoConfirmado)
                .Unschedule(PrazoPagamento)
                .Then(c => c.Saga.PagamentoId = c.Message.PagamentoId)
                .Publish(c => new IniciarExecucao(c.Saga.CorrelationId, c.Saga.NumeroOS, Ler<ServicoDoPedido>(c.Saga.ServicosJson)))
                .ThenAsync(c => MoverOS(c, StatusOrdemDeServico.EmExecucao))
                .ThenAsync(c => Etapa(c, "EmExecucao"))
                .TransitionTo(EmExecucao),
            When(PagamentoRecusado)
                .Unschedule(PrazoPagamento)
                .ThenAsync(c => DesfazerAntesDaExecucao(c, $"Pagamento recusado: {c.Message.Motivo}"))
                .TransitionTo(Cancelada),
            When(PrazoPagamento.Received)
                .ThenAsync(c => DesfazerAntesDaExecucao(c, "Prazo de pagamento expirado"))
                .TransitionTo(Cancelada));

        During(EmExecucao,
            When(ExecucaoFinalizada)
                .ThenAsync(c => MoverOS(c, StatusOrdemDeServico.Finalizada))
                .ThenAsync(c => Etapa(c, "Finalizada"))
                .TransitionTo(Finalizada),
            When(ExecucaoFalhou)
                .ThenAsync(c => DesfazerDepoisDoPagamento(c, $"Execução falhou: {c.Message.Motivo}"))
                .TransitionTo(Cancelada));

        // Webhook atrasado: o pagamento foi confirmado depois que a OS já tinha
        // sido cancelada (prazo estourado, peça em falta). O dinheiro volta.
        During(Cancelada,
            When(PagamentoConfirmado)
                .Publish(c => new EstornarPagamento(
                    c.Saga.CorrelationId, c.Message.OrcamentoId, c.Message.PagamentoId, "Pagamento confirmado depois do cancelamento da OS"))
                .ThenAsync(c => Compensou(c, "PagamentoConfirmadoTarde", "EstornarPagamento")));
    }

    private void Externo<T>(System.Linq.Expressions.Expression<Func<Event<T>>> evento) where T : class
    {
        Event(evento, e =>
        {
            e.CorrelateBy((saga, context) => saga.CorrelationId == IdDaOS(context.Message));
            e.OnMissingInstance(m => m.Discard());
        });
    }

    private static Guid IdDaOS(object mensagem) =>
        (Guid)mensagem.GetType().GetProperty(nameof(OrdemDeServicoAberta.OrdemDeServicoId))!.GetValue(mensagem)!;

    private static void Iniciar(OrdemDeServicoSagaState saga, string numeroOS, Guid clienteId)
    {
        saga.NumeroOS = numeroOS;
        saga.ClienteId = clienteId;
        saga.CriadaEm = DateTime.UtcNow;
        saga.AtualizadaEm = saga.CriadaEm;
    }

    private static void GuardarPedido(OrdemDeServicoSagaState saga, OrcamentoSolicitado pedido)
    {
        saga.PecasJson = JsonSerializer.Serialize(pedido.Pecas);
        saga.ServicosJson = JsonSerializer.Serialize(pedido.Servicos);
        saga.TemPecas = pedido.Pecas.Count > 0;
    }

    private static GerarOrcamento PedirOrcamento(OrcamentoSolicitado pedido) =>
        new(pedido.OrdemDeServicoId, pedido.NumeroOS, pedido.ClienteId, pedido.Itens);

    private static List<T> Ler<T>(string json) => JsonSerializer.Deserialize<List<T>>(json) ?? [];

    private static async Task DesfazerAntesDaExecucao(BehaviorContext<OrdemDeServicoSagaState> c, string motivo)
    {
        var compensacoes = new List<string>();
        if (c.Saga.TemPecas)
        {
            await c.Publish(new LiberarReservaPecas(c.Saga.CorrelationId, motivo));
            compensacoes.Add("LiberarReservaPecas");
        }

        await c.Publish(new CancelarOrcamento(c.Saga.CorrelationId, motivo));
        compensacoes.Add("CancelarOrcamento");

        await Cancelar(c, motivo, compensacoes.ToArray());
    }

    private static async Task DesfazerDepoisDoPagamento(BehaviorContext<OrdemDeServicoSagaState> c, string motivo)
    {
        var compensacoes = new List<string>();
        if (c.Saga.PagamentoId.HasValue && c.Saga.OrcamentoId.HasValue)
        {
            await c.Publish(new EstornarPagamento(c.Saga.CorrelationId, c.Saga.OrcamentoId.Value, c.Saga.PagamentoId.Value, motivo));
            compensacoes.Add("EstornarPagamento");
        }

        if (c.Saga.TemPecas)
        {
            await c.Publish(new LiberarReservaPecas(c.Saga.CorrelationId, motivo));
            compensacoes.Add("LiberarReservaPecas");
        }

        await c.Publish(new CancelarOrcamento(c.Saga.CorrelationId, motivo));
        compensacoes.Add("CancelarOrcamento");

        await Cancelar(c, motivo, compensacoes.ToArray());
    }

    private static async Task Cancelar(BehaviorContext<OrdemDeServicoSagaState> c, string motivo, params string[] compensacoes)
    {
        c.Saga.Motivo = motivo;
        await MoverOS(c, StatusOrdemDeServico.Cancelada);
        await Compensou(c, motivo, compensacoes.Length == 0 ? "nenhuma" : string.Join(", ", compensacoes));
        await Etapa(c, "Cancelada");
    }

    private static Task Compensou(BehaviorContext<OrdemDeServicoSagaState> c, string falha, string compensacoes)
    {
        c.GetServiceOrCreateInstance<IMetricasDeNegocio>().SagaCompensou(c.Saga.NumeroOS, falha, compensacoes);
        return Task.CompletedTask;
    }

    private static Task Etapa(BehaviorContext<OrdemDeServicoSagaState> c, string etapa)
    {
        c.Saga.AtualizadaEm = DateTime.UtcNow;
        c.GetServiceOrCreateInstance<IMetricasDeNegocio>().SagaAvancou(c.Saga.NumeroOS, etapa);
        return Task.CompletedTask;
    }

    /// <summary>Leva a OS ao status pedido, passando pelos intermediários</summary>
    /// <remarks>
    /// A OS é carregada pelo repositório no mesmo DbContext da saga e não é
    /// gravada aqui: o repositório da saga grava tudo junto no fim, na mesma
    /// transação do estado da saga e do outbox. Status já alcançado é pulado,
    /// o que torna a mensagem repetida inofensiva.
    /// </remarks>
    private static async Task MoverOS(BehaviorContext<OrdemDeServicoSagaState> c, params StatusOrdemDeServico[] caminho)
    {
        var repositorio = c.GetServiceOrCreateInstance<IOrdemDeServicoRepository>();
        var metricas = c.GetServiceOrCreateInstance<IMetricasDeNegocio>();
        var os = await repositorio.ObterPorIdAsync(c.Saga.CorrelationId);
        if (os is null)
            return;

        var correlationId = c.Headers.Get<string>("X-Correlation-Id") ?? c.MessageId?.ToString();

        foreach (var destino in caminho)
        {
            if (os.Status == destino || !OrdemDeServico.TransicaoPermitida(os.Status, destino))
                continue;

            var anterior = os.Status;
            var minutos = (DateTime.UtcNow - os.AtualizadoEm.ToUniversalTime()).TotalMinutes;
            os.AlterarStatus(destino, Origem, correlationId);

            // Mantém vivo o dashboard "tempo médio por status" da Fase 3.
            metricas.StatusAlterado(os.NumeroOS, anterior.ToString(), destino.ToString(), minutos);
        }
    }
}
