using Moq;
using FluentAssertions;
using GarageOS.Application.UseCases.Veiculos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.UnitTests.UseCases.Veiculos;

public class ObterVeiculoUseCaseTests
{
    private readonly Mock<IVeiculoRepository> _repoMock;
    private readonly ObterVeiculoUseCase _useCase;

    public ObterVeiculoUseCaseTests()
    {
        _repoMock = new Mock<IVeiculoRepository>();
        _useCase = new ObterVeiculoUseCase(_repoMock.Object);
    }

    [Fact]
    public async Task DeveRetornarVeiculo_QuandoIdExiste()
    {
        var id = Guid.NewGuid();

        var veiculo = new Veiculo("Honda", "City", "ABC1234", 2020, 60000);

        _repoMock.Setup(r => r.ObterPorIdAsync(id))
                 .ReturnsAsync(veiculo);

        var resultado = await _useCase.ExecutarAsync(id);

        resultado.Should().NotBeNull();
        resultado.MarcaVeiculo.Should().Be("Honda");
        resultado.ModeloVeiculo.Should().Be("City");
    }

    [Fact]
    public async Task DeveLancarException_QuandoIdNaoExiste()
    {
        var id = Guid.NewGuid();

        _repoMock.Setup(r => r.ObterPorIdAsync(id))
                 .ReturnsAsync((Veiculo?)null);

        Func<Task> act = async () => await _useCase.ExecutarAsync(id);

        await act.Should()
            .ThrowAsync<VeiculoNaoEncontradoException>();
    }
}