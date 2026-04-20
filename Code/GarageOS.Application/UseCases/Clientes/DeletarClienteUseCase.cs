using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Clientes;

public class DeletarClienteUseCase
{
    private readonly IClienteRepository _repository;

    public DeletarClienteUseCase(IClienteRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecutarAsync(Guid id)
    {
        var cliente = await _repository.ObterPorIdAsync(id)
            ?? throw new ClienteNaoEncontradoException(id);

        cliente.Desativar(); // Soft delete

        await _repository.AtualizarAsync(cliente);
    }
}