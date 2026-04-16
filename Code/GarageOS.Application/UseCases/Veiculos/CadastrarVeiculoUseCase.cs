using GarageOS.Application.DTOs.Veiculos;
using GarageOS.Application.UseCases.Servicos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;

namespace GarageOS.Application.UseCases.Veiculos;

public class CadastrarVeiculoUseCase
{
    private readonly IVeiculoRepository _repository;

    public CadastrarVeiculoUseCase(IVeiculoRepository repository)
    {
        _repository = repository;
    }

    public async Task<VeiculoResponse> ExecutarAsyncCadastrarVeiculo(CriarVeiculoRequest request)
    {
        var veiculo = new Veiculo(request.MarcaVeiculo, request.ModeloVeiculo, request.PlacaVeiculo, request.AnoVeiculo, request.PrecoVeiculo);

        await _repository.AdicionarAsync(veiculo);

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
