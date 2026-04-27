using FluentAssertions;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using Moq;
using Xunit;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class EnviarOrcamentoUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock = new();
    private readonly EnviarOrcamentoUseCase _useCase;

    public EnviarOrcamentoUseCaseTests()
    {
        _useCase = new EnviarOrcamentoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComOrcamentoExistente_DeveRetornarStatusAguardandoAprovacao()
    {
        var os = CriarOsComOrcamento();
        ConfigurarMocksHappyPath(os);

        var resultado = await _useCase.ExecutarAsync(os.Id);

        resultado.Status.Should().Be(StatusOrdemDeServico.AguardandoAprovacao);
    }

    [Fact]
    public async Task ExecutarAsync_ComOrcamentoExistente_DeveRetornarOrcamentoNaResponse()
    {
        var os = CriarOsComOrcamento();
        ConfigurarMocksHappyPath(os);

        var resultado = await _useCase.ExecutarAsync(os.Id);

        resultado.Orcamento.Should().NotBeNull();
        resultado.Orcamento!.Status.Should().Be(StatusOrcamento.Pendente);
    }

    [Fact]
    public async Task ExecutarAsync_DeveChamarAtualizarRepositorioUmaVez()
    {
        var os = CriarOsComOrcamento();
        ConfigurarMocksHappyPath(os);

        await _useCase.ExecutarAsync(os.Id);

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComOsInexistente_DeveLancarOrdemDeServicoNaoEncontradaException()
    {
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((OrdemDeServico?)null);

        var act = async () => await _useCase.ExecutarAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_SemOrcamentoNaOs_DeveLancarOrcamentoNaoEncontradoException()
    {
        var os = new OrdemDeServico("OS-2026-00001", Guid.NewGuid(), Guid.NewGuid());
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(os.Id))
            .ReturnsAsync(os);

        var act = async () => await _useCase.ExecutarAsync(os.Id);

        await act.Should().ThrowAsync<OrcamentoNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    private static OrdemDeServico CriarOsComOrcamento()
    {
        var os = new OrdemDeServico("OS-2026-00001", Guid.NewGuid(), Guid.NewGuid());
        var orcamento = new Orcamento(os.Id, 200.00m);
        os.VincularOrcamento(orcamento);
        return os;
    }

    private void ConfigurarMocksHappyPath(OrdemDeServico os)
    {
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(os.Id))
            .ReturnsAsync(os);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);
    }
}
