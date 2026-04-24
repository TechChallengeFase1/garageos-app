using GarageOS.Domain.Entities;

namespace GarageOS.Domain.Repositories;

public interface IOrcamentoRepository
{
    Task<Orcamento?> ObterPorIdAsync(Guid id);
    Task<Orcamento?> ObterPorOrdemDeServicoIdAsync(Guid ordemDeServicoId);
    Task AdicionarAsync(Orcamento orcamento);
    Task AtualizarAsync(Orcamento orcamento);
}
