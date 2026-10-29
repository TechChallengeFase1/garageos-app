using GarageOS.Domain.Enums;
using GarageOS.Domain.Utils;

namespace GarageOS.Domain.Entities;

/// <summary>Uma transição de status da OS</summary>
/// <remarks>
/// Até a Fase 3 a transição só existia como evento no New Relic. Agora fica
/// gravada: é o que o cliente vê no acompanhamento e o que permite reconstruir,
/// pelo CorrelationId, a requisição (ou a mensagem da saga) que causou a mudança.
/// </remarks>
public class HistoricoStatusOS
{
    public Guid Id { get; private set; }
    public Guid OrdemDeServicoId { get; private set; }
    public StatusOrdemDeServico? StatusAnterior { get; private set; }
    public StatusOrdemDeServico StatusNovo { get; private set; }
    public string Origem { get; private set; } = null!;
    public string? CorrelationId { get; private set; }
    public DateTime OcorridoEm { get; private set; }

    protected HistoricoStatusOS() { }

    public HistoricoStatusOS(Guid ordemDeServicoId, StatusOrdemDeServico? statusAnterior, StatusOrdemDeServico statusNovo, string origem, string? correlationId)
    {
        Id = Guid.NewGuid();
        OrdemDeServicoId = ordemDeServicoId;
        StatusAnterior = statusAnterior;
        StatusNovo = statusNovo;
        Origem = origem;
        CorrelationId = correlationId;
        OcorridoEm = BrasiliaTime.Agora;
    }
}
