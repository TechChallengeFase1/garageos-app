using GarageOS.Application.Abstractions;
using GarageOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Saga;

public class ConsultaDaSaga : IConsultaDaSaga
{
    private readonly GarageOSDbContext _context;

    public ConsultaDaSaga(GarageOSDbContext context)
    {
        _context = context;
    }

    public async Task<EstadoDaSaga?> ObterAsync(Guid ordemDeServicoId, CancellationToken cancellationToken = default)
    {
        var saga = await _context.Set<OrdemDeServicoSagaState>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CorrelationId == ordemDeServicoId, cancellationToken);

        return saga is null
            ? null
            : new EstadoDaSaga(saga.CorrelationId, saga.NumeroOS, saga.CurrentState, saga.OrcamentoId, saga.Total,
                saga.PagamentoId, saga.TemPecas, saga.Motivo, saga.CriadaEm, saga.AtualizadaEm);
    }
}
