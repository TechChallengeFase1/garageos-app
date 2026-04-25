using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using Moq;
using Xunit;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class AdicionarEstoqueNaOSUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock;
    private readonly Mock<IEstoqueRepository> _estoqueRepositoryMock;
    private readonly AdicionarEstoqueNaOSUseCase _useCase;

    public AdicionarEstoqueNaOSUseCaseTests()
    {
        _repositoryMock = new Mock<IOrdemDeServicoRepository>();
        _estoqueRepositoryMock = new Mock<IEstoqueRepository>();
        _useCase = new AdicionarEstoqueNaOSUseCase(
            _repositoryMock.Object,
            _estoqueRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveAdicionarEstoqueNaOS()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var estoqueId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = estoqueId,
            Quantidade = 2
        };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);
        var estoque = new Estoque("Óleo 5W30", 10, 150.00m, DateTime.Now, "Castrol");

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _estoqueRepositoryMock
            .Setup(r => r.ObterPorIdAsync(estoqueId))
            .ReturnsAsync(estoque);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Estoques.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecutarAsync_ComOrdemInexistente_DeveLancarOrdemDeServicoNaoEncontradaException()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var estoqueId = Guid.NewGuid();

        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = estoqueId,
            Quantidade = 2
        };

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync((OrdemDeServico?)null);

        // Act
        var act = async () => await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        await act.Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComEstoqueInexistente_DeveLancarEstoqueNaoEncontradoException()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var estoqueId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = estoqueId,
            Quantidade = 2
        };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _estoqueRepositoryMock
            .Setup(r => r.ObterPorIdAsync(estoqueId))
            .ReturnsAsync((Estoque?)null);

        // Act
        var act = async () => await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        await act.Should().ThrowAsync<EstoqueNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_DeveChamarAtualizarRepositorioUmaVez()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var estoqueId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = estoqueId,
            Quantidade = 5
        };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);
        var estoque = new Estoque("Óleo 5W30", 20, 150.00m, DateTime.Now, "Castrol");

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _estoqueRepositoryMock
            .Setup(r => r.ObterPorIdAsync(estoqueId))
            .ReturnsAsync(estoque);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarOSComEstoqueAdicionado()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var estoqueId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = estoqueId,
            Quantidade = 3
        };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);
        var estoque = new Estoque("Óleo 5W30", 20, 150.00m, DateTime.Now, "Castrol");

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _estoqueRepositoryMock
            .Setup(r => r.ObterPorIdAsync(estoqueId))
            .ReturnsAsync(estoque);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        resultado.Estoques.Should().NotBeEmpty();
        resultado.Estoques.First().EstoqueId.Should().Be(estoqueId);
        resultado.Estoques.First().Quantidade.Should().Be(3);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarEstoqueComDadosValidos()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var estoqueId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AdicionarEstoqueRequest
        {
            EstoqueId = estoqueId,
            Quantidade = 2
        };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);
        var estoque = new Estoque("Óleo de Freio DOT 4", 15, 85.00m, DateTime.Now, "Bosch");

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _estoqueRepositoryMock
            .Setup(r => r.ObterPorIdAsync(estoqueId))
            .ReturnsAsync(estoque);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        var estoqueAdicionado = resultado.Estoques.First();
        estoqueAdicionado.EstoqueId.Should().Be(estoqueId);
        estoqueAdicionado.Quantidade.Should().Be(2);
    }
}
