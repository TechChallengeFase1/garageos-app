using GarageOS.Domain.Entities;

namespace GarageOS.Domain.Repositories;

public interface IOrdemDeServicoRepository
{
    Task<IEnumerable<OrdemDeServico>> ListarTodosAsync();
    Task<OrdemDeServico?> ObterPorIdAsync(Guid id);
    Task<OrdemDeServico?> ObterPorNumeroOSAsync(string numeroOS);
    Task<OrdemDeServico?> ObterPorNumeroOSComTrackingAsync(string numeroOS);
    Task AdicionarAsync(OrdemDeServico ordemDeServico);
    Task AtualizarAsync(OrdemDeServico ordemDeServico);
    Task<int> ObterUltimoSequencialDoAnoAsync(int ano);
    Task<IEnumerable<OrdemDeServicoServico>> ObterServicosFinalizadosAsync();
}
