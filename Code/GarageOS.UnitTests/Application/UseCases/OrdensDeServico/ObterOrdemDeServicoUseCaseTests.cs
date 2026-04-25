using FluentAssertions;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using Moq;
using Xunit;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class ObterOrdemDeServicoUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock;
    private readonly ObterOrdemDeServicoUseCase _useCase;

    public ObterOrdemDeServicoUseCaseTests()
    {
        _repositoryMock = new Mock<IOrdemDeServicoRepository>();
        _useCase = new ObterOrdemDeServicoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdValido_DeveRetornarOrdemDeServicoResponse()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().NotBeEmpty();
        resultado.NumeroOS.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecutarAsync_ComIdInvalido_DeveLancarOrdemDeServicoNaoEncontradaException()
    {
        // Arrange
        var ordemId = Guid.NewGuid();

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync((OrdemDeServico?)null);

        // Act
        var act = async () => await _useCase.ExecutarAsync(ordemId);

        // Assert
        await act.Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarTodosOsCamposPreenchidos()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId);

        // Assert
        resultado.NumeroOS.Should().Be("OS-2026-00001");
        resultado.Status.Should().Be(StatusOrdemDeServico.Recebida);
        resultado.ClienteId.Should().Be(clienteId);
        resultado.VeiculoId.Should().Be(veiculoId);
        resultado.CriadoEm.Should().NotBe(default);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarListasVaziasQuandoNaoHouverDados()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId);

        // Assert
        resultado.Servicos.Should().NotBeNull().And.BeEmpty();
        resultado.Estoques.Should().NotBeNull().And.BeEmpty();
        resultado.Orcamento.Should().BeNull();
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarOrcamentoQuandoExistir()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);
        var orcamento = new Orcamento(ordemId, 500.00m);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId);

        // Assert
        resultado.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecutarAsync_DeveChamarRepositorioUmaVez()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico("OS-2026-00001", clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        // Act
        await _useCase.ExecutarAsync(ordemId);

        // Assert
        _repositoryMock.Verify(r => r.ObterPorIdAsync(ordemId), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarNumeroOSValido()
    {
        // Arrange
        var ordemId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var numeroOS = "OS-2026-00001";

        var ordemDeServico = new OrdemDeServico(numeroOS, clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(ordemId))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(ordemId);

        // Assert
        resultado.NumeroOS.Should().Be(numeroOS);
    }
}
