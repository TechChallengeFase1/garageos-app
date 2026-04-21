using GarageOS.Application.DTOs.Estoques;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Estoques;

public class ObterEstoqueUseCase
{
    private readonly IEstoqueRepository _repository;

    public ObterEstoqueUseCase(IEstoqueRepository repository)
    {
        _repository = repository;
    }

    public async Task<EstoqueResponse> ExecutarAsync(Guid id)
    {
        var estoque = await _repository.ObterPorIdAsync(id)
            ?? throw new EstoqueNaoEncontradoException(id);

        return new EstoqueResponse
        {
            Id = estoque.Id,
            Nome = estoque.Nome,
            Quantidade = estoque.Quantidade,
            Valor = estoque.Valor,
            DataEntrada = estoque.DataEntrada,
            DataSaida = estoque.DataSaida,
            Fornecedor = estoque.Fornecedor,
            Status = estoque.Status.ToString()
        };
    }
}
