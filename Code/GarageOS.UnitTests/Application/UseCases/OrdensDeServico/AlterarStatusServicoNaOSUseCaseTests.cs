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

public class AlterarStatusServicoNaOSUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock = new();
    private readonly AlterarStatusServicoNaOSUseCase _useCase;

    public AlterarStatusServicoNaOSUseCaseTests()
    {
        _useCase = new AlterarStatusServicoNaOSUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoIniciar_DeveRetornarStatusIniciado()
    {
        var (os, item) = CriarOsComServico();
        ConfigurarMock(os);

        var resultado = await _useCase.ExecutarAsync(os.NumeroOS, item.Id,
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        resultado.Status.Should().Be(StatusExecucaoServico.Iniciado);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoIniciar_DevePreencherIniciadaEm()
    {
        var (os, item) = CriarOsComServico();
        ConfigurarMock(os);

        var resultado = await _useCase.ExecutarAsync(os.NumeroOS, item.Id,
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        resultado.IniciadaEm.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecutarAsync_QuandoFinalizar_DeveRetornarStatusFinalizado()
    {
        var (os, item) = CriarOsComServicoIniciado();
        ConfigurarMock(os);

        var resultado = await _useCase.ExecutarAsync(os.NumeroOS, item.Id,
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Finalizado });

        resultado.Status.Should().Be(StatusExecucaoServico.Finalizado);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoFinalizar_DevePreencherFinalizadaEm()
    {
        var (os, item) = CriarOsComServicoIniciado();
        ConfigurarMock(os);

        var resultado = await _useCase.ExecutarAsync(os.NumeroOS, item.Id,
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Finalizado });

        resultado.FinalizadaEm.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecutarAsync_DeveChamarAtualizarRepositorioUmaVez()
    {
        var (os, item) = CriarOsComServico();
        ConfigurarMock(os);

        await _useCase.ExecutarAsync(os.NumeroOS, item.Id,
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComOsInexistente_DeveLancarOrdemDeServicoNaoEncontradaException()
    {
        _repositoryMock
            .Setup(r => r.ObterPorNumeroOSComTrackingAsync(It.IsAny<string>()))
            .ReturnsAsync((OrdemDeServico?)null);

        var act = async () => await _useCase.ExecutarAsync("OS-2026-00001", Guid.NewGuid(),
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        await act.Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComServicoItemInexistente_DeveLancarServicoNaOSNaoEncontradoException()
    {
        var os = new OrdemDeServico("OS-2026-00001", Guid.NewGuid(), Guid.NewGuid());
        ConfigurarMock(os);

        var act = async () => await _useCase.ExecutarAsync(os.NumeroOS, Guid.NewGuid(),
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        await act.Should().ThrowAsync<ServicoNaOSNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_IniciarJaIniciado_DeveLancarArgumentException()
    {
        var (os, item) = CriarOsComServicoIniciado();
        ConfigurarMock(os);

        var act = async () => await _useCase.ExecutarAsync(os.NumeroOS, item.Id,
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_FinalizarSemIniciar_DeveLancarArgumentException()
    {
        var (os, item) = CriarOsComServico();
        ConfigurarMock(os);

        var act = async () => await _useCase.ExecutarAsync(os.NumeroOS, item.Id,
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Finalizado });

        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    private static (OrdemDeServico os, OrdemDeServicoServico item) CriarOsComServico()
    {
        var os = new OrdemDeServico("OS-2026-00001", Guid.NewGuid(), Guid.NewGuid());
        var item = new OrdemDeServicoServico(os.Id, Guid.NewGuid());
        os.AdicionarServico(item);
        return (os, item);
    }

    private static (OrdemDeServico os, OrdemDeServicoServico item) CriarOsComServicoIniciado()
    {
        var (os, item) = CriarOsComServico();
        item.IniciarExecucao();
        return (os, item);
    }

    private void ConfigurarMock(OrdemDeServico os)
    {
        _repositoryMock
            .Setup(r => r.ObterPorNumeroOSComTrackingAsync(os.NumeroOS))
            .ReturnsAsync(os);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);
    }
}
