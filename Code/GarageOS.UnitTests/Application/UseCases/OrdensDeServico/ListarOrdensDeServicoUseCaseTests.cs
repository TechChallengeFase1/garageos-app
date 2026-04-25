using FluentAssertions;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Repositories;
using Moq;
using Xunit;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class ListarOrdensDeServicoUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock;
    private readonly ListarOrdensDeServicoUseCase _useCase;

    public ListarOrdensDeServicoUseCaseTests()
    {
        _repositoryMock = new Mock<IOrdemDeServicoRepository>();
        _useCase = new ListarOrdensDeServicoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComOrdensExistentes_DeveRetornarLista()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordensDeServico = new List<OrdemDeServico>
        {
            new OrdemDeServico("OS-2026-00001", clienteId, veiculoId),
            new OrdemDeServico("OS-2026-00002", clienteId, veiculoId)
        };

        _repositoryMock
            .Setup(r => r.ListarTodosAsync())
            .ReturnsAsync(ordensDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Should().HaveCount(2);
    }

    [Fact]
    public async Task ExecutarAsync_ComOrdensVazias_DeveRetornarListaVazia()
    {
        // Arrange
        var ordensDeServico = new List<OrdemDeServico>();

        _repositoryMock
            .Setup(r => r.ListarTodosAsync())
            .ReturnsAsync(ordensDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync();

        // Assert
        resultado.Should().NotBeNull();
        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecutarAsync_DeveMapearCorretamenteAsOrdens()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordensDeServico = new List<OrdemDeServico>
        {
            new OrdemDeServico("OS-2026-00001", clienteId, veiculoId)
        };

        _repositoryMock
            .Setup(r => r.ListarTodosAsync())
            .ReturnsAsync(ordensDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync();

        // Assert
        var ordem = resultado.First();
        ordem.NumeroOS.Should().Be("OS-2026-00001");
        ordem.Status.Should().Be(StatusOrdemDeServico.Recebida);
        ordem.ClienteId.Should().Be(clienteId);
        ordem.VeiculoId.Should().Be(veiculoId);
    }

    [Fact]
    public async Task ExecutarAsync_DeveIncluirListasVaziasDeServicosEEstoques()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordensDeServico = new List<OrdemDeServico>
        {
            new OrdemDeServico("OS-2026-00001", clienteId, veiculoId)
        };

        _repositoryMock
            .Setup(r => r.ListarTodosAsync())
            .ReturnsAsync(ordensDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync();

        // Assert
        var ordem = resultado.First();
        ordem.Servicos.Should().NotBeNull().And.BeEmpty();
        ordem.Estoques.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarTodosOsCamposPreenchidos()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordensDeServico = new List<OrdemDeServico>
        {
            new OrdemDeServico("OS-2026-00001", clienteId, veiculoId)
        };

        _repositoryMock
            .Setup(r => r.ListarTodosAsync())
            .ReturnsAsync(ordensDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync();

        // Assert
        var ordem = resultado.First();
        ordem.Id.Should().NotBeEmpty();
        ordem.NumeroOS.Should().NotBeNullOrEmpty();
        ordem.Status.Should().NotBe(null);
        ordem.CriadoEm.Should().NotBe(default);
    }
}
