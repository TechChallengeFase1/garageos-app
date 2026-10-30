using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Mappers;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class ObterOrdemDeServicoUseCase
{
    private readonly IOrdemDeServicoRepository _repository;

    public ObterOrdemDeServicoUseCase(IOrdemDeServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrdemDeServicoResponse> ExecutarAsync(Guid id)
    {
        var ordemDeServico = await _repository.ObterPorIdAsync(id)
            ?? throw new OrdemDeServicoNaoEncontradaException();

        return ordemDeServico.ParaResponse();
    }

    public async Task<IEnumerable<HistoricoStatusResponse>> ObterHistoricoAsync(Guid id)
    {
        var ordemDeServico = await _repository.ObterPorIdAsync(id)
            ?? throw new OrdemDeServicoNaoEncontradaException();

        return ordemDeServico.HistoricoParaResponse();
    }
}
