using FluentAssertions;
using Moq;
using GarageOS.Application.DTOs.Estoques;
using GarageOS.Application.UseCases.Estoques;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Repositories;
using EstoqueEntity = GarageOS.Domain.Entities.Estoque;

namespace GarageOS.UnitTests.Application.UseCases.Estoque;

public class CadastrarEstoqueUseCaseTests
{
    private readonly Mock<IEstoqueRepository> _repositoryMock = new();
    private readonly CadastrarEstoqueUseCase _useCase;

    public CadastrarEstoqueUseCaseTests()
    {
        _useCase = new CadastrarEstoqueUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveAdicionarEstoqueComStatusDisponivel()
    {
        // Arrange
        var request = new CriarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 10,
            Valor = 150.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Goodyear"
        };
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<EstoqueEntity>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be(request.Nome);
        result.Quantidade.Should().Be(request.Quantidade);
        result.Status.Should().Be(StatusEstoque.Disponivel.ToString());
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<EstoqueEntity>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComQuantidadeZero_DeveDefinirStatusIndisponivel()
    {
        // Arrange
        var request = new CriarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 0,
            Valor = 150.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Goodyear"
        };
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<EstoqueEntity>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(request);

        // Assert
        result.Quantidade.Should().Be(0);
        result.Status.Should().Be(StatusEstoque.Indisponivel.ToString());
    }

    [Fact]
    public async Task ExecutarAsync_RetornaResponseComDadosCorretos()
    {
        // Arrange
        var request = new CriarEstoqueRequest
        {
            Nome = "Óleo",
            Quantidade = 50,
            Valor = 30.00m,
            DataEntrada = DateTime.Now,
            Fornecedor = "Castrol"
        };
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<EstoqueEntity>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(request);

        // Assert
        result.Id.Should().NotBe(Guid.Empty);
        result.Valor.Should().Be(request.Valor);
        result.Fornecedor.Should().Be(request.Fornecedor);
    }

    [Fact]
    public async Task ExecutarAsync_ComDataSaida_DeveArmazenarDataSaida()
    {
        // Arrange
        var dataEntrada = DateTime.Now.AddDays(-10);
        var dataSaida = DateTime.Now;
        var request = new CriarEstoqueRequest
        {
            Nome = "Pneu",
            Quantidade = 5,
            Valor = 150.00m,
            DataEntrada = dataEntrada,
            DataSaida = dataSaida,
            Fornecedor = "Goodyear"
        };
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<EstoqueEntity>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(request);

        // Assert
        result.DataSaida.Should().Be(dataSaida);
    }
}
