using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GarageOS.Application.DTOs.Servicos;
using GarageOS.IntegrationTests.Fixtures;

namespace GarageOS.IntegrationTests.Api.Controllers;

public class ServicosControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ServicosControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/servicos ────────────────────────────────────────────────────

    [Fact]
    public async Task GET_ListarServicos_DeveRetornar200ComLista()
    {
        var response = await _client.GetAsync("/api/servicos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await response.Content.ReadFromJsonAsync<IEnumerable<ServicoResponse>>();
        lista.Should().NotBeNull();
    }

    // ── POST /api/servicos ───────────────────────────────────────────────────

    [Fact]
    public async Task POST_CadastrarServico_ComDadosValidos_DeveRetornar201()
    {
        var request = new CriarServicoRequest { NomeServico = "Troca de óleo", Preco = 150m };

        var response = await _client.PostAsJsonAsync("/api/servicos", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var servico = await response.Content.ReadFromJsonAsync<ServicoResponse>();
        servico!.Id.Should().NotBeEmpty();
        servico.NomeServico.Should().Be(request.NomeServico);
        servico.Preco.Should().Be(request.Preco);
    }

    [Fact]
    public async Task POST_CadastrarServico_ComNomeVazio_DeveRetornar400()
    {
        var request = new CriarServicoRequest { NomeServico = "", Preco = 150m };

        var response = await _client.PostAsJsonAsync("/api/servicos", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CadastrarServico_ComPrecoZero_DeveRetornar400()
    {
        var request = new CriarServicoRequest { NomeServico = "Revisão", Preco = 0 };

        var response = await _client.PostAsJsonAsync("/api/servicos", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/servicos/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GET_ObterServico_ComIdExistente_DeveRetornar200()
    {
        // Arrange: cadastra um serviço primeiro
        var criado = await CadastrarServicoAsync("Balanceamento", 80m);

        // Act
        var response = await _client.GetAsync($"/api/servicos/{criado!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var servico = await response.Content.ReadFromJsonAsync<ServicoResponse>();
        servico!.Id.Should().Be(criado.Id);
        servico.NomeServico.Should().Be("Balanceamento");
    }

    [Fact]
    public async Task GET_ObterServico_ComIdInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/servicos/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/servicos/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task PUT_AlterarServico_ComDadosValidos_DeveRetornar200ComDadosAtualizados()
    {
        // Arrange
        var criado = await CadastrarServicoAsync("Alinhamento", 120m);
        var request = new AtualizarServicoRequest { NomeServico = "Alinhamento 3D", Preco = 180m };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/servicos/{criado!.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizado = await response.Content.ReadFromJsonAsync<ServicoResponse>();
        atualizado!.NomeServico.Should().Be("Alinhamento 3D");
        atualizado.Preco.Should().Be(180m);
    }

    [Fact]
    public async Task PUT_AlterarServico_ComIdInexistente_DeveRetornar404()
    {
        var request = new AtualizarServicoRequest { NomeServico = "Qualquer", Preco = 100m };

        var response = await _client.PutAsJsonAsync($"/api/servicos/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ServicoResponse?> CadastrarServicoAsync(string nome, decimal preco)
    {
        var request = new CriarServicoRequest { NomeServico = nome, Preco = preco };
        var response = await _client.PostAsJsonAsync("/api/servicos", request);
        return await response.Content.ReadFromJsonAsync<ServicoResponse>();
    }
}
