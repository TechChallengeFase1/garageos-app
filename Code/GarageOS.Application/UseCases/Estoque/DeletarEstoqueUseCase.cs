using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Estoques;

public class DeletarEstoqueUseCase
{
    private readonly IEstoqueRepository _repository;

    public DeletarEstoqueUseCase(IEstoqueRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecutarAsync(Guid id)
    {
        var estoque = await _repository.ObterPorIdAsync(id)
            ?? throw new EstoqueNaoEncontradoException(id);

        await _repository.RemoverAsync(estoque);
    }
}
