using FluentAssertions;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;
using Moq;
using Xunit;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class CalcularAgingServicosUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock = new();
    private readonly CalcularAgingServicosUseCase _useCase;

    public CalcularAgingServicosUseCaseTests()
    {
        _useCase = new CalcularAgingServicosUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_SemServicosFinalizados_DeveRetornarListaVazia()
    {
        _repositoryMock
            .Setup(r => r.ObterServicosFinalizadosAsync())
            .ReturnsAsync([]);

        var resultado = await _useCase.ExecutarAsync();

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecutarAsync_ComUmServicoFinalizado_DeveRetornarUmItem()
    {
        var servicoId = Guid.NewGuid();
        var item = CriarItemFinalizado(servicoId, "Troca de Oleo", 60);
        _repositoryMock
            .Setup(r => r.ObterServicosFinalizadosAsync())
            .ReturnsAsync([item]);

        var resultado = await _useCase.ExecutarAsync();

        resultado.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExecutarAsync_ComUmServico_DeveCalcularTempoMedioCorreto()
    {
        var servicoId = Guid.NewGuid();
        var item = CriarItemFinalizado(servicoId, "Alinhamento", 90);
        _repositoryMock
            .Setup(r => r.ObterServicosFinalizadosAsync())
            .ReturnsAsync([item]);

        var resultado = (await _useCase.ExecutarAsync()).ToList();

        resultado[0].TempoMedioMinutos.Should().BeApproximately(90, 1);
    }

    [Fact]
    public async Task ExecutarAsync_ComDuasExecucoesDoMesmoServico_DeveCalcularMedia()
    {
        var servicoId = Guid.NewGuid();
        var item1 = CriarItemFinalizado(servicoId, "Balanceamento", 60);
        var item2 = CriarItemFinalizado(servicoId, "Balanceamento", 120);
        _repositoryMock
            .Setup(r => r.ObterServicosFinalizadosAsync())
            .ReturnsAsync([item1, item2]);

        var resultado = (await _useCase.ExecutarAsync()).ToList();

        resultado.Should().HaveCount(1);
        resultado[0].TotalExecucoes.Should().Be(2);
        resultado[0].TempoMedioMinutos.Should().BeApproximately(90, 1); // (60 + 120) / 2
    }

    [Fact]
    public async Task ExecutarAsync_ComDoisTiposDeServico_DeveRetornarDoisItens()
    {
        var servicoId1 = Guid.NewGuid();
        var servicoId2 = Guid.NewGuid();
        var item1 = CriarItemFinalizado(servicoId1, "Alinhamento", 60);
        var item2 = CriarItemFinalizado(servicoId2, "Balanceamento", 30);
        _repositoryMock
            .Setup(r => r.ObterServicosFinalizadosAsync())
            .ReturnsAsync([item1, item2]);

        var resultado = (await _useCase.ExecutarAsync()).ToList();

        resultado.Should().HaveCount(2);
        resultado.Should().Contain(r => r.ServicoNome == "Alinhamento");
        resultado.Should().Contain(r => r.ServicoNome == "Balanceamento");
    }

    [Fact]
    public async Task ExecutarAsync_DeveRetornarTotalExecucoesCorreto()
    {
        var servicoId = Guid.NewGuid();
        var itens = Enumerable.Range(0, 5)
            .Select(_ => CriarItemFinalizado(servicoId, "Revisao", 45))
            .ToList();
        _repositoryMock
            .Setup(r => r.ObterServicosFinalizadosAsync())
            .ReturnsAsync(itens);

        var resultado = (await _useCase.ExecutarAsync()).ToList();

        resultado[0].TotalExecucoes.Should().Be(5);
    }

    [Fact]
    public async Task ExecutarAsync_TempoMenorQue60Min_DeveFormatarComoMinutos()
    {
        var servicoId = Guid.NewGuid();
        var item = CriarItemFinalizado(servicoId, "Calibragem", 45);
        _repositoryMock
            .Setup(r => r.ObterServicosFinalizadosAsync())
            .ReturnsAsync([item]);

        var resultado = (await _useCase.ExecutarAsync()).ToList();

        resultado[0].TempoMedioFormatado.Should().EndWith("min");
    }

    [Fact]
    public async Task ExecutarAsync_TempoMaiorQue60Min_DeveFormatarComHoras()
    {
        var servicoId = Guid.NewGuid();
        var item = CriarItemFinalizado(servicoId, "Revisao Completa", 90);
        _repositoryMock
            .Setup(r => r.ObterServicosFinalizadosAsync())
            .ReturnsAsync([item]);

        var resultado = (await _useCase.ExecutarAsync()).ToList();

        resultado[0].TempoMedioFormatado.Should().Contain("h");
    }

    private static OrdemDeServicoServico CriarItemFinalizado(Guid servicoId, string nomeServico, double minutosExecucao)
    {
        var osId = Guid.NewGuid();
        var item = new OrdemDeServicoServico(osId, servicoId);

        var fim = DateTime.UtcNow;
        var inicio = fim.AddMinutes(-minutosExecucao);

        typeof(OrdemDeServicoServico).GetProperty("IniciadaEm")!.SetValue(item, inicio);
        typeof(OrdemDeServicoServico).GetProperty("FinalizadaEm")!.SetValue(item, fim);

        var servico = new Servico(nomeServico, 100m);
        typeof(OrdemDeServicoServico).GetProperty("Servico")!.SetValue(item, servico);

        return item;
    }
}
