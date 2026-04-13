using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory customizada para testes de integração da API GarageOS.
/// Fornece um servidor de teste em memória com todas as dependências configuradas.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Configura os serviços da aplicação para o ambiente de teste.
    /// Você pode sobrescrever implementações de serviços aqui para usar mocks ou alternativas.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Exemplo: Substituir implementações por mocks ou alternativas de teste
            // var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IVeiculoRepository));
            // if (descriptor != null)
            //     services.Remove(descriptor);
            // services.AddScoped<IVeiculoRepository>(_ => new FakeVeiculoRepository());
        });

        base.ConfigureWebHost(builder);
    }

    /// <summary>
    /// Cria um cliente HTTP com a base URL configurada para o servidor de teste.
    /// </summary>
    public new HttpClient CreateClient()
    {
        return base.CreateClient();
    }
}
