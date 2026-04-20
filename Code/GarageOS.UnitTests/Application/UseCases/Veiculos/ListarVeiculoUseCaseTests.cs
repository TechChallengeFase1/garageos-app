using Xunit;
using Moq;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;
using GarageOS.Application.UseCases.Veiculos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;

namespace GarageOS.UnitTests.UseCases.Veiculos;

public class ListarVeiculosUseCaseTests
{
    private readonly Mock<IVeiculoRepository> _repoMock;
    private readonly ListarVeiculosUseCase _useCase;

    public ListarVeiculosUseCaseTests()
    {
        _repoMock = new Mock<IVeiculoRepository>();
        _useCase = new ListarVeiculosUseCase(_repoMock.Object);
    }

    [Fact]
    public async Task DeveRetornarListaDeVeiculos_QuandoExistiremDados()
    {
        var veiculos = new List<Veiculo>
        {
            new Veiculo("Honda", "City", "ABC1234", 2020, 60000),
            new Veiculo("Toyota", "Corolla", "DEF5678", 2022, 90000)
        };

        _repoMock.Setup(r => r.ListarTodosAsync())
                 .ReturnsAsync(veiculos);

        var resultado = await _useCase.ExecutarAsync();

        resultado.Should().NotBeNull();
        resultado.Should().HaveCount(2);

        resultado.First().MarcaVeiculo.Should().Be("Honda");
        resultado.First().ModeloVeiculo.Should().Be("City");
    }

    [Fact]
    public async Task DeveRetornarListaVazia_QuandoNaoExistiremDados()
    {
        var veiculos = new List<Veiculo>();

        _repoMock.Setup(r => r.ListarTodosAsync())
                 .ReturnsAsync(veiculos);

        var resultado = await _useCase.ExecutarAsync();

        resultado.Should().NotBeNull();
        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task DeveMapearCorretamenteParaVeiculoResponse()
    {
        var veiculos = new List<Veiculo>
        {
            new Veiculo("Honda", "City", "ABC1234", 2020, 60000)
        };

        _repoMock.Setup(r => r.ListarTodosAsync())
                 .ReturnsAsync(veiculos);

        var resultado = await _useCase.ExecutarAsync();

        var item = resultado.First();

        item.MarcaVeiculo.Should().Be("Honda");
        item.ModeloVeiculo.Should().Be("City");
        item.PlacaVeiculo.Should().Be("ABC1234");
        item.AnoVeiculo.Should().Be(2020);
        item.PrecoVeiculo.Should().Be(60000);
    }
}