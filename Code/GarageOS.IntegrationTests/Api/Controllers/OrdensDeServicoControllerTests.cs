using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using GarageOS.Application.DTOs.Clientes;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.DTOs.Servicos;
using GarageOS.Application.DTOs.Veiculos;
using GarageOS.Domain.Enums;
using GarageOS.IntegrationTests.Fixtures;
using GarageOS.IntegrationTests.Helpers;

namespace GarageOS.IntegrationTests.Api.Controllers;

public partial class OrdensDeServicoControllerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public OrdensDeServicoControllerTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [GeneratedRegex(@"^OS-\d{4}-\d{5}$")]
    private static partial Regex FormatoNumeroOS();

    // ── Abertura ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_AbrirCompleta_DeveAbrirEmRecebidaComPrecosCopiados()
    {
        var servico = await CadastrarServicoAsync("Revisão Completa", 200m);
        var peca = _factory.Estoque.Cadastrar("Filtro de óleo", 35m, 10);

        var response = await AbrirAsync([servico.Id], [new PecaRequest { EstoqueId = peca.Id, Quantidade = 2 }]);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var os = (await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>(JsonDefaults.Options))!;
        os.Status.Should().Be(StatusOrdemDeServico.Recebida);
        os.NumeroOS.Should().MatchRegex(FormatoNumeroOS());
        os.Servicos.Should().ContainSingle(s => s.ServicoNome == "Revisão Completa" && s.PrecoUnitario == 200m);
        os.Pecas.Should().ContainSingle(p => p.Nome == "Filtro de óleo" && p.Quantidade == 2 && p.Subtotal == 70m);
        os.ValorEstimado.Should().Be(270m);
    }

    [Fact]
    public async Task POST_AbrirCompleta_Simultaneas_DevemTerNumerosDistintos()
    {
        var servico = await CadastrarServicoAsync("Lavagem", 50m);
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();

        var respostas = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", new AbrirOrdemDeServicoCompletaRequest
            {
                ClienteId = cliente.Id, VeiculoId = veiculo.Id, ServicosIds = [servico.Id], Pecas = []
            })));

        respostas.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.Created);
        var numeros = await Task.WhenAll(respostas.Select(async r =>
            (await r.Content.ReadFromJsonAsync<OrdemDeServicoResponse>(JsonDefaults.Options))!.NumeroOS));
        numeros.Distinct().Should().HaveCount(5);
    }

    [Fact]
    public async Task POST_AbrirCompleta_ClienteInexistente_DeveRetornar404()
    {
        var servico = await CadastrarServicoAsync("Alinhamento", 80m);
        var veiculo = await CadastrarVeiculoAsync();

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = Guid.NewGuid(), VeiculoId = veiculo.Id, ServicosIds = [servico.Id], Pecas = []
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_AbrirCompleta_PecaInexistenteNoExecucao_DeveRetornar404()
    {
        var servico = await CadastrarServicoAsync("Balanceamento", 60m);

        var response = await AbrirAsync([servico.Id], [new PecaRequest { EstoqueId = Guid.NewGuid(), Quantidade = 1 }]);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_AbrirCompleta_PecaSemQuantidade_DeveRetornar409()
    {
        var servico = await CadastrarServicoAsync("Freios", 150m);
        var peca = _factory.Estoque.Cadastrar("Pastilha", 90m, 1);

        var response = await AbrirAsync([servico.Id], [new PecaRequest { EstoqueId = peca.Id, Quantidade = 4 }]);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_AbrirCompleta_Invalido_DeveRetornar400()
    {
        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa",
            new AbrirOrdemDeServicoCompletaRequest { ServicosIds = [], Pecas = null });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_AbrirCompleta_SemToken_DeveRetornar401()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/ordensdeservico/abertura-completa")
        {
            Content = JsonContent.Create(new AbrirOrdemDeServicoCompletaRequest())
        };
        request.Headers.Add("X-Test-No-Auth", "1");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Itens ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task POST_AdicionarServico_DeveIniciarDiagnostico()
    {
        var os = await CriarOSAsync();
        var outro = await CadastrarServicoAsync("Higienização", 70m);

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os.Id}/servicos", new AdicionarServicoRequest { ServicoId = outro.Id });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizada = (await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>(JsonDefaults.Options))!;
        atualizada.Status.Should().Be(StatusOrdemDeServico.EmDiagnostico);
        atualizada.Servicos.Should().HaveCount(2);
    }

    [Fact]
    public async Task POST_AdicionarPeca_DeveIncluirComValorDoMomento()
    {
        var os = await CriarOSAsync();
        var peca = _factory.Estoque.Cadastrar("Bateria", 480m, 3);

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os.Id}/pecas", new AdicionarPecaRequest { EstoqueId = peca.Id, Quantidade = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>(JsonDefaults.Options))!
            .Pecas.Should().ContainSingle(p => p.EstoqueId == peca.Id && p.ValorUnitario == 480m);
    }

    [Fact]
    public async Task POST_AdicionarServico_OSInexistente_DeveRetornar404()
    {
        var servico = await CadastrarServicoAsync("Revisão", 100m);

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{Guid.NewGuid()}/servicos", new AdicionarServicoRequest { ServicoId = servico.Id });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Status e histórico ───────────────────────────────────────────────────

    [Fact]
    public async Task POST_SolicitarOrcamento_DeveAbrirASagaEResponder202()
    {
        var os = await CriarOSAsync();

        var response = await _client.PostAsync($"/api/ordensdeservico/{os.Id}/orcamento/solicitar", null);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        // A saga roda em segundo plano (transporte em memória): espera ela registrar o pedido.
        EstadoDaSagaDto? saga = null;
        for (var tentativa = 0; tentativa < 50 && saga?.Etapa != "AguardandoOrcamento"; tentativa++)
        {
            await Task.Delay(200);
            var resposta = await _client.GetAsync($"/api/ordensdeservico/{os.Id}/saga");
            if (resposta.IsSuccessStatusCode)
                saga = await resposta.Content.ReadFromJsonAsync<EstadoDaSagaDto>(JsonDefaults.Options);
        }
        saga!.Etapa.Should().Be("AguardandoOrcamento");
        saga.NumeroOS.Should().Be(os.NumeroOS);
    }

    [Fact]
    public async Task POST_SolicitarOrcamento_OSInexistente_DeveRetornar404()
    {
        var response = await _client.PostAsync($"/api/ordensdeservico/{Guid.NewGuid()}/orcamento/solicitar", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Saga_Inexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/ordensdeservico/{Guid.NewGuid()}/saga");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record EstadoDaSagaDto(Guid OrdemDeServicoId, string NumeroOS, string Etapa);

    [Fact]
    public async Task PATCH_Status_ManualDiferenteDeEntregue_DeveRetornar400()
    {
        var os = await CriarOSAsync();

        var response = await AlterarStatusAsync(os.Id, StatusOrdemDeServico.Cancelada);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Obter_Inexistente_DeveRetornar404()
    {
        (await _client.GetAsync($"/api/ordensdeservico/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync($"/api/ordensdeservico/{Guid.NewGuid()}/historico")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Acompanhamento público ───────────────────────────────────────────────

    [Fact]
    public async Task GET_Acompanhar_SemToken_DeveMostrarEtapas()
    {
        var os = await CriarOSAsync();
        var outro = await CadastrarServicoAsync("Higienização " + Guid.NewGuid().ToString("N")[..6], 70m);
        await _client.PostAsJsonAsync($"/api/ordensdeservico/{os.Id}/servicos", new AdicionarServicoRequest { ServicoId = outro.Id });
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/ordensdeservico/acompanhar/{os.NumeroOS}");
        request.Headers.Add("X-Test-No-Auth", "1");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var acompanhamento = (await response.Content.ReadFromJsonAsync<AcompanhamentoOSResponse>(JsonDefaults.Options))!;
        acompanhamento.Status.Should().Be("EmDiagnostico");
        acompanhamento.Historico.Select(h => h.Status).Should().Equal("Recebida", "EmDiagnostico");
    }

    [Fact]
    public async Task GET_Acompanhar_Inexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/ordensdeservico/acompanhar/OS-1999-00001");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RotasQueSairamDoOS_DevemRetornar404()
    {
        var os = await CriarOSAsync();

        (await _client.GetAsync("/api/estoques")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.PostAsync($"/api/ordensdeservico/{os.Id}/orcamento", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.GetAsync("/api/ordensdeservico/aging")).StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    // ── Apoio ────────────────────────────────────────────────────────────────

    private async Task<HttpResponseMessage> AbrirAsync(List<Guid> servicos, List<PecaRequest> pecas)
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        return await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente.Id, VeiculoId = veiculo.Id, ServicosIds = servicos, Pecas = pecas
        });
    }

    private async Task<OrdemDeServicoResponse> CriarOSAsync()
    {
        var servico = await CadastrarServicoAsync("Serviço " + Guid.NewGuid().ToString("N")[..8], 99m);
        var response = await AbrirAsync([servico.Id], []);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>(JsonDefaults.Options))!;
    }

    private Task<HttpResponseMessage> AlterarStatusAsync(Guid id, StatusOrdemDeServico status, string? correlationId = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/ordensdeservico/{id}/status")
        {
            Content = JsonContent.Create(new AlterarStatusRequest { Status = status }, options: JsonDefaults.Options)
        };
        if (correlationId is not null)
            request.Headers.Add("X-Correlation-Id", correlationId);
        return _client.SendAsync(request);
    }

    private async Task<ClienteResponse> CadastrarClienteAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/clientes", new CriarClienteRequest
        {
            Nome = "Cliente OS Teste",
            Documento = Dados.Cpf(),
            Email = Dados.Email(),
            Telefone = Dados.Telefone(),
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "Sao Paulo",
            Estado = "SP",
            Cep = "01001000"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<ClienteResponse>(JsonDefaults.Options))!;
    }

    private async Task<VeiculoResponse> CadastrarVeiculoAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/veiculos", new CriarVeiculoRequest
        {
            MarcaVeiculo = "Toyota",
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = Dados.Placa(),
            AnoVeiculo = 2022,
            PrecoVeiculo = 90000
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<VeiculoResponse>(JsonDefaults.Options))!;
    }

    private async Task<ServicoResponse> CadastrarServicoAsync(string nome, decimal preco)
    {
        var response = await _client.PostAsJsonAsync("/api/servicos", new CriarServicoRequest { NomeServico = nome, Preco = preco });
        return (await response.Content.ReadFromJsonAsync<ServicoResponse>(JsonDefaults.Options))!;
    }
}
