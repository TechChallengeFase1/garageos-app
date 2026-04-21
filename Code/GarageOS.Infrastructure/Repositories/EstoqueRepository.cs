using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;
using GarageOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Repositories;

public class EstoqueRepository : IEstoqueRepository
{
    private readonly GarageOSDbContext _context;

    public EstoqueRepository(GarageOSDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Estoque>> ListarTodosAsync()
    {
        return await _context.Estoques
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Estoque?> ObterPorIdAsync(Guid id)
    {
        return await _context.Estoques.FindAsync(id);
    }

    public async Task AdicionarAsync(Estoque estoque)
    {
        await _context.Estoques.AddAsync(estoque);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(Estoque estoque)
    {
        _context.Estoques.Update(estoque);
        await _context.SaveChangesAsync();
    }
}
