using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GarageOS.Application.DTOs.Clientes;
using GarageOS.IntegrationTests.Fixtures;
using GarageOS.IntegrationTests.Helpers;

namespace GarageOS.IntegrationTests.Api.Controllers;

public class ClientesControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public ClientesControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── GET /api/clientes ────────────────────────────────────────────────────

    [Fact]
    public async Task GET_ListarClientes_DeveRetornar200ComLista()
    {
        var response = await _client.GetAsync("/api/clientes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var lista = await response.Content.ReadFromJsonAsync<IEnumerable<ClienteResponse>>();
        lista.Should().NotBeNull();
    }

    // ── POST /api/clientes ───────────────────────────────────────────────────

    [Fact]
    public async Task POST_CadastrarCliente_ComDadosValidos_DeveRetornar201()
    {
        var request = CriarClienteRequestPadrao(Dados.Email(), Dados.Cpf());

        var response = await _client.PostAsJsonAsync("/api/clientes", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var cliente = await response.Content.ReadFromJsonAsync<ClienteResponse>();
        cliente!.Id.Should().NotBeEmpty();
        cliente.Nome.Should().Be(request.Nome);
        cliente.Email.Should().Be(request.Email);
        cliente.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task POST_CadastrarCliente_ComNomeVazio_DeveRetornar400()
    {
        var request = CriarClienteRequestPadrao(Dados.Email(), Dados.Cpf());
        request.Nome = "";

        var response = await _client.PostAsJsonAsync("/api/clientes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CadastrarCliente_ComDocumentoInvalido_DeveRetornar400()
    {
        var request = CriarClienteRequestPadrao(Dados.Email(), "123");

        var response = await _client.PostAsJsonAsync("/api/clientes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CadastrarCliente_ComEmailInvalido_DeveRetornar400()
    {
        var request = CriarClienteRequestPadrao("emailinvalido", Dados.Cpf());

        var response = await _client.PostAsJsonAsync("/api/clientes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CadastrarCliente_ComCepMenosDe8Digitos_DeveRetornar400()
    {
        var request = CriarClienteRequestPadrao(Dados.Email(), Dados.Cpf());
        request.Cep = "123";

        var response = await _client.PostAsJsonAsync("/api/clientes", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_CadastrarCliente_ComDocumentoDuplicado_DeveRetornar409()
    {
        var doc = Dados.Cpf();
        await _client.PostAsJsonAsync("/api/clientes", CriarClienteRequestPadrao(Dados.Email(), doc));

        var response = await _client.PostAsJsonAsync("/api/clientes", CriarClienteRequestPadrao(Dados.Email(), doc));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task POST_CadastrarCliente_ComEmailDuplicado_DeveRetornar409()
    {
        var email = Dados.Email();
        await _client.PostAsJsonAsync("/api/clientes", CriarClienteRequestPadrao(email, Dados.Cnpj()));

        var response = await _client.PostAsJsonAsync("/api/clientes", CriarClienteRequestPadrao(email, Dados.Cnpj()));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    // ── GET /api/clientes/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task GET_ObterCliente_ComIdExistente_DeveRetornar200()
    {
        var email = Dados.Email();
        var criado = await CadastrarClienteAsync(email, Dados.Cnpj());

        var response = await _client.GetAsync($"/api/clientes/{criado!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cliente = await response.Content.ReadFromJsonAsync<ClienteResponse>();
        cliente!.Id.Should().Be(criado.Id);
        cliente.Email.Should().Be(email);
    }

    [Fact]
    public async Task GET_ObterCliente_ComIdInexistente_DeveRetornar404()
    {
        var response = await _client.GetAsync($"/api/clientes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/clientes/{id} ───────────────────────────────────────────────

    [Fact]
    public async Task PUT_AlterarCliente_ComDadosValidos_DeveRetornar200ComDadosAtualizados()
    {
        var doc = Dados.Cnpj();
        var criado = await CadastrarClienteAsync(Dados.Email(), doc);

        var novoEmail = Dados.Email();
        var request = new AtualizarClienteRequest
        {
            Nome = "Nome Atualizado",
            Documento = doc,
            Email = novoEmail,
            Telefone = Dados.Telefone(),
            Logradouro = "Rua Nova",
            Numero = "456",
            Bairro = "Novo Bairro",
            Cidade = "Sao Paulo",
            Estado = "SP",
            Cep = "04567890"
        };

        var response = await _client.PutAsJsonAsync($"/api/clientes/{criado!.Id}", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var atualizado = await response.Content.ReadFromJsonAsync<ClienteResponse>();
        atualizado!.Nome.Should().Be("Nome Atualizado");
        atualizado.Email.Should().Be(novoEmail);
    }

    [Fact]
    public async Task PUT_AlterarCliente_ComIdInexistente_DeveRetornar404()
    {
        var request = new AtualizarClienteRequest
        {
            Nome = "Qualquer",
            Documento = Dados.Cpf(),
            Email = Dados.Email(),
            Telefone = Dados.Telefone(),
            Logradouro = "Rua",
            Numero = "1",
            Bairro = "Bairro",
            Cidade = "Cidade",
            Estado = "SP",
            Cep = "01234567"
        };

        var response = await _client.PutAsJsonAsync($"/api/clientes/{Guid.NewGuid()}", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── DELETE /api/clientes/{id} ────────────────────────────────────────────

    [Fact]
    public async Task DELETE_DeletarCliente_ComIdExistente_DeveRetornar204()
    {
        var criado = await CadastrarClienteAsync(Dados.Email(), Dados.Cpf());

        var response = await _client.DeleteAsync($"/api/clientes/{criado!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DELETE_DeletarCliente_ComIdInexistente_DeveRetornar404()
    {
        var response = await _client.DeleteAsync($"/api/clientes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<ClienteResponse?> CadastrarClienteAsync(string email, string documento)
    {
        var response = await _client.PostAsJsonAsync("/api/clientes",
            CriarClienteRequestPadrao(email, documento));
        return await response.Content.ReadFromJsonAsync<ClienteResponse>();
    }

    private static CriarClienteRequest CriarClienteRequestPadrao(string email, string documento) =>
        new()
        {
            Nome = "Cliente Teste",
            Documento = documento,
            Email = email,
            Telefone = Dados.Telefone(),
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "Sao Paulo",
            Estado = "SP",
            Cep = "01234567"
        };
}
