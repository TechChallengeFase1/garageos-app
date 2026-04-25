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

public class AdicionarServicoNaOSUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock;
    private readonly Mock<IServicoRepository> _servicoRepositoryMock;
    private readonly AdicionarServicoNaOSUseCase _useCase;

    public AdicionarServicoNaOSUseCaseTests()
    {
        _repositoryMock = new Mock<IOrdemDeServicoRepository>();
        _servicoRepositoryMock = new Mock<IServicoRepository>();
        _useCase = new AdicionarServicoNaOSUseCase(
            _repositoryMock.Object,
            _servicoRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveAdicionarServicoNaOS()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AdicionarServicoRequest { ServicoId = servicoId };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);
        var servico = new Servico("Troca de óleo", 150.00m);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _servicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(servicoId))
            .ReturnsAsync(servico);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Servicos.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecutarAsync_ComOrdemInexistente_DeveLancarOrdemDeServicoNaoEncontradaException()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();

        var request = new AdicionarServicoRequest { ServicoId = servicoId };

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
    public async Task ExecutarAsync_ComServicoInexistente_DeveLancarServicoNaoEncontradoException()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AdicionarServicoRequest { ServicoId = servicoId };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _servicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(servicoId))
            .ReturnsAsync((Servico?)null);

        // Act
        var act = async () => await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        await act.Should().ThrowAsync<ServicoNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_DeveChamarAtualizarRepositorioUmaVez()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AdicionarServicoRequest { ServicoId = servicoId };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);
        var servico = new Servico("Troca de óleo", 150.00m);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _servicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(servicoId))
            .ReturnsAsync(servico);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarOSComServicoAdicionado()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AdicionarServicoRequest { ServicoId = servicoId };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);
        var servico = new Servico("Troca de óleo", 150.00m);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _servicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(servicoId))
            .ReturnsAsync(servico);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        resultado.Servicos.Should().NotBeEmpty();
        resultado.Servicos.First().ServicoId.Should().Be(servicoId);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarServicoComIdCorreto()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var request = new AdicionarServicoRequest { ServicoId = servicoId };

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);
        var servico = new Servico("Revisão Completa", 500.00m);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        _servicoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(servicoId))
            .ReturnsAsync(servico);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId, request);

        // Assert
        var servicoAdicionado = resultado.Servicos.First();
        servicoAdicionado.ServicoId.Should().Be(servicoId);
    }
}
