using GarageOS.Application.DTOs.Veiculos;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Veiculos;

public class ObterVeiculoUseCase
{
    private readonly IVeiculoRepository _repository;

    public ObterVeiculoUseCase(IVeiculoRepository repository)
    {
        _repository = repository;
    }

    public async Task<VeiculoResponse> ExecutarAsync(Guid id)
    {
        var veiculo = await _repository.ObterPorIdAsync(id)
            ?? throw new VeiculoNaoEncontradoException(id);

        return new VeiculoResponse
        {
            Id = veiculo.Id,
            MarcaVeiculo = veiculo.MarcaVeiculo,
            ModeloVeiculo = veiculo.ModeloVeiculo,
            PlacaVeiculo = veiculo.PlacaVeiculo,
            AnoVeiculo = veiculo.AnoVeiculo,
            PrecoVeiculo = veiculo.PrecoVeiculo
        };
    }
}
