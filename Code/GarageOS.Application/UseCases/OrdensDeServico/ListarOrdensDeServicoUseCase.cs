using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.Mappers;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.OrdensDeServico;

public class ListarOrdensDeServicoUseCase
{
    private readonly IOrdemDeServicoRepository _repository;

    public ListarOrdensDeServicoUseCase(IOrdemDeServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<OrdemDeServicoResponse>> ExecutarAsync()
    {
        var ordensDeServico = await _repository.ListarTodosAsync();
        return ordensDeServico.Select(os => os.ParaResponse());
    }
}
