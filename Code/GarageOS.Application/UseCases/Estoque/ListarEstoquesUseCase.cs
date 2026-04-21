using GarageOS.Application.DTOs.Estoques;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Estoques;

public class ListarEstoquesUseCase
{
    private readonly IEstoqueRepository _repository;

    public ListarEstoquesUseCase(IEstoqueRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<EstoqueResponse>> ExecutarAsync()
    {
        var itens = await _repository.ListarTodosAsync();

        return itens.Select(e => new EstoqueResponse
        {
            Id = e.Id,
            Nome = e.Nome,
            Quantidade = e.Quantidade,
            Valor = e.Valor,
            DataEntrada = e.DataEntrada,
            DataSaida = e.DataSaida,
            Fornecedor = e.Fornecedor,
            Status = e.Status.ToString()
        });
    }
}
