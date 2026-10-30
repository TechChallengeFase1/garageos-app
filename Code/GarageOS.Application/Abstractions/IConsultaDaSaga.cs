namespace GarageOS.Application.Abstractions;

/// <summary>Leitura do estado da saga de uma OS, para acompanhamento e depuração</summary>
public interface IConsultaDaSaga
{
    Task<EstadoDaSaga?> ObterAsync(Guid ordemDeServicoId, CancellationToken cancellationToken = default);
}

/// <summary>Onde a saga da OS está, e o que ela sabe dos outros serviços</summary>
public record EstadoDaSaga(
    Guid OrdemDeServicoId,
    string NumeroOS,
    string Etapa,
    Guid? OrcamentoId,
    decimal? Total,
    Guid? PagamentoId,
    bool TemPecas,
    string? Motivo,
    DateTime CriadaEm,
    DateTime AtualizadaEm);
