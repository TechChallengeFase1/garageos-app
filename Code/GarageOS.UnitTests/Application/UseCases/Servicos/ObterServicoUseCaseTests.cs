using FluentAssertions;
using Moq;
using GarageOS.Application.UseCases.Servicos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;

namespace GarageOS.UnitTests.Application.UseCases.Servicos;

public class ObterServicoUseCaseTests
{
    private readonly Mock<IServicoRepository> _repositoryMock = new();
    private readonly ObterServicoUseCase _useCase;

    public ObterServicoUseCaseTests()
    {
        _useCase = new ObterServicoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdValido_DeveRetornarServicoResponse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var servico = new Servico("Troca de Óleo", 150.00m);
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(servico);

        // Act
        var result = await _useCase.ExecutarAsync(id);

        // Assert
        result.Should().NotBeNull();
        result.NomeServico.Should().Be(servico.NomeServico);
        result.Preco.Should().Be(servico.Preco);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdNaoEncontrado_DeveLancarServicoNaoEncontradoException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Servico)null);

        // Act & Assert
        await Assert.ThrowsAsync<ServicoNaoEncontradoException>(() => _useCase.ExecutarAsync(id));
    }

    [Fact]
    public async Task ExecutarAsync_RetornaResponseComIdCorreto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var servico = new Servico("Revisão", 300.00m);
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(servico);

        // Act
        var result = await _useCase.ExecutarAsync(id);

        // Assert
        result.Id.Should().Be(servico.Id);
    }
}
