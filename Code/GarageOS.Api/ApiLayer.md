# 🎯 Camada de API / Apresentação (Api)

## Propósito
É o projeto ASP.NET Core principal que é executado. Recebe requisições HTTP, repassa para a Application e retorna respostas formatadas.

## Dependências
- ✅ Depende de Application e Infrastructure
- ✅ Aqui ficam Controllers, Middlewares, configuração de DI
- ❌ Nunca contém lógica de negócio diretamente

## O que deve conter

### 📁 Controllers
Endpoints HTTP da aplicação.

```csharp
// Exemplo:
[ApiController]
[Route("api/[controller]")]
public class VeiculosController : ControllerBase
{
    private readonly CadastrarVeiculoUseCase _cadastrarVeiculoUseCase;
    
    public VeiculosController(CadastrarVeiculoUseCase cadastrarVeiculoUseCase)
    {
        _cadastrarVeiculoUseCase = cadastrarVeiculoUseCase;
    }
    
    [HttpPost]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarVeiculoRequest request)
    {
        var id = await _cadastrarVeiculoUseCase.ExecutarAsync(request);
        return CreatedAtAction(nameof(Obter), new { id }, id);
    }
}
```

### 📁 Middlewares
Filtros e middlewares para tratar requisições.

```csharp
// Exemplo:
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    
    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}
```

### 📁 Extensions
Métodos de extensão para registrar serviços na DI.

```csharp
// Exemplo:
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<CadastrarVeiculoUseCase>();
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        return services;
    }
}
```

### Program.cs
Configuração da aplicação e setup do pipeline.

```csharp
// Exemplo:
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplicationServices();
builder.Services.AddScoped<ExceptionHandlingMiddleware>();

var app = builder.Build();

app.MapControllers();
app.Run();
```

## Responsabilidade 🎯
Seu único trabalho é:
1. ✅ Receber requisições HTTP
2. ✅ Repassar execução para a Application
3. ✅ Retornar resposta com Status Code correto (JSON)

Nunca implemente lógica de negócio aqui!
