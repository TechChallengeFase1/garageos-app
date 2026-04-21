using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Veiculos;

public class VincularVeiculoClienteUseCase
{
    private readonly IVeiculoRepository _veiculoRepository;
    private readonly IClienteRepository _clienteRepository;

    public VincularVeiculoClienteUseCase(
        IVeiculoRepository veiculoRepository,
        IClienteRepository clienteRepository)
    {
        _veiculoRepository = veiculoRepository;
        _clienteRepository = clienteRepository;
    }

    public async Task ExecutarAsync(Guid veiculoId, Guid clienteId)
    {
        var veiculo = await _veiculoRepository.ObterPorIdAsync(veiculoId);
        if (veiculo == null)
            throw new VeiculoNaoEncontradoException(veiculoId);

        var cliente = await _clienteRepository.ObterPorIdAsync(clienteId);
        if (cliente == null)
            throw new ClienteNaoEncontradoException(clienteId);

        veiculo.VincularCliente(clienteId);

        await _veiculoRepository.AtualizarAsync(veiculo);
    }
}