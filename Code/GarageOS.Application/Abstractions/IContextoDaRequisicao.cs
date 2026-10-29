namespace GarageOS.Application.Abstractions;

/// <summary>Dados da requisição atual que os use cases precisam sem conhecer HTTP</summary>
public interface IContextoDaRequisicao
{
    /// <summary>X-Correlation-Id da requisição, gravado no histórico da OS</summary>
    string? CorrelationId { get; }
}
