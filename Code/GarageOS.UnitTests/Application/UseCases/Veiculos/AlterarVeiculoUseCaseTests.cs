using Moq;
using FluentAssertions;
using GarageOS.Application.UseCases.Veiculos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Application.DTOs.Veiculos;

namespace GarageOS.UnitTests.UseCases.Veiculos;

public class AlterarVeiculoUseCaseTests
{
    private readonly Mock<IVeiculoRepository> _repoMock;
    private readonly AlterarVeiculoUseCase _useCase;

    public AlterarVeiculoUseCaseTests()
    {
        _repoMock = new Mock<IVeiculoRepository>();
        _useCase = new AlterarVeiculoUseCase(_repoMock.Object);
    }

    [Fact]
    public async Task DeveAtualizarVeiculo_QuandoDadosValidos()
    {
        var id = Guid.NewGuid();

        var veiculo = new Veiculo("Honda", "City", "ABC1234", 2020, 60000);

        _repoMock.Setup(r => r.ObterPorIdAsync(id))
                 .ReturnsAsync(veiculo);

        var request = new AtualizarVeiculoRequest
        {
            MarcaVeiculo = "Toyota",
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = "DEF5678",
            AnoVeiculo = 2022
        };

        await _useCase.ExecutarAsyncAlterarVeiculo(id, request);

        veiculo.MarcaVeiculo.Should().Be("Toyota");
        veiculo.ModeloVeiculo.Should().Be("Corolla");

        _repoMock.Verify(r => r.AtualizarAsync(veiculo), Times.Once);
    }

    [Fact]
    public async Task DeveLancarException_QuandoVeiculoNaoExiste()
    {
        var id = Guid.NewGuid();

        _repoMock.Setup(r => r.ObterPorIdAsync(id))
                 .ReturnsAsync((Veiculo?)null);

        var request = new AtualizarVeiculoRequest();

        Func<Task> act = async () => await _useCase.ExecutarAsyncAlterarVeiculo(id, request);

        await act.Should()
            .ThrowAsync<VeiculoNaoEncontradoException>();
    }
}