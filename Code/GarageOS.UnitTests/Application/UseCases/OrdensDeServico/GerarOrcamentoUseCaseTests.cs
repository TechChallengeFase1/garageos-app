using FluentAssertions;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using Moq;
using Xunit;
using EstoqueEntity = GarageOS.Domain.Entities.Estoque;
using ServicoEntity = GarageOS.Domain.Entities.Servico;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class GerarOrcamentoUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock = new();
    private readonly Mock<IOrcamentoRepository> _orcamentoRepositoryMock = new();
    private readonly GerarOrcamentoUseCase _useCase;

    public GerarOrcamentoUseCaseTests()
    {
        _useCase = new GerarOrcamentoUseCase(
            _repositoryMock.Object,
            _orcamentoRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComOsExistente_DeveRetornarResponseComOrcamento()
    {
        var os = CriarOS();
        ConfigurarMocksHappyPath(os);

        var resultado = await _useCase.ExecutarAsync(os.Id);

        resultado.Should().NotBeNull();
        resultado.Orcamento.Should().NotBeNull();
        resultado.Id.Should().Be(os.Id);
    }

    [Fact]
    public async Task ExecutarAsync_ComOsSemItens_DeveCalcularPrecoZero()
    {
        var os = CriarOS();
        ConfigurarMocksHappyPath(os);

        var resultado = await _useCase.ExecutarAsync(os.Id);

        resultado.Orcamento!.Preco.Should().Be(0m);
    }

    [Fact]
    public async Task ExecutarAsync_ComServicosEEstoques_DeveCalcularPrecoCorreto()
    {
        var os = CriarOS();
        var servico = new ServicoEntity("Alinhamento", 120.00m);
        var estoque = new EstoqueEntity("Oleo 5W30", 10, 45.00m, DateTime.Now, "Castrol");

        var itemServico = new OrdemDeServicoServico(os.Id, servico.Id);
        typeof(OrdemDeServicoServico).GetProperty("Servico")!.SetValue(itemServico, servico);

        var itemEstoque = new OrdemDeServicoEstoque(os.Id, estoque.Id, 2);
        typeof(OrdemDeServicoEstoque).GetProperty("Estoque")!.SetValue(itemEstoque, estoque);

        os.AdicionarServico(itemServico);
        os.AdicionarEstoque(itemEstoque);
        ConfigurarMocksHappyPath(os);

        var resultado = await _useCase.ExecutarAsync(os.Id);

        resultado.Orcamento!.Preco.Should().Be(210.00m); // 120 + (45 * 2)
    }

    [Fact]
    public async Task ExecutarAsync_DeveDefinirStatusOrcamentoComoPendente()
    {
        var os = CriarOS();
        ConfigurarMocksHappyPath(os);

        var resultado = await _useCase.ExecutarAsync(os.Id);

        resultado.Orcamento!.Status.Should().Be(StatusOrcamento.Pendente);
    }

    [Fact]
    public async Task ExecutarAsync_DeveChamarAdicionarOrcamentoUmaVez()
    {
        var os = CriarOS();
        ConfigurarMocksHappyPath(os);

        await _useCase.ExecutarAsync(os.Id);

        _orcamentoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Orcamento>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_DeveChamarAtualizarOsUmaVez()
    {
        var os = CriarOS();
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
        _orcamentoRepositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Orcamento>()), Times.Never);
    }

    private static OrdemDeServico CriarOS() =>
        new("OS-2026-00001", Guid.NewGuid(), Guid.NewGuid());

    private void ConfigurarMocksHappyPath(OrdemDeServico os)
    {
        _repositoryMock
            .Setup(r => r.ObterPorIdAsync(os.Id))
            .ReturnsAsync(os);

        _orcamentoRepositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Orcamento>()))
            .Returns(Task.CompletedTask);

        _repositoryMock
            .Setup(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);
    }
}
