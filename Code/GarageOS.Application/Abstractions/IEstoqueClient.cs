namespace GarageOS.Application.Abstractions;

/// <summary>Consulta ao estoque, que pertence ao Execução Service</summary>
/// <remarks>
/// Síncrono de propósito: o atendente está esperando a OS abrir, e precisa
/// saber na hora se a peça existe e quanto custa. Nada de ler o banco do outro
/// serviço: a consulta é pela API dele (GET /api/estoques/{id}).
/// </remarks>
public interface IEstoqueClient
{
    /// <returns>A peça, ou null se ela não existe</returns>
    /// <exception cref="Domain.Exceptions.ServicoExternoIndisponivelException">O Execução não respondeu</exception>
    Task<PecaDoEstoque?> ObterPecaAsync(Guid estoqueId, CancellationToken cancellationToken = default);
}

/// <summary>O que o OS precisa saber de uma peça para incluí-la na OS</summary>
public record PecaDoEstoque(Guid Id, string Nome, decimal Valor, int QuantidadeDisponivel);
