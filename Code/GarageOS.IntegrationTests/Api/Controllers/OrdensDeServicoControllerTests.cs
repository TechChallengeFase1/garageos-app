using System.Net;
using System.Net.Http.Json;
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

    // ── POST /api/ordensdeservico ────────────────────────────────────────────

    [Fact]
    public async Task POST_CriarOrdemDeServico_ComDadosValidos_DeveRetornar201()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();

        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var os = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        os!.Id.Should().NotBeEmpty();
        os.NumeroOS.Should().StartWith("OS-");
        os.ClienteId.Should().Be(cliente.Id);
        os.VeiculoId.Should().Be(veiculo.Id);
        os.Status.Should().Be(StatusOrdemDeServico.Recebida);
    }

    [Fact]
    public async Task POST_CriarOrdemDeServico_ComClienteInexistente_DeveRetornar400()
    {
        var veiculo = await CadastrarVeiculoAsync();

        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = Guid.NewGuid(),
            VeiculoId = veiculo!.Id
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CriarOrdemDeServico_ComVeiculoInexistente_DeveRetornar400()
    {
        var cliente = await CadastrarClienteAsync();

        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = Guid.NewGuid()
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CriarOrdemDeServico_DuasOsSequenciais_DeveTerNumerosSequenciais()
    {
        var cliente = await CadastrarClienteAsync();
        var veiculo = await CadastrarVeiculoAsync();

        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id
        };

        var r1 = await _client.PostAsJsonAsync("/api/ordensdeservico", request);
        var r2 = await _client.PostAsJsonAsync("/api/ordensdeservico", request);

        var os1 = await r1.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        var os2 = await r2.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();

        os1!.NumeroOS.Should().NotBe(os2!.NumeroOS);
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
        resultado!.Servicos.Should().HaveCount(1);
        resultado.Servicos.First().ServicoId.Should().Be(servico.Id);
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

        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = cliente!.Id,
            VeiculoId = veiculo!.Id
        };

        var response = await _client.PostAsJsonAsync("/api/ordensdeservico", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            $"Falha ao criar OS: cliente={cliente.Id}, veiculo={veiculo.Id}, status={response.StatusCode}");
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
        resultado.Orcamento!.Preco.Should().Be(240.00m); // 150 + (45 * 2)
        resultado.Orcamento.Status.Should().Be(StatusOrcamento.Pendente);
    }

    [Fact]
    public async Task POST_GerarOrcamento_SemServicosNemEstoques_DeveRetornar200ComPrecoZero()
    {
        var os = await CriarOrdemDeServicoAsync();

        var response = await _client.PostAsJsonAsync($"/api/ordensdeservico/{os!.Id}/orcamento", new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var resultado = await response.Content.ReadFromJsonAsync<OrdemDeServicoResponse>();
        resultado!.Orcamento.Should().NotBeNull();
        resultado.Orcamento!.Preco.Should().Be(0m);
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
}
