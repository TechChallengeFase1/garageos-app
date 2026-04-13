# 🧪 GarageOS.IntegrationTests

## Propósito
Testes de integração da **Infrastructure** e **Api** layers - pipeline HTTP real, banco de dados real, testes de ponta a ponta.

## 📦 Pacotes Instalados
- **xUnit** - Framework de testes
- **Microsoft.AspNetCore.Mvc.Testing** - Spinning up da API em memória
- **FluentAssertions** - Assertions mais legíveis

## 📁 Estrutura

```
GarageOS.IntegrationTests/
├── Api/
│   └── Controllers/       ← Testes HTTP dos endpoints (GET/POST/PUT/DELETE)
├── Infrastructure/
│   └── Repositories/      ← Testes dos repositórios contra BD real
└── Fixtures/
    └── ApiFactory.cs      ← WebApplicationFactory customizada
```

## 🏭 ApiFactory

A classe `ApiFactory` herda de `WebApplicationFactory<Program>` e permite:
- Spinning up de um servidor de teste em memória
- Configuração de serviços customizados para testes (mocks, alternativas)
- Criação de clientes HTTP para fazer requisições

### Customizando ApiFactory

```csharp
// Exemplo: Substituir um repositório por um fake
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureServices(services =>
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IVeiculoRepository));
        if (descriptor != null)
            services.Remove(descriptor);
        
        services.AddScoped<IVeiculoRepository>(_ => new FakeVeiculoRepository());
    });

    base.ConfigureWebHost(builder);
}
```

## ✍️ Exemplo: Teste de Controller (GET)

```csharp
// GarageOS.IntegrationTests/Api/Controllers/VeiculosControllerTests.cs
namespace GarageOS.IntegrationTests.Api.Controllers;

public class VeiculosControllerTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    
    public VeiculosControllerTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }
    
    [Fact]
    public async Task Get_ObterTodosVeiculos_DeveRetornar200()
    {
        // Act
        var response = await _client.GetAsync("/api/veiculos");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }
    
    [Fact]
    public async Task Get_ObterVeiculoPorId_DeveRetornar200()
    {
        // Arrange
        var veiculoId = Guid.NewGuid();
        
        // Act
        var response = await _client.GetAsync($"/api/veiculos/{veiculoId}");
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

## ✍️ Exemplo: Teste de Controller (POST)

```csharp
[Fact]
public async Task Post_CadastrarVeiculo_ComDadosValidos_DeveRetornar201()
{
    // Arrange
    var request = new CadastrarVeiculoRequest 
    { 
        Placa = "ABC1234", 
        Marca = "Toyota" 
    };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/veiculos", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    response.Headers.Location.Should().NotBeNull();
    
    var content = await response.Content.ReadAsAsync<VeiculoResponse>();
    content.Placa.Should().Be(request.Placa);
}

[Fact]
public async Task Post_CadastrarVeiculo_ComDadosInvalidos_DeveRetornar400()
{
    // Arrange
    var request = new CadastrarVeiculoRequest 
    { 
        Placa = "", // Inválido
        Marca = "Toyota" 
    };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/veiculos", request);
    
    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
```

## ✍️ Exemplo: Teste de Repositório

```csharp
// GarageOS.IntegrationTests/Infrastructure/Repositories/VeiculoRepositoryTests.cs
namespace GarageOS.IntegrationTests.Infrastructure.Repositories;

public class VeiculoRepositoryTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    
    public VeiculoRepositoryTests(ApiFactory factory)
    {
        _factory = factory;
    }
    
    [Fact]
    public async Task AdicionarAsync_ComVeiculoValido_DevePersistirNoBancoDados()
    {
        // Arrange
        var veiculo = new Veiculo("XYZ9999", "Honda");
        var repository = _factory.Services.GetRequiredService<IVeiculoRepository>();
        
        // Act
        await repository.AdicionarAsync(veiculo);
        
        // Assert
        var recuperado = await repository.ObterPorIdAsync(veiculo.Id);
        recuperado.Should().NotBeNull();
        recuperado.Placa.Should().Be("XYZ9999");
    }
    
    [Fact]
    public async Task ObterPorIdAsync_ComIdInexistente_DeveRetornarNull()
    {
        // Arrange
        var repository = _factory.Services.GetRequiredService<IVeiculoRepository>();
        
        // Act
        var resultado = await repository.ObterPorIdAsync(Guid.NewGuid());
        
        // Assert
        resultado.Should().BeNull();
    }
}
```

## 🏃 Executando os Testes

```bash
# Rodar todos os testes de integração
dotnet test GarageOS.IntegrationTests

# Rodar testes de uma classe específica
dotnet test GarageOS.IntegrationTests --filter "ClassName=VeiculosControllerTests"

# Com output detalhado
dotnet test GarageOS.IntegrationTests --logger "console;verbosity=detailed"

# Modo watch
dotnet watch test GarageOS.IntegrationTests
```

## 🐳 Usando Testcontainers (Avançado)

Para testes contra um banco SQL Server real em Docker:

```bash
dotnet add GarageOS.IntegrationTests package Testcontainers.MsSql
```

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();
    
    public string ConnectionString => _container.GetConnectionString();
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }
    
    public async Task DisposeAsync()
    {
        await _container.StopAsync();
    }
}
```

## ✅ Boas Práticas

1. **Isolamento**: Use fixtures para setup/cleanup
2. **Nomeação**: `Metodo_Cenario_ResultadoEsperado`
3. **HTTP Status Codes**: Sempre validar status correto
4. **Serialização**: Use `PostAsJsonAsync`, `ReadAsAsync`
5. **Assertions**: Validar headers, body, status code
6. **Configuração**: Use ApiFactory para customizar comportamento
7. **Limpeza**: IAsyncLifetime para setup/cleanup automático
8. **Lentidão**: Testes de integração são mais lentos - aceite isso

## 📊 Diferenças: Unit vs Integration

| Aspecto | Unit Tests | Integration Tests |
|---------|-----------|------------------|
| Velocidade | Rápido (< 1s) | Lento (2-10s+) |
| Escopo | Função/Classe | Endpoint/Repositório |
| I/O | Nenhum (mocks) | Real (BD, HTTP) |
| Foco | Regras de negócio | Fluxo completo |
| Ambiente | Qualquer | Requer infra |

## 🎯 Cobertura

- Use unit tests para regras de negócio (alta cobertura)
- Use integration tests para endpoints críticos
- Unit tests: ~70-90% de cobertura
- Integration tests: ~20-40% dos fluxos principais
