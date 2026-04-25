using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using Moq;
using Xunit;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class AlterarStatusOrdemDeServicoUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock;
    private readonly AlterarStatusOrdemDeServicoUseCase _useCase;

    public AlterarStatusOrdemDeServicoUseCaseTests()
    {
        _repositoryMock = new Mock<IOrdemDeServicoRepository>();
        _useCase = new AlterarStatusOrdemDeServicoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveAlterarStatusParaFinalizada()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Finalizada
        };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        resultado.Status.Should().Be(StatusOrdemDeServico.Finalizada);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveAlterarStatusParaEntregue()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Entregue
        };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        resultado.Status.Should().Be(StatusOrdemDeServico.Entregue);
    }

    [Fact]
    public async Task ExecutarAsync_ComOrdemInexistente_DeveLancarOrdemDeServicoNaoEncontradaException()
    {
        // Arrange
        var ordemId = Guid.NewGuid();

        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Finalizada
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
    public async Task ExecutarAsync_DeveChamarAtualizarRepositorioUmaVez()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Finalizada
        };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarOSComStatusAlterado()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AlterarStatusRequest
        {
            Status = StatusOrdemDeServico.Entregue
        };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        resultado.Should().NotBeNull();
        resultado.NumeroOS.Should().Be("OS-2026-00001");
        resultado.Status.Should().Be(StatusOrdemDeServico.Entregue);
    }
}
