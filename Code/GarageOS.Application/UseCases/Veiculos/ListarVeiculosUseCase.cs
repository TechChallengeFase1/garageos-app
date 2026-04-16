using GarageOS.Application.DTOs.Veiculos;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Veiculos;

public class ListarVeiculosUseCase
{
    private readonly IVeiculoRepository _repository;

    public ListarVeiculosUseCase(IVeiculoRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<VeiculoResponse>> ExecutarAsync()
    {
        var servicos = await _repository.ListarTodosAsync();

        return servicos.Select(s => new VeiculoResponse
        {
            Id = s.Id,
            MarcaVeiculo = s.MarcaVeiculo,
            ModeloVeiculo = s.ModeloVeiculo,
            PlacaVeiculo = s.PlacaVeiculo,
            AnoVeiculo = s.AnoVeiculo,
            PrecoVeiculo = s.PrecoVeiculo
        });
    }
}
