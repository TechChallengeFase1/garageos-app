using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;
using GarageOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Repositories;

public class OrcamentoRepository : IOrcamentoRepository
{
    private readonly GarageOSDbContext _context;

    public OrcamentoRepository(GarageOSDbContext context)
    {
        _context = context;
    }

    public async Task<Orcamento?> ObterPorIdAsync(Guid id)
    {
        return await _context.Orcamentos
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<Orcamento?> ObterPorOrdemDeServicoIdAsync(Guid ordemDeServicoId)
    {
        return await _context.Orcamentos
            .FirstOrDefaultAsync(x => x.OrdemDeServicoId == ordemDeServicoId);
    }

    public async Task AdicionarAsync(Orcamento orcamento)
    {
        _context.Orcamentos.Add(orcamento);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(Orcamento orcamento)
    {
        _context.Orcamentos.Update(orcamento);
        await _context.SaveChangesAsync();
    }
}
