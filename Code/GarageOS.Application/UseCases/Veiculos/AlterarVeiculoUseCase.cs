using GarageOS.Application.DTOs.Veiculos;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Veiculos;

public class AlterarVeiculoUseCase
{
    private readonly IVeiculoRepository _repository;

    public AlterarVeiculoUseCase(IVeiculoRepository repository)
    {
        _repository = repository;
    }

    public async Task<VeiculoResponse> ExecutarAsyncAlterarVeiculo(Guid id, AtualizarVeiculoRequest request)
    {
        var veiculo = await _repository.ObterPorIdAsync(id)
            ?? throw new VeiculoNaoEncontradoException(id);

        veiculo.AtualizarParcial(
    request.MarcaVeiculo,
    request.ModeloVeiculo,
    request.PlacaVeiculo,
    request.AnoVeiculo,
    request.PrecoVeiculo
);

        await _repository.AtualizarAsync(veiculo);

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
