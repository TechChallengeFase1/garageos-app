using FluentAssertions;
using Moq;
using GarageOS.Application.UseCases.Servicos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;

namespace GarageOS.UnitTests.Application.UseCases.Servicos;

public class ListarServicosUseCaseTests
{
    private readonly Mock<IServicoRepository> _repositoryMock = new();
    private readonly ListarServicosUseCase _useCase;

    public ListarServicosUseCaseTests()
    {
        _useCase = new ListarServicosUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComServicosExistentes_DeveRetornarLista()
    {
        // Arrange
        var servicos = new List<Servico>
        {
            new("Troca de Óleo", 150.00m),
            new("Revisão", 300.00m),
            new("Pintura", 1000.00m)
        };
        _repositoryMock.Setup(r => r.ListarTodosAsync()).ReturnsAsync(servicos);

        // Act
        var result = await _useCase.ExecutarAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExecutarAsync_SemServicos_DeveRetornarListaVazia()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ListarTodosAsync()).ReturnsAsync(new List<Servico>());

        // Act
        var result = await _useCase.ExecutarAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecutarAsync_MapeiaServicosCorretamente()
    {
        // Arrange
        var servico = new Servico("Troca de Óleo", 150.00m);
        var servicos = new List<Servico> { servico };
        _repositoryMock.Setup(r => r.ListarTodosAsync()).ReturnsAsync(servicos);

        // Act
        var result = await _useCase.ExecutarAsync();

        // Assert
        var response = result.First();
        response.NomeServico.Should().Be(servico.NomeServico);
        response.Preco.Should().Be(servico.Preco);
    }
}
