using FluentAssertions;
using GarageOS.Application.DTOs.Veiculos;
using GarageOS.Application.UseCases.Veiculos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;
using Moq;

namespace GarageOS.UnitTests.Application.UseCases;

public class CadastrarVeiculoUseCaseTests
{
    private readonly Mock<IVeiculoRepository> _repositoryMock;
    private readonly CadastrarVeiculoUseCase _useCase;

    public CadastrarVeiculoUseCaseTests()
    {
        _repositoryMock = new Mock<IVeiculoRepository>();
        _useCase = new CadastrarVeiculoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveRetornarVeiculoResponse()
    {
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Honda",
            ModeloVeiculo = "Civic",
            PlacaVeiculo = "ABC1234",
            AnoVeiculo = 2020,
            PrecoVeiculo = 50000
        };

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Veiculo>()))
            .Returns(Task.CompletedTask);

        var resultado = await _useCase.ExecutarAsyncCadastrarVeiculo(request);

        resultado.Should().NotBeNull();
        resultado.Id.Should().NotBeEmpty();
        resultado.MarcaVeiculo.Should().Be(request.MarcaVeiculo);
    }

    [Fact]
    public async Task ExecutarAsync_DeveChamarRepositorioUmaVez()
    {
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Toyota",
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = "XYZ1234",
            AnoVeiculo = 2022,
            PrecoVeiculo = 80000
        };

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Veiculo>()))
            .Returns(Task.CompletedTask);

        await _useCase.ExecutarAsyncCadastrarVeiculo(request);

        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Veiculo>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComPlacaVazia_DeveLancarException()
    {
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Honda",
            ModeloVeiculo = "Civic",
            PlacaVeiculo = "",
            AnoVeiculo = 2020,
            PrecoVeiculo = 50000
        };

        var act = async () => await _useCase.ExecutarAsyncCadastrarVeiculo(request);

        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Veiculo>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComPrecoNegativo_DeveLancarException()
    {
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Honda",
            ModeloVeiculo = "Civic",
            PlacaVeiculo = "ABC1234",
            AnoVeiculo = 2020,
            PrecoVeiculo = -100
        };

        var act = async () => await _useCase.ExecutarAsyncCadastrarVeiculo(request);

        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Veiculo>()), Times.Never);
    }
}