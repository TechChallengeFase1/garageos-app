using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using FluentAssertions;
using GarageOS.Application.DTOs.Clientes;
using GarageOS.Application.DTOs.Estoques;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.DTOs.Servicos;
using GarageOS.Application.DTOs.Veiculos;
using GarageOS.Domain.Enums;
using GarageOS.IntegrationTests.Fixtures;
using GarageOS.IntegrationTests.Helpers;

namespace GarageOS.IntegrationTests.Api.Controllers;

public class OrdensDeServicoControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public OrdensDeServicoControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/ordensdeservico ─────────────────────────────────────────────

    [Fact]
    public async Task GET_ListarOrdensDeServico_DeveRetornar200ComLista()
    {
        var response = await _client.GetAsync("/api/ordensdeservico");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await response.Content.ReadFromJsonAsync<IEnumerable<OrdemDeServicoResponse>>();
        lista.Should().NotBeNull();
    }

    // ── POST /api/ordensdeservico/abertura-completa ──────────────────────────

    [Fact]
    public async Task POST_AbrirCompleta_ComDadosValidos_DeveRetornar201()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        var servico = await CadastrarServicoAsync("Revisao Completa", 200.00m);

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id,
            ServicosIds = [servico!.Id],
            Pecas = []
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var os = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        os!.Id.Should().NotBeEmpty();
        os.NumeroOS.Should().StartWith("OS-");
        os.ClienteId.Should().Be(cliente.Id);
        os.VeiculoId.Should().Be(veiculo.Id);
    }

    [Fact]
    public async Task POST_AbrirCompleta_ComClienteInexistente_DeveRetornar400()
    {
        var veiculo = await CadastrarVeiculoAsync();
        var servico = await CadastrarServicoAsync("Troca de Pneu", 100.00m);

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = Guid.NewGuid(),
            VeiculoId = veiculo!.Id,
            ServicosIds = [servico!.Id],
            Pecas = []
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_AbrirCompleta_ComVeiculoInexistente_DeveRetornar400()
    {
        var cliente = await CadastrarClienteAsync();
        var servico = await CadastrarServicoAsync("Troca de Pastilha", 90.00m);

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = Guid.NewGuid(),
            ServicosIds = [servico!.Id],
            Pecas = []
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_AbrirCompleta_DuasOsSequenciais_DeveTerNumerosSequenciais()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        var servico = await CadastrarServicoAsync("Diagnostico", 50.00m);

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id,
            ServicosIds = [servico!.Id],
            Pecas = []
        };

        var r1 = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);
        var r2 = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);

        var os1 = await r1.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        var os2 = await r2.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();

        os1!.NumeroOS.Should().NotBe(os2!.NumeroOS);
    }

    [Fact]
    public async Task POST_AbrirCompleta_ComPecasVazia_DeveRetornar201ComServicoVinculadoESemEstoques()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        var servico = await CadastrarServicoAsync("Alinhamento Direcao", 130.00m);

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id,
            ServicosIds = [servico!.Id],
            Pecas = []
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var os = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        os!.NumeroOS.Should().StartWith("OS-");
        os.Servicos.Should().ContainSingle(s => s.ServicoId == servico.Id);
        os.Estoques.Should().BeEmpty();
    }

    [Fact]
    public async Task POST_AbrirCompleta_ComPecasPreenchidas_DevemEstarVinculadasSemDecrementarEstoque()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        var servico = await CadastrarServicoAsync("Troca de Correia", 220.00m);
        var estoque = await CadastrarEstoqueAsync("Correia Dentada", 8, 60.00m);

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id,
            ServicosIds = [servico!.Id],
            Pecas = [new PecaRequest { EstoqueId = estoque!.Id, Quantidade = 2 }]
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var os = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        os!.Estoques.Should().ContainSingle(e => e.EstoqueId == estoque.Id && e.Quantidade == 2);

        var estoqueAposAbertura = await _client.GetFromJsonAsync<EstoqueResponse>($"/api/estoques/{estoque.Id}");
        estoqueAposAbertura!.Quantidade.Should().Be(8);
    }

    [Fact]
    public async Task POST_AbrirCompleta_ComPecasAusenteDoJson_DeveRetornar400()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        var servico = await CadastrarServicoAsync("Revisao 10 mil km", 180.00m);

        var payloadSemPecas = new Dictionary<string, object>
        {
            ["clienteId"] = cliente!.Id,
            ["veiculoId"] = veiculo!.Id,
            ["servicosIds"] = new[] { servico!.Id }
        };
        var json = JsonSerializer.Serialize(payloadSemPecas);
        json.Should().NotContain("pecas", "o teste precisa garantir que a chave 'pecas' esteja totalmente ausente do JSON");

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/ordensdeservico/abertura-completa", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_AbrirCompleta_ComServicosIdsVazio_DeveRetornar400()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id,
            ServicosIds = [],
            Pecas = []
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_AbrirCompleta_ComServicoInexistenteNaLista_DeveRetornar400ENadaPersistido()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        var servicoValido = await CadastrarServicoAsync("Servico Valido", 70.00m);

        var contagemAntes = (await _client.GetFromJsonAsync<List<OrdemDeServicoResponse>>("/api/ordensdeservico"))!.Count;

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id,
            ServicosIds = [servicoValido!.Id, Guid.NewGuid()],
            Pecas = []
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var contagemDepois = (await _client.GetFromJsonAsync<List<OrdemDeServicoResponse>>("/api/ordensdeservico"))!.Count;
        contagemDepois.Should().Be(contagemAntes);
    }

    [Fact]
    public async Task POST_AbrirCompleta_ComEstoqueInexistenteNaLista_DeveRetornar400ENadaPersistido()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        var servico = await CadastrarServicoAsync("Troca de Amortecedor", 300.00m);

        var contagemAntes = (await _client.GetFromJsonAsync<List<OrdemDeServicoResponse>>("/api/ordensdeservico"))!.Count;

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id,
            ServicosIds = [servico!.Id],
            Pecas = [new PecaRequest { EstoqueId = Guid.NewGuid(), Quantidade = 1 }]
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var contagemDepois = (await _client.GetFromJsonAsync<List<OrdemDeServicoResponse>>("/api/ordensdeservico"))!.Count;
        contagemDepois.Should().Be(contagemAntes);
    }

    [Fact]
    public async Task POST_AbrirCompleta_ComQuantidadeMenorOuIgualAZero_DeveRetornar400()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        var servico = await CadastrarServicoAsync("Troca de Vela", 60.00m);
        var estoque = await CadastrarEstoqueAsync("Vela de Ignicao", 20, 15.00m);

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id,
            ServicosIds = [servico!.Id],
            Pecas = [new PecaRequest { EstoqueId = estoque!.Id, Quantidade = 0 }]
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_AbrirCompleta_NumeroOS_DeveSeguirFormatoSequencial()
    {
        var os = await CriarOrdemDeServicoAsync();

        os!.NumeroOS.Should().MatchRegex(@"^OS-\d{4}-\d{5}$");
    }

    [Fact]
    public async Task POST_AbrirCompleta_SemTokenJWT_DeveRetornar401()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        var servico = await CadastrarServicoAsync("Servico Sem Token", 40.00m);

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id,
            ServicosIds = [servico!.Id],
            Pecas = []
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/api/ordensdeservico/abertura-completa")
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Add("X-Test-No-Auth", "true");

        var response = await _client.SendAsync(httpRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task POST_RotaAntigaOrdensDeServico_DeveRetornar404()
    {
        var response = await _client.PostAsJsonAsync("/api/ordensdeservico", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/ordensdeservico/{id} ────────────────────────────────────────

    [Fact]
    public async Task GET_ObterOrdemDeServico_ComIdExistente_DeveRetornar200()
    {
        var os = await CriarOrdemDeServicoAsync();

        var response = await _client.GetAsync($"/api/ordensdeservico/{os!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        resultado!.Id.Should().Be(os.Id);
        resultado.NumeroOS.Should().Be(os.NumeroOS);
    }

    [Fact]
    public async Task GET_ObterOrdemDeServico_ComIdInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/ordensdeservico/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/ordensdeservico/{id}/servicos ──────────────────────────────

    [Fact]
    public async Task POST_AdicionarServico_ComDadosValidos_DeveRetornar200()
    {
        var os = await CriarOrdemDeServicoAsync();
        var servico = await CadastrarServicoAsync("Alinhamento", 120.00m);

        var request = new AdicionarServicoRequest { ServicoId = servico!.Id };

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/servicos", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        // SPEC_DEVIATION: CriarOrdemDeServicoAsync() now opens the OS via abertura-completa,
        // which requires >= 1 ServicosIds (AC4), so the OS already has one base servico.
        // Reason: assert the newly added servico is present, not an exact total count of 1.
        resultado!.Servicos.Should().Contain(s => s.ServicoId == servico.Id);
    }

    [Fact]
    public async Task POST_AdicionarServico_ComOsInexistente_DeveRetornar404()
    {
        var servico = await CadastrarServicoAsync("Balanceamento", 80.00m);

        var request = new AdicionarServicoRequest { ServicoId = servico!.Id };

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{Guid.NewGuid()}/servicos", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_AdicionarServico_ComServicoInexistente_DeveRetornar400()
    {
        var os = await CriarOrdemDeServicoAsync();

        var request = new AdicionarServicoRequest { ServicoId = Guid.NewGuid() };

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/servicos", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── POST /api/ordensdeservico/{id}/estoques ──────────────────────────────

    [Fact]
    public async Task POST_AdicionarEstoque_ComDadosValidos_DeveRetornar200()
    {
        var os = await CriarOrdemDeServicoAsync();
        var estoque = await CadastrarEstoqueAsync("Oleo Motor", 10, 45.00m);

        var request = new AdicionarEstoqueRequest { EstoqueId = estoque!.Id, Quantidade = 2 };

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/estoques", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        resultado!.Estoques.Should().HaveCount(1);
        resultado.Estoques.First().EstoqueId.Should().Be(estoque.Id);
        resultado.Estoques.First().Quantidade.Should().Be(2);
    }

    [Fact]
    public async Task POST_AdicionarEstoque_ComOsInexistente_DeveRetornar404()
    {
        var estoque = await CadastrarEstoqueAsync("Filtro Combustivel", 5, 30.00m);

        var request = new AdicionarEstoqueRequest { EstoqueId = estoque!.Id, Quantidade = 1 };

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{Guid.NewGuid()}/estoques", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_AdicionarEstoque_ComEstoqueInexistente_DeveRetornar400()
    {
        var os = await CriarOrdemDeServicoAsync();

        var request = new AdicionarEstoqueRequest { EstoqueId = Guid.NewGuid(), Quantidade = 1 };

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/estoques", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── PATCH /api/ordensdeservico/{id}/status ───────────────────────────────

    [Fact]
    public async Task PATCH_AlterarStatus_ParaFinalizada_DeveRetornar200()
    {
        var os = await CriarOrdemDeServicoAsync();

        var request = new AlterarStatusRequest { Status = StatusOrdemDeServico.Finalizada };

        var response = await _client.PatchAsJsonAsync($"/api/ordensdeservico/{os!.Id}/status", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        resultado!.Status.Should().Be(StatusOrdemDeServico.Finalizada);
    }

    [Fact]
    public async Task PATCH_AlterarStatus_ComOsInexistente_DeveRetornar404()
    {
        var request = new AlterarStatusRequest { Status = StatusOrdemDeServico.Entregue };

        var response = await _client.PatchAsJsonAsync($"/api/ordensdeservico/{Guid.NewGuid()}/status", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── GET /api/ordensdeservico/acompanhar/{numeroOS} ───────────────────────

    [Fact]
    public async Task GET_Acompanhar_ComNumeroOSExistente_DeveRetornar200()
    {
        var os = await CriarOrdemDeServicoAsync();

        var response = await _client.GetAsync($"/api/ordensdeservico/acompanhar/{os!.NumeroOS}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GET_Acompanhar_ComNumeroOSInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync("/api/ordensdeservico/acompanhar/OS-2099-99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<OrdemDeServicoResponse?> CriarOrdemDeServicoAsync()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();
        var servico = await CadastrarServicoAsync("Servico Base " + Guid.NewGuid().ToString("N")[..8], 99.00m);

        var request = new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id,
            ServicosIds = [servico!.Id],
            Pecas = []
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico/abertura-completa", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Falha ao criar OS: cliente={cliente.Id}, veiculo={veiculo.Id}, servico={servico.Id}, status={response.StatusCode}");
        return await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
    }

    private async Task<ClienteResponse?> CadastrarClienteAsync()
    {
        var request = new CriarClienteRequest
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
            Cep = "01234567"
        };

        var response = await _client.PostAsJsonAsync("/api/clientes", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Falha ao cadastrar cliente doc={request.Documento} email={request.Email}, status={response.StatusCode}");
        return await response.Content.ReadFromJsonAsync<ClienteResponse>();
    }

    private async Task<VeiculoResponse?> CadastrarVeiculoAsync()
    {
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Toyota",
            ModeloVeiculo = "Corolla",
            PlacaVeiculo = Dados.Placa(),
            AnoVeiculo = 2022,
            PrecoVeiculo = 90000
        };

        var response = await _client.PostAsJsonAsync("/api/veiculos", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Falha ao cadastrar veiculo placa={request.PlacaVeiculo}, status={response.StatusCode}");
        return await response.Content.ReadFromJsonAsync<VeiculoResponse>();
    }

    private async Task<ServicoResponse?> CadastrarServicoAsync(string nome, decimal preco)
    {
        var request = new CriarServicoRequest { NomeServico = nome, Preco = preco };
        var response = await _client.PostAsJsonAsync("/api/servicos", request);
        return await response.Content.ReadFromJsonAsync<ServicoResponse>();
    }

    private async Task<EstoqueResponse?> CadastrarEstoqueAsync(string nome, int quantidade, decimal valor)
    {
        var request = new CriarEstoqueRequest
        {
            Nome = nome,
            Quantidade = quantidade,
            Valor = valor,
            DataEntrada = DateTime.UtcNow,
            Fornecedor = "Fornecedor Teste"
        };

        var response = await _client.PostAsJsonAsync("/api/estoques", request);
        return await response.Content.ReadFromJsonAsync<EstoqueResponse>();
    }

    // ── POST /api/ordensdeservico/{id}/orcamento ─────────────────────────────

    [Fact]
    public async Task POST_GerarOrcamento_ComServicosEEstoques_DeveRetornar200ComPrecoCalculado()
    {
        var os = await CriarOrdemDeServicoAsync();
        var servico = await CadastrarServicoAsync("Troca de Oleo", 150.00m);
        var estoque = await CadastrarEstoqueAsync("Oleo 5W30", 10, 45.00m);

        await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/servicos",
            new AdicionarServicoRequest { ServicoId = servico!.Id });
        await _client.PostAsJsonAsync($"/api/ordensdeservico/{os.Id}/estoques",
            new AdicionarEstoqueRequest { EstoqueId = estoque!.Id, Quantidade = 2 });

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os.Id}/orcamento", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        resultado!.Orcamento.Should().NotBeNull();
        // SPEC_DEVIATION: CriarOrdemDeServicoAsync() now opens the OS via abertura-completa,
        // which links a base servico (preco 99.00m) since the endpoint requires >= 1 ServicosIds.
        // Reason: total includes that base servico's price: 99 (base) + 150 + (45 * 2) = 339.
        resultado.Orcamento!.Preco.Should().Be(339.00m);
        resultado.Orcamento.Status.Should().Be(StatusOrcamento.Pendente);
    }

    [Fact]
    public async Task POST_GerarOrcamento_SemEstoquesNemServicosAdicionais_DeveRetornar200ComPrecoDoServicoBase()
    {
        var os = await CriarOrdemDeServicoAsync();

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/orcamento", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        resultado!.Orcamento.Should().NotBeNull();
        // SPEC_DEVIATION: CriarOrdemDeServicoAsync() now opens the OS via abertura-completa,
        // which links a base servico (preco 99.00m) since the endpoint requires >= 1 ServicosIds.
        // Reason: with no additional servicos/estoques, the orcamento total equals the base servico's price.
        resultado.Orcamento!.Preco.Should().Be(99.00m);
    }

    [Fact]
    public async Task POST_GerarOrcamento_ComOsInexistente_DeveRetornar404()
    {
        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{Guid.NewGuid()}/orcamento", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── POST /api/ordensdeservico/{id}/orcamento/enviar ──────────────────────

    [Fact]
    public async Task POST_EnviarOrcamento_ComOrcamentoGerado_DeveRetornar200ComStatusAguardandoAprovacao()
    {
        var os = await CriarOrdemDeServicoAsync();
        await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/orcamento", new { });

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os.Id}/orcamento/enviar", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        resultado!.Status.Should().Be(StatusOrdemDeServico.AguardandoAprovacao);
    }

    [Fact]
    public async Task POST_EnviarOrcamento_SemOrcamento_DeveRetornar404()
    {
        var os = await CriarOrdemDeServicoAsync();

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/orcamento/enviar", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_EnviarOrcamento_ComOsInexistente_DeveRetornar404()
    {
        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{Guid.NewGuid()}/orcamento/enviar", new { });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /api/ordensdeservico/{id}/orcamento/resposta ───────────────────

    [Fact]
    public async Task PATCH_ResponderOrcamento_Aprovado_DeveRetornar200ComStatusEmExecucao()
    {
        var os = await CriarOsComOrcamentoEnviadoAsync();

        var response = await _client.PatchAsJsonAsync($"/api/ordensdeservico/{os!.Id}/orcamento/resposta",
            new ResponderOrcamentoRequest { Aprovado = true });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        resultado!.Status.Should().Be(StatusOrdemDeServico.EmExecucao);
        resultado.Orcamento!.Status.Should().Be(StatusOrcamento.Aprovado);
    }

    [Fact]
    public async Task PATCH_ResponderOrcamento_Aprovado_DeveDecrementarQuantidadeNoEstoque()
    {
        var estoque = await CadastrarEstoqueAsync("Filtro de Oleo", 10, 30.00m);
        var os = await CriarOrdemDeServicoAsync();

        await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/estoques",
            new AdicionarEstoqueRequest { EstoqueId = estoque!.Id, Quantidade = 3 });
        await _client.PostAsJsonAsync($"/api/ordensdeservico/{os.Id}/orcamento", new { });
        await _client.PostAsJsonAsync($"/api/ordensdeservico/{os.Id}/orcamento/enviar", new { });

        await _client.PatchAsJsonAsync($"/api/ordensdeservico/{os.Id}/orcamento/resposta",
            new ResponderOrcamentoRequest { Aprovado = true });

        var estoqueAtualizado = await _client.GetFromJsonAsync<EstoqueResponse>($"/api/estoques/{estoque.Id}");
        estoqueAtualizado!.Quantidade.Should().Be(7); // 10 - 3
    }

    [Fact]
    public async Task PATCH_ResponderOrcamento_Reprovado_DeveRetornar200ComStatusFinalizada()
    {
        var os = await CriarOsComOrcamentoEnviadoAsync();

        var response = await _client.PatchAsJsonAsync($"/api/ordensdeservico/{os!.Id}/orcamento/resposta",
            new ResponderOrcamentoRequest { Aprovado = false });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        resultado!.Status.Should().Be(StatusOrdemDeServico.Finalizada);
        resultado.Orcamento!.Status.Should().Be(StatusOrcamento.Rejeitado);
    }

    [Fact]
    public async Task PATCH_ResponderOrcamento_ComOsInexistente_DeveRetornar404()
    {
        var response = await _client.PatchAsJsonAsync($"/api/ordensdeservico/{Guid.NewGuid()}/orcamento/resposta",
            new ResponderOrcamentoRequest { Aprovado = true });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_ResponderOrcamento_SemOrcamento_DeveRetornar404()
    {
        var os = await CriarOrdemDeServicoAsync();

        var response = await _client.PatchAsJsonAsync($"/api/ordensdeservico/{os!.Id}/orcamento/resposta",
            new ResponderOrcamentoRequest { Aprovado = true });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<OrdemDeServicoResponse?> CriarOsComOrcamentoEnviadoAsync()
    {
        var os = await CriarOrdemDeServicoAsync();
        await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/orcamento", new { });
        await _client.PostAsJsonAsync($"/api/ordensdeservico/{os.Id}/orcamento/enviar", new { });
        return os;
    }

    // ── PATCH /api/ordensdeservico/{numeroOS}/servicos/{servicoItemId}/status ──

    [Fact]
    public async Task PATCH_AlterarStatusServico_ParaIniciado_DeveRetornar200ComStatusIniciado()
    {
        var (os, item) = await CriarOsComServicoAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/ordensdeservico/{os.NumeroOS}/servicos/{item.Id}/status",
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<ServicoItemResponse>();
        resultado!.Status.Should().Be(StatusExecucaoServico.Iniciado);
        resultado.IniciadaEm.Should().NotBeNull();
    }

    [Fact]
    public async Task PATCH_AlterarStatusServico_ParaFinalizado_DeveRetornar200ComStatusFinalizado()
    {
        var (os, item) = await CriarOsComServicoIniciadoAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/ordensdeservico/{os.NumeroOS}/servicos/{item.Id}/status",
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Finalizado });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<ServicoItemResponse>();
        resultado!.Status.Should().Be(StatusExecucaoServico.Finalizado);
        resultado.FinalizadaEm.Should().NotBeNull();
    }

    [Fact]
    public async Task PATCH_AlterarStatusServico_StatusInvalido_DeveRetornar400()
    {
        var (os, item) = await CriarOsComServicoAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/ordensdeservico/{os.NumeroOS}/servicos/{item.Id}/status",
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Criada });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_AlterarStatusServico_ComOsInexistente_DeveRetornar404()
    {
        var response = await _client.PatchAsJsonAsync(
            $"/api/ordensdeservico/OS-9999-99999/servicos/{Guid.NewGuid()}/status",
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_AlterarStatusServico_ComServicoItemInexistente_DeveRetornar404()
    {
        var os = await CriarOrdemDeServicoAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/ordensdeservico/{os!.NumeroOS}/servicos/{Guid.NewGuid()}/status",
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PATCH_AlterarStatusServico_IniciarJaIniciado_DeveRetornar400()
    {
        var (os, item) = await CriarOsComServicoIniciadoAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/ordensdeservico/{os.NumeroOS}/servicos/{item.Id}/status",
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PATCH_AlterarStatusServico_FinalizarSemIniciar_DeveRetornar400()
    {
        var (os, item) = await CriarOsComServicoAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/ordensdeservico/{os.NumeroOS}/servicos/{item.Id}/status",
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Finalizado });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/ordensdeservico/aging ───────────────────────────────────────

    [Fact]
    public async Task GET_Aging_SemServicosFinalizados_DeveRetornar200ComListaVazia()
    {
        var response = await _client.GetAsync("/api/ordensdeservico/aging");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<IEnumerable<AgingServicoResponse>>();
        resultado.Should().NotBeNull();
    }

    [Fact]
    public async Task GET_Aging_ComServicoFinalizado_DeveRetornar200ComDados()
    {
        var (os, item) = await CriarOsComServicoIniciadoAsync();
        await _client.PatchAsJsonAsync(
            $"/api/ordensdeservico/{os.NumeroOS}/servicos/{item.Id}/status",
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Finalizado });

        var response = await _client.GetAsync("/api/ordensdeservico/aging");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = (await response.Content.ReadFromJsonAsync<IEnumerable<AgingServicoResponse>>())!.ToList();
        resultado.Should().NotBeEmpty();
        resultado.Should().Contain(r => r.TotalExecucoes >= 1);
    }

    // ── Helpers adicionais ───────────────────────────────────────────────────

    private async Task<(OrdemDeServicoResponse os, ServicoItemResponse servicoItem)> CriarOsComServicoAsync()
    {
        var os = await CriarOrdemDeServicoAsync();
        var servico = await CadastrarServicoAsync("Servico " + Guid.NewGuid().ToString("N")[..8], 150.00m);

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/servicos",
            new AdicionarServicoRequest { ServicoId = servico!.Id });

        var osAtualizada = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        return (osAtualizada!, osAtualizada!.Servicos.First());
    }

    private async Task<(OrdemDeServicoResponse os, ServicoItemResponse servicoItem)> CriarOsComServicoIniciadoAsync()
    {
        var (os, item) = await CriarOsComServicoAsync();

        await _client.PatchAsJsonAsync(
            $"/api/ordensdeservico/{os.NumeroOS}/servicos/{item.Id}/status",
            new AlterarStatusServicoNaOSRequest { Status = StatusExecucaoServico.Iniciado });

        return (os, item);
    }
}
