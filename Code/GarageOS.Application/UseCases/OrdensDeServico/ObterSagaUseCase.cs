using GarageOS.Application.Abstractions;
using GarageOS.Domain.Exceptions;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class ObterSagaUseCase
{
    private readonly IConsultaDaSaga _consulta;

    public ObterSagaUseCase(IConsultaDaSaga consulta)
    {
        _consulta = consulta;
    }

    public async Task<EstadoDaSaga> ExecutarAsync(Guid ordemDeServicoId)
    {
        return await _consulta.ObterAsync(ordemDeServicoId)
            ?? throw new OrdemDeServicoNaoEncontradaException("A OS não tem saga (ainda não foi aberta ou não existe).");
    }
}
