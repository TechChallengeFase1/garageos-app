using FluentAssertions;
using Moq;
using GarageOS.Application.UseCases.Estoques;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using EstoqueEntity = GarageOS.Domain.Entities.Estoque;

namespace GarageOS.UnitTests.Application.UseCases.Estoque;

public class DeletarEstoqueUseCaseTests
{
    private readonly Mock<IEstoqueRepository> _repositoryMock = new();
    private readonly DeletarEstoqueUseCase _useCase;

    public DeletarEstoqueUseCaseTests()
    {
        _useCase = new DeletarEstoqueUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdValido_DeveDeletarEstoque()
    {
        // Arrange
        var id = Guid.NewGuid();
        var estoque = new EstoqueEntity("Pneu", 10, 150.00m, DateTime.Now, "Goodyear");
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(estoque);
        _repositoryMock.Setup(r => r.RemoverAsync(It.IsAny<EstoqueEntity>())).Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.RemoverAsync(It.Is<EstoqueEntity>(e => e.Id == estoque.Id)), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdNaoEncontrado_DeveLancarEstoqueNaoEncontradoException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((EstoqueEntity?)null);

        // Act & Assert
        await Assert.ThrowsAsync<EstoqueNaoEncontradoException>(() => _useCase.ExecutarAsync(id));
    }

    [Fact]
    public async Task ExecutarAsync_UsaHardDelete()
    {
        // Arrange
        var id = Guid.NewGuid();
        var estoque = new EstoqueEntity("Pneu", 10, 150.00m, DateTime.Now, "Goodyear");
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(estoque);
        _repositoryMock.Setup(r => r.RemoverAsync(It.IsAny<EstoqueEntity>())).Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.RemoverAsync(It.IsAny<EstoqueEntity>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ChamaRepositorioUmaVez()
    {
        // Arrange
        var id = Guid.NewGuid();
        var estoque = new EstoqueEntity("Pneu", 10, 150.00m, DateTime.Now, "Goodyear");
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(estoque);
        _repositoryMock.Setup(r => r.RemoverAsync(It.IsAny<EstoqueEntity>())).Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.RemoverAsync(It.IsAny<EstoqueEntity>()), Times.Once);
    }
}
