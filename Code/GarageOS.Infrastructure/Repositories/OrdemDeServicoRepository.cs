using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Repositories;
using GarageOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Repositories;

public class OrdemDeServicoRepository : IOrdemDeServicoRepository
{
    private readonly GarageOSDbContext _context;

    public OrdemDeServicoRepository(GarageOSDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrdemDeServico>> ListarTodosAsync()
    {
        return await _context.OrdensDeServico
            .AsNoTracking()
            .Where(x => x.Status != StatusOrdemDeServico.Finalizada
                 && x.Status != StatusOrdemDeServico.Entregue)
            .Include(x => x.Servicos)
            .ThenInclude(x => x.Servico)
            .Include(x => x.Estoques)
            .ThenInclude(x => x.Estoque)
            .Include(x => x.Orcamento)
            .OrderBy(x => x.Status == StatusOrdemDeServico.EmExecucao ? 0
                    : x.Status == StatusOrdemDeServico.AguardandoAprovacao ? 1
                    : x.Status == StatusOrdemDeServico.EmDiagnostico ? 2
                    : x.Status == StatusOrdemDeServico.Recebida ? 3
                    : 4)
            .ThenBy(x => x.CriadoEm)
            .ToListAsync();
    }

    public async Task<OrdemDeServico?> ObterPorIdAsync(Guid id)
    {
        return await _context.OrdensDeServico
            .Include(x => x.Servicos)
            .ThenInclude(x => x.Servico)
            .Include(x => x.Estoques)
            .ThenInclude(x => x.Estoque)
            .Include(x => x.Orcamento)
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<OrdemDeServico?> ObterPorNumeroOSAsync(string numeroOS)
    {
        return await _context.OrdensDeServico
            .AsNoTracking()
            .Include(x => x.Servicos)
            .ThenInclude(x => x.Servico)
            .Include(x => x.Estoques)
            .ThenInclude(x => x.Estoque)
            .FirstOrDefaultAsync(x => x.NumeroOS == numeroOS);
    }

    public async Task<OrdemDeServico?> ObterPorNumeroOSComTrackingAsync(string numeroOS)
    {
        return await _context.OrdensDeServico
            .Include(x => x.Servicos)
            .ThenInclude(x => x.Servico)
            .Include(x => x.Estoques)
            .ThenInclude(x => x.Estoque)
            .Include(x => x.Orcamento)
            .FirstOrDefaultAsync(x => x.NumeroOS == numeroOS);
    }

    public async Task AdicionarAsync(OrdemDeServico ordemDeServico)
    {
        _context.OrdensDeServico.Add(ordemDeServico);
        await _context.SaveChangesAsync();
    }

    public async Task AtualizarAsync(OrdemDeServico ordemDeServico)
    {
        _context.OrdensDeServico.Update(ordemDeServico);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<OrdemDeServicoServico>> ObterServicosFinalizadosAsync()
    {
        return await _context.OrdensDeServicoServicos
            .AsNoTracking()
            .Include(x => x.Servico)
            .Where(x => x.IniciadaEm.HasValue && x.FinalizadaEm.HasValue)
            .ToListAsync();
    }

    public async Task<int> ObterUltimoSequencialDoAnoAsync(int ano)
    {
        var ultimaOS = await _context.OrdensDeServico
            .AsNoTracking()
            .Where(x => x.CriadoEm.Year == ano)
            .OrderByDescending(x => x.CriadoEm)
            .FirstOrDefaultAsync();

        if (ultimaOS == null)
            return 0;

        var partes = ultimaOS.NumeroOS.Split('-');
        if (partes.Length == 3 && int.TryParse(partes[2], out var sequencial))
            return sequencial;

        return 0;
    }
}
