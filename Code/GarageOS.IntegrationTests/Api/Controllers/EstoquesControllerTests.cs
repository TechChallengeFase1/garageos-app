using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GarageOS.Application.DTOs.Estoques;
using GarageOS.IntegrationTests.Fixtures;

namespace GarageOS.IntegrationTests.Api.Controllers;

public class EstoquesControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public EstoquesControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/estoques ────────────────────────────────────────────────────

    [Fact]
    public async Task GET_ListarEstoques_DeveRetornar200ComLista()
    {
        var response = await _client.GetAsync("/api/estoques");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await response.Content.ReadFromJsonAsync<IEnumerable<EstoqueResponse>>();
        lista.Should().NotBeNull();
    }

    // ── POST /api/estoques ───────────────────────────────────────────────────

    [Fact]
    public async Task POST_CadastrarEstoque_ComDadosValidos_DeveRetornar201()
    {
        var request = CriarEstoqueRequestPadrao("Oleo 5W30", 10, 50.00m);

        var response = await _client.PostAsJsonAsync("/api/estoques", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var estoque = await response.Content.ReadFromJsonAsync<EstoqueResponse>();
        estoque!.Id.Should().NotBeEmpty();
        estoque.Nome.Should().Be("Oleo 5W30");
        estoque.Quantidade.Should().Be(10);
        estoque.Valor.Should().Be(50.00m);
    }

    [Fact]
    public async Task POST_CadastrarEstoque_ComQuantidadeZero_DeveRetornar201ComStatusIndisponivel()
    {
        var request = CriarEstoqueRequestPadrao("Peca Indisponivel", 0, 100.00m);

        var response = await _client.PostAsJsonAsync("/api/estoques", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var estoque = await response.Content.ReadFromJsonAsync<EstoqueResponse>();
        estoque!.Quantidade.Should().Be(0);
    }

    [Fact]
    public async Task POST_CadastrarEstoque_ComNomeVazio_DeveRetornar400()
    {
        var request = CriarEstoqueRequestPadrao("", 5, 30.00m);

        var response = await _client.PostAsJsonAsync("/api/estoques", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CadastrarEstoque_ComValorZero_DeveRetornar400()
    {
        var request = CriarEstoqueRequestPadrao("Filtro Oleo", 5, 0m);

        var response = await _client.PostAsJsonAsync("/api/estoques", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CadastrarEstoque_ComFornecedorVazio_DeveRetornar400()
    {
        var request = CriarEstoqueRequestPadrao("Peca Teste", 5, 50.00m);
        request.Fornecedor = "";

        var response = await _client.PostAsJsonAsync("/api/estoques", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/estoques/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GET_ObterEstoque_ComIdExistente_DeveRetornar200()
    {
        var criado = await CadastrarEstoqueAsync("Filtro Ar", 5, 25.00m);

        var response = await _client.GetAsync($"/api/estoques/{criado!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var estoque = await response.Content.ReadFromJsonAsync<EstoqueResponse>();
        estoque!.Id.Should().Be(criado.Id);
        estoque.Nome.Should().Be("Filtro Ar");
    }

    [Fact]
    public async Task GET_ObterEstoque_ComIdInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/estoques/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/estoques/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task PUT_AlterarEstoque_ComDadosValidos_DeveRetornar200ComDadosAtualizados()
    {
        var criado = await CadastrarEstoqueAsync("Vela Ignicao", 8, 15.00m);

        var request = new AtualizarEstoqueRequest
        {
            Nome = "Vela de Ignicao NGK",
            Quantidade = 20,
            Valor = 18.50m,
            DataEntrada = DateTime.UtcNow,
            Fornecedor = "NGK"
        };

        var response = await _client.PutAsJsonAsync($"/api/estoques/{criado!.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizado = await response.Content.ReadFromJsonAsync<EstoqueResponse>();
        atualizado!.Nome.Should().Be("Vela de Ignicao NGK");
        atualizado.Quantidade.Should().Be(20);
        atualizado.Valor.Should().Be(18.50m);
    }

    [Fact]
    public async Task PUT_AlterarEstoque_ComIdInexistente_DeveRetornar404()
    {
        var request = new AtualizarEstoqueRequest
        {
            Nome = "Qualquer",
            Quantidade = 1,
            Valor = 10.00m,
            DataEntrada = DateTime.UtcNow,
            Fornecedor = "Fornecedor"
        };

        var response = await _client.PutAsJsonAsync($"/api/estoques/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/estoques/{id} ────────────────────────────────────────────

    [Fact]
    public async Task DELETE_DeletarEstoque_ComIdExistente_DeveRetornar204()
    {
        var criado = await CadastrarEstoqueAsync("Peca Para Deletar", 2, 40.00m);

        var response = await _client.DeleteAsync($"/api/estoques/{criado!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_DeletarEstoque_ComIdInexistente_DeveRetornar404()
    {
        var response = await _client.DeleteAsync($"/api/estoques/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DELETE_DeletarEstoque_AposDelecao_ObterDeveRetornar404()
    {
        var criado = await CadastrarEstoqueAsync("Peca Temporaria", 1, 10.00m);

        await _client.DeleteAsync($"/api/estoques/{criado!.Id}");
        var response = await _client.GetAsync($"/api/estoques/{criado.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<EstoqueResponse?> CadastrarEstoqueAsync(string nome, int quantidade, decimal valor)
    {
        var request = CriarEstoqueRequestPadrao(nome, quantidade, valor);
        var response = await _client.PostAsJsonAsync("/api/estoques", request);
        return await response.Content.ReadFromJsonAsync<EstoqueResponse>();
    }

    private static CriarEstoqueRequest CriarEstoqueRequestPadrao(string nome, int quantidade, decimal valor) =>
        new()
        {
            Nome = nome,
            Quantidade = quantidade,
            Valor = valor,
            DataEntrada = DateTime.UtcNow,
            Fornecedor = "Fornecedor Teste"
        };
}
