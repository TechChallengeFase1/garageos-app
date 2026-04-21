using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Veiculos;

public class DeletarVeiculoUseCase
{
    private readonly IVeiculoRepository _veiculoRepository;

    public DeletarVeiculoUseCase(IVeiculoRepository veiculoRepository)
    {
        _veiculoRepository = veiculoRepository;
    }

    public async Task<bool> ExecutarAsync(Guid id)
    {
        var veiculo = await _veiculoRepository.ObterPorIdAsync(id);

        if (veiculo == null)
            return false;

        await _veiculoRepository.RemoverAsync(id);
        return true;
    }
}