using GarageOS.Domain.Entities;

namespace GarageOS.Domain.Repositories
{
    public interface IClienteRepository
    {
        Task<IEnumerable<Cliente>> ListarTodosAsync();
        Task<Cliente?> ObterPorIdAsync(Guid id);
        Task AdicionarAsync(Cliente cliente);
        Task AtualizarAsync(Cliente cliente);
        Task<Cliente?> ObterPorEmailAsync(string email);
        Task<Cliente?> ObterPorTelefoneAsync(string telefone);
        Task<Cliente?> ObterPorDocumentoAsync(string documento);

    }
}
