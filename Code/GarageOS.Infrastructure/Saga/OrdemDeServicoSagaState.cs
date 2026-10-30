using MassTransit;

namespace GarageOS.Infrastructure.Saga;

/// <summary>Estado persistido da saga de uma OS (uma linha por OS)</summary>
/// <remarks>
/// CorrelationId é o Id da OS: toda mensagem da saga carrega o
/// OrdemDeServicoId, e é por ele que a instância é encontrada. Peças e serviços
/// ficam guardados em JSON porque a saga precisa deles mais tarde (reservar,
/// executar) e os outros serviços não os devolvem.
/// </remarks>
public class OrdemDeServicoSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
    public string NumeroOS { get; set; } = string.Empty;
    public Guid ClienteId { get; set; }

    public Guid? OrcamentoId { get; set; }
    public decimal? Total { get; set; }
    public Guid? PagamentoId { get; set; }

    public string PecasJson { get; set; } = "[]";
    public string ServicosJson { get; set; } = "[]";
    public bool TemPecas { get; set; }

    /// <summary>Por que a saga terminou em Cancelada</summary>
    public string? Motivo { get; set; }

    public Guid? PrazoAprovacaoToken { get; set; }
    public Guid? PrazoPagamentoToken { get; set; }

    public DateTime CriadaEm { get; set; }
    public DateTime AtualizadaEm { get; set; }

    /// <summary>xmin do PostgreSQL, para concorrência otimista</summary>
    public uint Versao { get; set; }
}
