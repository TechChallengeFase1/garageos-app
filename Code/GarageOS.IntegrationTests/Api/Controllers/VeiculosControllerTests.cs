using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GarageOS.Application.DTOs.Veiculos;
using GarageOS.IntegrationTests.Fixtures;

namespace GarageOS.IntegrationTests.Api.Controllers;

public class VeiculosControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public VeiculosControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/veiculos ─────────────────────────────────────

    [Fact]
    public async Task GET_ListarVeiculos_DeveRetornar200ComLista()
    {
        var response = await _client.GetAsync("/api/veiculos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await response.Content.ReadFromJsonAsync<IEnumerable<VeiculoResponse>>();
        lista.Should().NotBeNull();
    }

    // ── POST /api/veiculos ────────────────────────────────────

    [Fact]
    public async Task POST_CadastrarVeiculo_ComDadosValidos_DeveRetornar201()
    {
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Honda",
            ModeloVeiculo = "Civic",
            PlacaVeiculo = "ABC1234",
            AnoVeiculo = 2020,
            PrecoVeiculo = 50000
        };

        var response = await _client.PostAsJsonAsync("/api/veiculos", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var veiculo = await response.Content.ReadFromJsonAsync<VeiculoResponse>();
        veiculo!.Id.Should().NotBeEmpty();
        veiculo.MarcaVeiculo.Should().Be(request.MarcaVeiculo);
    }

    [Fact]
    public async Task POST_CadastrarVeiculo_ComPlacaInvalida_DeveRetornar400()
    {
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Honda",
            ModeloVeiculo = "Civic",
            PlacaVeiculo = "", // inválido
            AnoVeiculo = 2020,
            PrecoVeiculo = 50000
        };

        var response = await _client.PostAsJsonAsync("/api/veiculos", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/veiculos/{id} ───────────────────────────────

    [Fact]
    public async Task GET_ObterVeiculo_ComIdExistente_DeveRetornar200()
    {
        var criado = await CadastrarVeiculoAsync();

        var response = await _client.GetAsync($"/api/veiculos/{criado!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var veiculo = await response.Content.ReadFromJsonAsync<VeiculoResponse>();
        veiculo!.Id.Should().Be(criado.Id);
    }

    [Fact]
    public async Task GET_ObterVeiculo_ComIdInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/veiculos/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PATCH /api/veiculos/{id} ─────────────────────────────

    [Fact]
    public async Task PATCH_AlterarVeiculo_ComDadosValidos_DeveRetornar200()
    {
        var criado = await CadastrarVeiculoAsync();

        var request = new AtualizarVeiculoRequest
        {
            MarcaVeiculo = "Toyota",
            ModeloVeiculo = "Corolla",
            AnoVeiculo = 2022,
            PrecoVeiculo = 70000
        };

        var response = await _client.PatchAsJsonAsync($"/api/veiculos/{criado!.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var atualizado = await response.Content.ReadFromJsonAsync<VeiculoResponse>();
        atualizado!.MarcaVeiculo.Should().Be("Toyota");
    }

    [Fact]
    public async Task PATCH_AlterarVeiculo_ComIdInexistente_DeveRetornar404()
    {
        var request = new AtualizarVeiculoRequest
        {
            MarcaVeiculo = "Ford",
            ModeloVeiculo = "Ka",
            AnoVeiculo = 2018,
            PrecoVeiculo = 30000
        };

        var response = await _client.PatchAsJsonAsync($"/api/veiculos/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helper ───────────────────────────────────────────────

    private async Task<VeiculoResponse?> CadastrarVeiculoAsync()
    {
        var request = new CriarVeiculoRequest
        {
            MarcaVeiculo = "Honda",
            ModeloVeiculo = "Fit",
            PlacaVeiculo = "DEF1234",
            AnoVeiculo = 2019,
            PrecoVeiculo = 40000
        };

        var response = await _client.PostAsJsonAsync("/api/veiculos", request);
        return await response.Content.ReadFromJsonAsync<VeiculoResponse>();
    }
}