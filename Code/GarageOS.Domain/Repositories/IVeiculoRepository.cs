using GarageOS.Domain.Entities;

namespace GarageOS.Domain.Repositories;

public interface IVeiculoRepository
{
    Task<IEnumerable<Veiculo>> ListarTodosAsync();
    Task<Veiculo?> ObterPorIdAsync(Guid id);
    Task AdicionarAsync(Veiculo servico);
    Task AtualizarAsync(Veiculo servico);
}