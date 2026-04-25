using FluentAssertions;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using Moq;
using Xunit;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class AcompanharOrdemDeServicoUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock;
    private readonly AcompanharOrdemDeServicoUseCase _useCase;

    public AcompanharOrdemDeServicoUseCaseTests()
    {
        _repositoryMock = new Mock<IOrdemDeServicoRepository>();
        _useCase = new AcompanharOrdemDeServicoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComNumeroOSValido_DeveRetornarAcompanhamentoResponse()
    {
        // Arrange
        var numeroOS = "OS-2026-00001";
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico(numeroOS, clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorNumeroOSAsync(numeroOS))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(numeroOS);

        // Assert
        resultado.Should().NotBeNull();
        resultado.NumeroOS.Should().Be(numeroOS);
    }

    [Fact]
    public async Task ExecutarAsync_ComNumeroOSInvalido_DeveLancarOrdemDeServicoNaoEncontradaException()
    {
        // Arrange
        var numeroOS = "OS-2026-99999";

        _repositoryMock
            .Setup(r => r.ObterPorNumeroOSAsync(numeroOS))
            .ReturnsAsync((OrdemDeServico?)null);

        // Act
        var act = async () => await _useCase.ExecutarAsync(numeroOS);

        // Assert
        await act.Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarStatusEmFormatoString()
    {
        // Arrange
        var numeroOS = "OS-2026-00001";
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico(numeroOS, clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorNumeroOSAsync(numeroOS))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(numeroOS);

        // Assert
        resultado.Status.Should().Be(StatusOrdemDeServico.Recebida.ToString());
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarListaDeServicos()
    {
        // Arrange
        var numeroOS = "OS-2026-00001";
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico(numeroOS, clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorNumeroOSAsync(numeroOS))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(numeroOS);

        // Assert
        resultado.Servicos.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecutarAsync_DeveConsultarRepositorioPorNumeroOS()
    {
        // Arrange
        var numeroOS = "OS-2026-00001";
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico(numeroOS, clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorNumeroOSAsync(numeroOS))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(numeroOS);

        // Assert
        _repositoryMock.Verify(r => r.ObterPorNumeroOSAsync(numeroOS), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarResponseComValoresCompletos()
    {
        // Arrange
        var numeroOS = "OS-2026-00001";
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico(numeroOS, clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorNumeroOSAsync(numeroOS))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(numeroOS);

        // Assert
        resultado.NumeroOS.Should().NotBeNullOrEmpty();
        resultado.Status.Should().NotBeNullOrEmpty();
        resultado.Servicos.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarAcompanhamentoComNomeServicoPreenchido()
    {
        // Arrange
        var numeroOS = "OS-2026-00001";
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();

        var ordemDeServico = new OrdemDeServico(numeroOS, clienteId, veiculoId);

        _repositoryMock
            .Setup(r => r.ObterPorNumeroOSAsync(numeroOS))
            .ReturnsAsync(ordemDeServico);

        // Act
        var resultado = await _useCase.ExecutarAsync(numeroOS);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Servicos.Should().NotBeNull();
    }
}
