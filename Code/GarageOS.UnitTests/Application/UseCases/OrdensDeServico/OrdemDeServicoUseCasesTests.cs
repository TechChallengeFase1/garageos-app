using FluentAssertions;
using GarageOS.Application.Abstractions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Contracts.V1;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.ValueObjects;
using Moq;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class OrdemDeServicoUseCasesTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repository = new();
    private readonly Mock<IClienteRepository> _clientes = new();
    private readonly Mock<IVeiculoRepository> _veiculos = new();
    private readonly Mock<IServicoRepository> _servicos = new();
    private readonly Mock<IEstoqueClient> _estoque = new();
    private readonly Mock<IMetricasDeNegocio> _metricas = new();
    private readonly Mock<IContextoDaRequisicao> _contexto = new();
    private readonly Mock<IPublicadorDeEventos> _publicador = new();

    private readonly Servico _servico = new("Revisão", 250m);
    private readonly PecaDoEstoque _peca = new(Guid.NewGuid(), "Filtro de ar", 60m, 5);

    public OrdemDeServicoUseCasesTests()
    {
        _contexto.Setup(c => c.CorrelationId).Returns("corr-teste");
        _repository.Setup(r => r.ObterProximoSequencialDoAnoAsync(It.IsAny<int>())).ReturnsAsync(42);
        _servicos.Setup(r => r.ObterPorIdAsync(_servico.Id)).ReturnsAsync(_servico);
        _estoque.Setup(e => e.ObterPecaAsync(_peca.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_peca);
    }

    private AbrirOrdemDeServicoCompletaUseCase Abrir() =>
        new(_repository.Object, _clientes.Object, _veiculos.Object, _servicos.Object, _estoque.Object, _metricas.Object, _contexto.Object, _publicador.Object);

    private AbrirOrdemDeServicoCompletaRequest PedidoValido()
    {
        var cliente = new Cliente("Maria", "52998224725", "maria@teste.com", "11999998888",
            new Endereco("Rua A", "1", "Centro", "São Paulo", "SP", "01001000"));
        var veiculo = new Veiculo("Fiat", "Uno", "ABC1D23", 2020, 30000m);
        _clientes.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync(cliente);
        _veiculos.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync(veiculo);

        return new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = Guid.NewGuid(),
            VeiculoId = Guid.NewGuid(),
            ServicosIds = [_servico.Id],
            Pecas = [new PecaRequest { EstoqueId = _peca.Id, Quantidade = 2 }]
        };
    }

    private static OrdemDeServico OSRecebida() =>
        new("OS-2026-00001", Guid.NewGuid(), Guid.NewGuid(), [(Guid.NewGuid(), "Revisão", 100m)], []);

    [Fact]
    public async Task Abrir_DeveCopiarPrecosNumerarEGravar()
    {
        var resposta = await Abrir().ExecutarAsync(PedidoValido());

        resposta.NumeroOS.Should().Be($"OS-{DateTime.UtcNow.Year}-00042");
        resposta.Status.Should().Be(StatusOrdemDeServico.Recebida);
        resposta.Servicos.Should().ContainSingle(s => s.ServicoNome == "Revisão" && s.PrecoUnitario == 250m);
        resposta.Pecas.Should().ContainSingle(p => p.Nome == "Filtro de ar" && p.ValorUnitario == 60m && p.Quantidade == 2);
        resposta.ValorEstimado.Should().Be(370m);
        _repository.Verify(r => r.AdicionarAsync(It.Is<OrdemDeServico>(os => os.Historico.Single().CorrelationId == "corr-teste")), Times.Once);
        _metricas.Verify(m => m.OrdemDeServicoCriada(resposta.NumeroOS, It.IsAny<Guid>()), Times.Once);
        _publicador.Verify(p => p.PublicarAsync(It.Is<OrdemDeServicoAberta>(e => e.NumeroOS == resposta.NumeroOS), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SolicitarOrcamento_DevePublicarItensComPrecosCopiados()
    {
        var os = new OrdemDeServico("OS-2026-00009", Guid.NewGuid(), Guid.NewGuid(),
            [(Guid.NewGuid(), "Revisão", 200m)], [(Guid.NewGuid(), "Filtro", 30m, 2)]);
        _repository.Setup(r => r.ObterPorIdAsync(os.Id)).ReturnsAsync(os);
        OrcamentoSolicitado? publicado = null;
        _publicador.Setup(p => p.PublicarAsync(It.IsAny<OrcamentoSolicitado>(), It.IsAny<CancellationToken>()))
            .Callback<OrcamentoSolicitado, CancellationToken>((m, _) => publicado = m);

        await new SolicitarOrcamentoUseCase(_repository.Object, _publicador.Object).ExecutarAsync(os.Id);

        publicado!.Itens.Should().HaveCount(2);
        publicado.Itens.Should().Contain(i => i.Tipo == "Peca" && i.Quantidade == 2 && i.ValorUnitario == 30m);
        publicado.Pecas.Should().ContainSingle(p => p.Quantidade == 2);
        publicado.Servicos.Should().ContainSingle(s => s.Nome == "Revisão");
        _repository.Verify(r => r.AtualizarAsync(os), Times.Once);
    }

    [Fact]
    public async Task SolicitarOrcamento_DepoisDoDiagnostico_DeveLancar()
    {
        var os = OSRecebida();
        os.AlterarStatus(StatusOrdemDeServico.Cancelada, "Manual");
        _repository.Setup(r => r.ObterPorIdAsync(os.Id)).ReturnsAsync(os);

        var acao = () => new SolicitarOrcamentoUseCase(_repository.Object, _publicador.Object).ExecutarAsync(os.Id);

        await acao.Should().ThrowAsync<OrdemDeServicoStatusInvalidoException>();
        _publicador.Verify(p => p.PublicarAsync(It.IsAny<OrcamentoSolicitado>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SolicitarOrcamento_SemServico_DeveLancar()
    {
        var os = new OrdemDeServico("OS-2026-00010", Guid.NewGuid(), Guid.NewGuid(), [], []);
        _repository.Setup(r => r.ObterPorIdAsync(os.Id)).ReturnsAsync(os);

        var acao = () => new SolicitarOrcamentoUseCase(_repository.Object, _publicador.Object).ExecutarAsync(os.Id);

        await acao.Should().ThrowAsync<OrdemDeServicoStatusInvalidoException>();
    }

    [Fact]
    public async Task SolicitarOrcamento_OSInexistente_DeveLancar()
    {
        var acao = () => new SolicitarOrcamentoUseCase(_repository.Object, _publicador.Object).ExecutarAsync(Guid.NewGuid());

        await acao.Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
    }

    [Fact]
    public async Task ObterSaga_DeveDevolverOuLancar()
    {
        var consulta = new Mock<IConsultaDaSaga>();
        var id = Guid.NewGuid();
        consulta.Setup(c => c.ObterAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EstadoDaSaga(id, "OS-1", "AguardandoAprovacao", null, null, null, false, null, DateTime.UtcNow, DateTime.UtcNow));
        var useCase = new ObterSagaUseCase(consulta.Object);

        (await useCase.ExecutarAsync(id)).Etapa.Should().Be("AguardandoAprovacao");
        await useCase.Invoking(u => u.ExecutarAsync(Guid.NewGuid())).Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
    }

    [Fact]
    public async Task Abrir_ClienteInexistente_NaoDeveGravar()
    {
        var acao = () => Abrir().ExecutarAsync(new AbrirOrdemDeServicoCompletaRequest { ClienteId = Guid.NewGuid() });

        await acao.Should().ThrowAsync<ClienteNaoEncontradoException>();
        _repository.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task Abrir_VeiculoInexistente_DeveLancar()
    {
        var pedido = PedidoValido();
        _veiculos.Setup(r => r.ObterPorIdAsync(It.IsAny<Guid>())).ReturnsAsync((Veiculo?)null);

        var acao = () => Abrir().ExecutarAsync(pedido);

        await acao.Should().ThrowAsync<VeiculoNaoEncontradoException>();
    }

    [Fact]
    public async Task Abrir_ServicoInexistente_DeveLancar()
    {
        var pedido = PedidoValido();
        pedido.ServicosIds = [Guid.NewGuid()];

        var acao = () => Abrir().ExecutarAsync(pedido);

        await acao.Should().ThrowAsync<ServicoNaoEncontradoException>();
    }

    [Fact]
    public async Task Abrir_PecaInexistenteNoExecucao_DeveLancarENaoGravar()
    {
        var pedido = PedidoValido();
        pedido.Pecas = [new PecaRequest { EstoqueId = Guid.NewGuid(), Quantidade = 1 }];

        var acao = () => Abrir().ExecutarAsync(pedido);

        await acao.Should().ThrowAsync<PecaNaoEncontradaException>();
        _repository.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task Abrir_PecaSemQuantidade_DeveLancar()
    {
        var pedido = PedidoValido();
        pedido.Pecas = [new PecaRequest { EstoqueId = _peca.Id, Quantidade = 6 }];

        var acao = () => Abrir().ExecutarAsync(pedido);

        await acao.Should().ThrowAsync<EstoqueInsuficienteException>();
    }

    [Fact]
    public async Task Abrir_ExecucaoFora_DevePropagarIndisponibilidade()
    {
        var pedido = PedidoValido();
        _estoque.Setup(e => e.ObterPecaAsync(_peca.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServicoExternoIndisponivelException("Execução Service"));

        var acao = () => Abrir().ExecutarAsync(pedido);

        await acao.Should().ThrowAsync<ServicoExternoIndisponivelException>();
    }

    [Fact]
    public async Task Abrir_FalhaAoGravar_DeveEmitirMetricaDeFalha()
    {
        var pedido = PedidoValido();
        _repository.Setup(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>())).ThrowsAsync(new InvalidOperationException("banco"));

        var acao = () => Abrir().ExecutarAsync(pedido);

        await acao.Should().ThrowAsync<InvalidOperationException>();
        _metricas.Verify(m => m.FalhaNoProcessamento(It.IsAny<string>(), "InvalidOperationException"), Times.Once);
    }

    [Fact]
    public async Task AdicionarServico_DeveCopiarPrecoEIniciarDiagnostico()
    {
        var os = OSRecebida();
        _repository.Setup(r => r.ObterPorIdAsync(os.Id)).ReturnsAsync(os);

        var resposta = await new AdicionarServicoNaOSUseCase(_repository.Object, _servicos.Object, _contexto.Object)
            .ExecutarAsync(os.Id, new AdicionarServicoRequest { ServicoId = _servico.Id });

        resposta.Status.Should().Be(StatusOrdemDeServico.EmDiagnostico);
        resposta.Servicos.Should().Contain(s => s.ServicoId == _servico.Id && s.PrecoUnitario == 250m);
        _repository.Verify(r => r.AtualizarAsync(os), Times.Once);
    }

    [Fact]
    public async Task AdicionarServico_OSInexistente_DeveLancar()
    {
        var acao = () => new AdicionarServicoNaOSUseCase(_repository.Object, _servicos.Object, _contexto.Object)
            .ExecutarAsync(Guid.NewGuid(), new AdicionarServicoRequest { ServicoId = _servico.Id });

        await acao.Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
    }

    [Fact]
    public async Task AdicionarServico_ServicoInexistente_DeveLancar()
    {
        var os = OSRecebida();
        _repository.Setup(r => r.ObterPorIdAsync(os.Id)).ReturnsAsync(os);

        var acao = () => new AdicionarServicoNaOSUseCase(_repository.Object, _servicos.Object, _contexto.Object)
            .ExecutarAsync(os.Id, new AdicionarServicoRequest { ServicoId = Guid.NewGuid() });

        await acao.Should().ThrowAsync<ServicoNaoEncontradoException>();
    }

    [Fact]
    public async Task AdicionarPeca_DeveConferirNoExecucaoEGravar()
    {
        var os = OSRecebida();
        _repository.Setup(r => r.ObterPorIdAsync(os.Id)).ReturnsAsync(os);

        var resposta = await new AdicionarPecaNaOSUseCase(_repository.Object, _estoque.Object, _contexto.Object)
            .ExecutarAsync(os.Id, new AdicionarPecaRequest { EstoqueId = _peca.Id, Quantidade = 3 });

        resposta.Pecas.Should().ContainSingle(p => p.EstoqueId == _peca.Id && p.Subtotal == 180m);
        _repository.Verify(r => r.AtualizarAsync(os), Times.Once);
    }

    [Fact]
    public async Task AdicionarPeca_OSInexistente_DeveLancar()
    {
        var acao = () => new AdicionarPecaNaOSUseCase(_repository.Object, _estoque.Object, _contexto.Object)
            .ExecutarAsync(Guid.NewGuid(), new AdicionarPecaRequest { EstoqueId = _peca.Id, Quantidade = 1 });

        await acao.Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
    }

    [Fact]
    public async Task AlterarStatus_Valido_DeveGravarComOrigemManualEEmitirMetrica()
    {
        var os = OSRecebida();
        _repository.Setup(r => r.ObterPorIdAsync(os.Id)).ReturnsAsync(os);

        var resposta = await new AlterarStatusOrdemDeServicoUseCase(_repository.Object, _metricas.Object, _contexto.Object)
            .ExecutarAsync(os.Id, new AlterarStatusRequest { Status = StatusOrdemDeServico.Cancelada });

        resposta.Status.Should().Be(StatusOrdemDeServico.Cancelada);
        os.Historico.Last().Should().Match<HistoricoStatusOS>(h => h.Origem == "Manual" && h.CorrelationId == "corr-teste");
        _metricas.Verify(m => m.StatusAlterado(os.NumeroOS, "Recebida", "Cancelada", It.IsAny<double>()), Times.Once);
    }

    [Fact]
    public async Task AlterarStatus_TransicaoInvalida_DeveEmitirFalhaENaoGravar()
    {
        var os = OSRecebida();
        _repository.Setup(r => r.ObterPorIdAsync(os.Id)).ReturnsAsync(os);

        var acao = () => new AlterarStatusOrdemDeServicoUseCase(_repository.Object, _metricas.Object, _contexto.Object)
            .ExecutarAsync(os.Id, new AlterarStatusRequest { Status = StatusOrdemDeServico.Entregue });

        await acao.Should().ThrowAsync<OrdemDeServicoStatusInvalidoException>();
        _repository.Verify(r => r.AtualizarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
        _metricas.Verify(m => m.FalhaNoProcessamento(os.NumeroOS, nameof(OrdemDeServicoStatusInvalidoException)), Times.Once);
    }

    [Fact]
    public async Task AlterarStatus_OSInexistente_DeveLancar()
    {
        var acao = () => new AlterarStatusOrdemDeServicoUseCase(_repository.Object, _metricas.Object, _contexto.Object)
            .ExecutarAsync(Guid.NewGuid(), new AlterarStatusRequest { Status = StatusOrdemDeServico.Cancelada });

        await acao.Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
    }

    [Fact]
    public async Task Listar_DeveMapearTodas()
    {
        _repository.Setup(r => r.ListarTodosAsync()).ReturnsAsync([OSRecebida(), OSRecebida()]);

        var lista = await new ListarOrdensDeServicoUseCase(_repository.Object).ExecutarAsync();

        lista.Should().HaveCount(2);
    }

    [Fact]
    public async Task Obter_E_Historico_DevemDevolverAOS()
    {
        var os = OSRecebida();
        os.AlterarStatus(StatusOrdemDeServico.EmDiagnostico, "Manual", "c-2");
        _repository.Setup(r => r.ObterPorIdAsync(os.Id)).ReturnsAsync(os);
        var useCase = new ObterOrdemDeServicoUseCase(_repository.Object);

        var resposta = await useCase.ExecutarAsync(os.Id);
        var historico = (await useCase.ObterHistoricoAsync(os.Id)).ToList();

        resposta.Id.Should().Be(os.Id);
        historico.Select(h => h.StatusNovo).Should().Equal("Recebida", "EmDiagnostico");
        historico.Last().Should().Match<HistoricoStatusResponse>(h => h.StatusAnterior == "Recebida" && h.CorrelationId == "c-2");
    }

    [Fact]
    public async Task Obter_Inexistente_DeveLancar()
    {
        var useCase = new ObterOrdemDeServicoUseCase(_repository.Object);

        await useCase.Invoking(u => u.ExecutarAsync(Guid.NewGuid())).Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
        await useCase.Invoking(u => u.ObterHistoricoAsync(Guid.NewGuid())).Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
    }

    [Fact]
    public async Task Acompanhar_DeveMostrarServicosEEtapasSemDadosInternos()
    {
        var os = OSRecebida();
        os.AlterarStatus(StatusOrdemDeServico.EmDiagnostico, "Manual", "segredo");
        _repository.Setup(r => r.ObterPorNumeroOSAsync(os.NumeroOS)).ReturnsAsync(os);

        var resposta = await new AcompanharOrdemDeServicoUseCase(_repository.Object).ExecutarAsync(os.NumeroOS);

        resposta.Status.Should().Be("EmDiagnostico");
        resposta.Servicos.Should().ContainSingle(s => s.NomeServico == "Revisão");
        resposta.Historico.Select(h => h.Status).Should().Equal("Recebida", "EmDiagnostico");
    }

    [Fact]
    public async Task Acompanhar_Inexistente_DeveLancar()
    {
        var acao = () => new AcompanharOrdemDeServicoUseCase(_repository.Object).ExecutarAsync("OS-0000-00000");

        await acao.Should().ThrowAsync<OrdemDeServicoNaoEncontradaException>();
    }
}
