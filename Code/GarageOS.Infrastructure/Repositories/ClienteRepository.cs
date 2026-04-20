using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;
using GarageOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly GarageOSDbContext _context;

        public ClienteRepository(GarageOSDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Cliente>> ListarTodosAsync()
        {
            return await _context.Clientes
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Cliente?> ObterPorIdAsync(Guid id)
        {
            return await _context.Clientes.FindAsync(id);
        }

        public async Task AdicionarAsync(Cliente cliente)
        {
            await _context.Clientes.AddAsync(cliente);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Cliente cliente)
        {
            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();
        }

        public async Task<Cliente?> ObterPorDocumentoAsync(string documento)
        {
            return await _context.Clientes
                .FirstOrDefaultAsync(c => c.Documento.Valor == documento);
        }

        public async Task<Cliente?> ObterPorEmailAsync(string email)
        {
            return await _context.Clientes
                .FirstOrDefaultAsync(c => c.Email == email);
        }

        public async Task<Cliente?> ObterPorTelefoneAsync(string telefone)
        {
            return await _context.Clientes
                .FirstOrDefaultAsync(c => c.Telefone == telefone);
        }
    }
}
