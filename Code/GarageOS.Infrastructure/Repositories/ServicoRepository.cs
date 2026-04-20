using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;
using GarageOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Repositories;

public class ServicoRepository : IServicoRepository
{
    private readonly GarageOSDbContext _context;

    public ServicoRepository(GarageOSDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Servico>> ListarTodosAsync()
    {
        return await _context.Servicos
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Servico?> ObterPorIdAsync(Guid id)
    {
        return await _context.Servicos.FindAsync(id);
    }

    public async Task AdicionarAsync(Servico servico)
    {
        await _context.Servicos.AddAsync(servico);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(Servico servico)
    {
        _context.Servicos.Update(servico);
        await _context.SaveChangesAsync();
    }
}
