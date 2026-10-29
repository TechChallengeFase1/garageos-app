using FluentAssertions;
using GarageOS.Infrastructure.Data;
using GarageOS.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.IntegrationTests.Infrastructure;

/// <summary>A Lambda de login por CPF lê a tabela Clientes direto</summary>
/// <remarks>
/// Ela faz SELECT "Id", "Nome", "Email", "Ativo" FROM "Clientes" WHERE
/// "DocumentoValor" = $1, com a role somente leitura auth_reader. Se um
/// mapeamento do EF renomear qualquer uma dessas colunas, o login por CPF quebra
/// em produção sem nenhum teste da Lambda perceber. Este teste percebe.
/// </remarks>
public class ContratoComALambdaTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ContratoComALambdaTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TabelaClientes_DeveTerAsColunasQueALambdaConsulta()
    {
        using var escopo = _factory.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<GarageOSDbContext>();

        var colunas = await db.Database.SqlQueryRaw<string>(
            "SELECT column_name AS \"Value\" FROM information_schema.columns WHERE table_name = 'Clientes'")
            .ToListAsync();

        colunas.Should().Contain(["Id", "Nome", "Email", "Ativo", "DocumentoValor"]);
    }
}
