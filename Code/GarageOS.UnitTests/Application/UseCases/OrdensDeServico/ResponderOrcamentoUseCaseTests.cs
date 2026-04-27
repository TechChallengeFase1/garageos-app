using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using Moq;
using Xunit;
using EstoqueEntity = GarageOS.Domain.Entities.Estoque;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class ResponderOrcamentoUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock = new();
    private readonly ResponderOrcamentoUseCase _useCase;

    public ResponderOrcamentoUseCaseTests()
    {
        _useCase = new ResponderOrcamentoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoAprovado_DeveRetornarStatusEmExecucao()
    {
        var os = CriarOsComOrcamento();
        ConfigurarMocksHappyPath(os);

        var resultado = await _useCase.ExecutarAsync(os.Id, new ResponderOrcamentoRequest { Aprovado = true });

        resultado.Status.Should().Be(StatusOrdemDeServico.EmExecucao);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoAprovado_DeveAprovarOrcamento()
    {
        var os = CriarOsComOrcamento();
        ConfigurarMocksHappyPath(os);

        var resultado = await _useCase.ExecutarAsync(os.Id, new ResponderOrcamentoRequest { Aprovado = true });

        resultado.Orcamento!.Status.Should().Be(StatusOrcamento.Aprovado);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoReprovado_DeveRetornarStatusFinalizada()
    {
        var os = CriarOsComOrcamento();
        ConfigurarMocksHappyPath(os);

        var resultado = await _useCase.ExecutarAsync(os.Id, new ResponderOrcamentoRequest { Aprovado = false });

        resultado.Status.Should().Be(StatusOrdemDeServico.Finalizada);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoReprovado_DeveRejeitarOrcamento()
    {
        var os = CriarOsComOrcamento();
        ConfigurarMocksHappyPath(os);

        var resultado = await _useCase.ExecutarAsync(os.Id, new ResponderOrcamentoRequest { Aprovado = false });

        resultado.Orcamento!.Status.Should().Be(StatusOrcamento.Rejeitado);
    }

    [Fact]
    public async Task ExecutarAsync_QuandoAprovado_DeveDarBaixaNoEstoque()
    {
        var estoque = new EstoqueEntity("Filtro de Oleo", 10, 30.00m, DateTime.Now, "Bosch");
        var os = CriarOsComOrcamentoEEstoque(estoque, quantidade: 3);
        ConfigurarMocksHappyPath(os);

        await _useCase.ExecutarAsync(os.Id, new ResponderOrcamentoRequest { Aprovado = true });

        estoque.Quantidade.Should().Be(7); // 10 - 3
    }

    [Fact]
    public async Task ExecutarAsync_QuandoReprovado_NaoDeveDarBaixaNoEstoque()
    {
        var estoque = new EstoqueEntity("Filtro de Oleo", 10, 30.00m, DateTime.Now, "Bosch");
        var os = CriarOsComOrcamentoEEstoque(estoque, quantidade: 3);
        ConfigurarMocksHappyPath(os);

        await _useCase.ExecutarAsync(os.Id, new ResponderOrcamentoRequest { Aprovado = false });

        estoque.Quantidade.Should().Be(10); // não alterado
    }

    [Fact]
    public async Task ExecutarAsync_DeveChamarAtualizarRepositorioUmaVez()
    {
        var os = CriarOsComOrcamento();
        ConfigurarMocksHappyPath(os);

        await _useCase.ExecutarAsync(os.Id, new ResponderOrcamentoRequest { Aprovado = true });

        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComOsInexistente_DeveLancarOrdemDeServicoNaoEncontradaException()
    {
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((OrdemDeServico?)null);

        var act = async () => await _useCase.ExecutarAsync(Guid.NewGuid(),
            new ResponderOrcamentoRequest { Aprovado = true });

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

        var act = async () => await _useCase.ExecutarAsync(os.Id,
            new ResponderOrcamentoRequest { Aprovado = true });

        await act.Should().ThrowAsync<OrcamentoNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    private static OrdemDeServico CriarOsComOrcamento()
    {
        var os = new OrdemDeServico("OS-2026-00001", Guid.NewGuid(), Guid.NewGuid());
        os.VincularOrcamento(new Orcamento(os.Id, 150.00m));
        return os;
    }

    private static OrdemDeServico CriarOsComOrcamentoEEstoque(EstoqueEntity estoque, int quantidade)
    {
        var os = new OrdemDeServico("OS-2026-00001", Guid.NewGuid(), Guid.NewGuid());

        var itemEstoque = new OrdemDeServicoEstoque(os.Id, estoque.Id, quantidade);
        typeof(OrdemDeServicoEstoque).GetProperty("Estoque")!.SetValue(itemEstoque, estoque);
        os.AdicionarEstoque(itemEstoque);

        os.VincularOrcamento(new Orcamento(os.Id, estoque.Valor * quantidade));
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
