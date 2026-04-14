using GarageOS.Domain.Entities;

namespace GarageOS.Domain.Repositories;

public interface IServicoRepository
{
    Task<IEnumerable<Servico>> ListarTodosAsync();
    Task<Servico?> ObterPorIdAsync(Guid id);
    Task AdicionarAsync(Servico servico);
    Task AtualizarAsync(Servico servico);
}
