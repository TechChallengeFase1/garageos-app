using FluentAssertions;
using Moq;
using GarageOS.Application.DTOs.Estoques;
using GarageOS.Application.UseCases.Estoques;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using EstoqueEntity = GarageOS.Domain.Entities.Estoque;

namespace GarageOS.UnitTests.Application.UseCases.Estoque;

public class AlterarEstoqueUseCaseTests
{
    private readonly Mock<IEstoqueRepository> _repositoryMock = new();
    private readonly AlterarEstoqueUseCase _useCase;

    public AlterarEstoqueUseCaseTests()
    {
        _useCase = new AlterarEstoqueUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveAtualizarEstoque()
    {
        // Arrange
        var id = Guid.NewGuid();
        var estoque = new EstoqueEntity("Pneu", 10, 150.00m, DateTime.Now, "Goodyear");
        var request = new AtualizarEstoqueRequest
        {
            Nome = "Pneu Premium",
            Quantidade = 20,
            Valor = 200.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Pirelli"
        };
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(estoque);
        _repositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<EstoqueEntity>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(id, request);

        // Assert
        result.Should().NotBeNull();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<EstoqueEntity>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComEstoqueNaoEncontrado_DeveLancarEstoqueNaoEncontradoException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new AtualizarEstoqueRequest();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((EstoqueEntity?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EstoqueNaoEncontradoException>(() => _useCase.ExecutarAsync(id, request));
    }

    [Fact]
    public async Task ExecutarAsync_RetornaResponseComDadosAtualizados()
    {
        // Arrange
        var id = Guid.NewGuid();
        var estoque = new EstoqueEntity("Pneu", 10, 150.00m, DateTime.Now, "Goodyear");
        var request = new AtualizarEstoqueRequest
        {
            Nome = "Óleo",
            Quantidade = 100,
            Valor = 50.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Castrol"
        };
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(estoque);
        _repositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<EstoqueEntity>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(id, request);

        // Assert
        result.Valor.Should().Be(request.Valor);
    }
}
