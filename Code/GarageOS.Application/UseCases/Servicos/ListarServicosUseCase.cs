using GarageOS.Application.DTOs.Servicos;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Servicos;

public class ListarServicosUseCase
{
    private readonly IServicoRepository _repository;

    public ListarServicosUseCase(IServicoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<ServicoResponse>> ExecutarAsync()
    {
        var servicos = await _repository.ListarTodosAsync();

        return servicos.Select(s => new ServicoResponse
        {
            Id = s.Id,
            NomeServico = s.NomeServico,
            Preco = s.Preco
        });
    }
}
