using FluentAssertions;
using Moq;
using GarageOS.Application.DTOs.Servicos;
using GarageOS.Application.UseCases.Servicos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.UnitTests.Application.UseCases.Servicos;

public class AlterarServicoUseCaseTests
{
    private readonly Mock<IServicoRepository> _repositoryMock = new();
    private readonly AlterarServicoUseCase _useCase;

    public AlterarServicoUseCaseTests()
    {
        _useCase = new AlterarServicoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveAtualizarServico()
    {
        // Arrange
        var id = Guid.NewGuid();
        var servico = new Servico("Troca de Óleo", 150.00m);
        var request = new AtualizarServicoRequest { NomeServico = "Revisão Completa", Preco = 500.00m };
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(servico);
        _repositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<Servico>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(id, request);

        // Assert
        result.NomeServico.Should().Be(request.NomeServico);
        result.Preco.Should().Be(request.Preco);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Servico>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComServicoNaoEncontrado_DeveLancarServicoNaoEncontradoException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new AtualizarServicoRequest();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Servico?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ServicoNaoEncontradoException>(() => _useCase.ExecutarAsync(id, request));
    }

    [Fact]
    public async Task ExecutarAsync_RetornaResponseComDadosCorretos()
    {
        // Arrange
        var id = Guid.NewGuid();
        var servico = new Servico("Troca de Óleo", 150.00m);
        var request = new AtualizarServicoRequest { NomeServico = "Pintura", Preco = 1000.00m };
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(servico);
        _repositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<Servico>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(id, request);

        // Assert
        result.Id.Should().Be(servico.Id);
        result.NomeServico.Should().Be(request.NomeServico);
    }
}
