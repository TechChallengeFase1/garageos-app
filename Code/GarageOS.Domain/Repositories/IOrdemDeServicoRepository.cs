using GarageOS.Domain.Entities;

namespace GarageOS.Domain.Repositories;

public interface IOrdemDeServicoRepository
{
    Task<IEnumerable<OrdemDeServico>> ListarTodosAsync();
    Task<OrdemDeServico?> ObterPorIdAsync(Guid id);
    Task<OrdemDeServico?> ObterPorNumeroOSAsync(string numeroOS);
    Task AdicionarAsync(OrdemDeServico ordemDeServico);
    Task AtualizarAsync(OrdemDeServico ordemDeServico);

    /// <summary>Próximo número da sequência do ano, gerado pelo banco</summary>
    /// <remarks>
    /// Uma SEQUENCE do PostgreSQL por ano. Antes o número saía de "ler a última
    /// OS e somar 1", e duas aberturas simultâneas (com o HPA, várias réplicas)
    /// geravam o mesmo número.
    /// </remarks>
    Task<int> ObterProximoSequencialDoAnoAsync(int ano);
}
