using FluentAssertions;
using Moq;
using GarageOS.Application.UseCases.Estoques;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using EstoqueEntity = GarageOS.Domain.Entities.Estoque;

namespace GarageOS.UnitTests.Application.UseCases.Estoque;

public class ObterEstoqueUseCaseTests
{
    private readonly Mock<IEstoqueRepository> _repositoryMock = new();
    private readonly ObterEstoqueUseCase _useCase;

    public ObterEstoqueUseCaseTests()
    {
        _useCase = new ObterEstoqueUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdValido_DeveRetornarEstoqueResponse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var estoque = new EstoqueEntity("Pneu", 10, 150.00m, DateTime.Now, "Goodyear");
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(estoque);

        // Act
        var result = await _useCase.ExecutarAsync(id);

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be(estoque.Nome);
        result.Quantidade.Should().Be(estoque.Quantidade);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdNaoEncontrado_DeveLancarEstoqueNaoEncontradoException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((EstoqueEntity)null);

        // Act & Assert
        await Assert.ThrowsAsync<EstoqueNaoEncontradoException>(() => _useCase.ExecutarAsync(id));
    }

    [Fact]
    public async Task ExecutarAsync_RetornaResponseComIdCorreto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var estoque = new EstoqueEntity("Óleo", 50, 30.00m, DateTime.Now, "Castrol");
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(estoque);

        // Act
        var result = await _useCase.ExecutarAsync(id);

        // Assert
        result.Id.Should().Be(estoque.Id);
    }
}
