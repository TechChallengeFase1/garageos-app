using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;
using GarageOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Repositories;

public class VeiculoRepository : IVeiculoRepository
{
    private readonly GarageOSDbContext _context;

    public VeiculoRepository(GarageOSDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Veiculo>> ListarTodosAsync()
    {
        return await _context.Veiculos
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Veiculo?> ObterPorIdAsync(Guid id)
    {
        return await _context.Veiculos.FindAsync(id);
    }

    public async Task AdicionarAsync(Veiculo veiculo)
    {
        await _context.Veiculos.AddAsync(veiculo);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(Veiculo veiculo)
    {
        _context.Veiculos.Update(veiculo);
        await _context.SaveChangesAsync();
    }
}