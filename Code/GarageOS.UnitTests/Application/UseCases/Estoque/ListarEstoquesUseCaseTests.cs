using FluentAssertions;
using Moq;
using GarageOS.Application.UseCases.Estoques;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;
using EstoqueEntity = GarageOS.Domain.Entities.Estoque;

namespace GarageOS.UnitTests.Application.UseCases.Estoque;

public class ListarEstoquesUseCaseTests
{
    private readonly Mock<IEstoqueRepository> _repositoryMock = new();
    private readonly ListarEstoquesUseCase _useCase;

    public ListarEstoquesUseCaseTests()
    {
        _useCase = new ListarEstoquesUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComEstoquesExistentes_DeveRetornarLista()
    {
        // Arrange
        var estoques = new List<EstoqueEntity>
        {
            new("Pneu", 10, 150.00m, DateTime.Now, "Goodyear"),
            new("Óleo", 50, 30.00m, DateTime.Now, "Castrol"),
            new("Filtro", 20, 25.00m, DateTime.Now, "Bosch")
        };
        _repositoryMock.Setup(r => r.ListarTodosAsync()).ReturnsAsync(estoques);

        // Act
        var result = await _useCase.ExecutarAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExecutarAsync_SemEstoques_DeveRetornarListaVazia()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ListarTodosAsync()).ReturnsAsync(new List<EstoqueEntity>());

        // Act
        var result = await _useCase.ExecutarAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecutarAsync_MapeiaEstoquesCorretamente()
    {
        // Arrange
        var estoque = new EstoqueEntity("Pneu", 10, 150.00m, DateTime.Now, "Goodyear");
        var estoques = new List<EstoqueEntity> { estoque };
        _repositoryMock.Setup(r => r.ListarTodosAsync()).ReturnsAsync(estoques);

        // Act
        var result = await _useCase.ExecutarAsync();

        // Assert
        var response = result.First();
        response.Nome.Should().Be(estoque.Nome);
        response.Quantidade.Should().Be(estoque.Quantidade);
    }
}
