using GarageOS.Domain.Entities;

namespace GarageOS.Domain.Repositories;

public interface IEstoqueRepository
{
    Task<IEnumerable<Estoque>> ListarTodosAsync();
    Task<Estoque?> ObterPorIdAsync(Guid id);
    Task AdicionarAsync(Estoque estoque);
    Task AtualizarAsync(Estoque estoque);
    Task RemoverAsync(Estoque estoque);
}
