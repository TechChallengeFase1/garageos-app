using GarageOS.Application.DTOs.Estoques;
using GarageOS.Domain.Repositories;
using EstoqueEntity = GarageOS.Domain.Entities.Estoque;

namespace GarageOS.Application.UseCases.Estoques;

public class CadastrarEstoqueUseCase
{
    private readonly IEstoqueRepository _repository;

    public CadastrarEstoqueUseCase(IEstoqueRepository repository)
    {
        _repository = repository;
    }

    public async Task<EstoqueResponse> ExecutarAsync(CriarEstoqueRequest request)
    {
        var estoque = new EstoqueEntity(
            request.Nome,
            request.Quantidade,
            request.Valor,
            request.DataEntrada,
            request.Fornecedor,
            request.DataSaida);

        await _repository.AdicionarAsync(estoque);

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
