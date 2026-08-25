using System.Text.Json.Serialization;
using GarageOS.Api.Extensions;
using GarageOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// ─── Logs estruturados em JSON ───────────────────────────────────────────────
// Substitui o logger padrão do ASP.NET Core, que emite texto plano e não é
// pesquisável por campo. O CompactJsonFormatter grava uma linha JSON por evento
// no stdout, que é de onde o Kubernetes e o agente do New Relic coletam.
//
// Enrich.FromLogContext() não é opcional: sem ele o CorrelationIdMiddleware
// empurra a propriedade e ela simplesmente não aparece em lugar nenhum.
builder.Services.AddSerilog((servicos, configuracao) => configuracao
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .ReadFrom.Services(servicos)
    .WriteTo.Console(new CompactJsonFormatter()));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerWithJwt();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddGarageOSHealthChecks(builder.Configuration);

var app = builder.Build();

// ─── Modo migration ──────────────────────────────────────────────────────────
//
// Acionado pelo Job do Kubernetes (k8s/jobs/migrations-job.yaml) com --migrate.
// Aplica as migrations pendentes e encerra com codigo 0, sem abrir porta HTTP.
//
// POR QUE UM JOB, E NAO NO STARTUP:
// com o HPA, o Kubernetes sobe varias replicas simultaneamente e todas
// tentariam migrar ao mesmo tempo, disputando a mesma tabela de historico.
// O Job roda UMA vez, e o rollout do Deployment so comeca depois que ele
// termina com sucesso.
//
// Usa a MESMA imagem da aplicacao: nao e preciso SDK do .NET nem `dotnet ef`
// no runtime, e garante que a migration executada e a do commit publicado.
if (args.Contains("--migrate"))
{
    using var escopoMigration = app.Services.CreateScope();
    var loggerMigration = escopoMigration.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var contextoMigration = escopoMigration.ServiceProvider.GetRequiredService<GarageOSDbContext>();

    var pendentes = (await contextoMigration.Database.GetPendingMigrationsAsync()).ToList();

    if (pendentes.Count == 0)
    {
        loggerMigration.LogInformation("Nenhuma migration pendente. Banco ja esta atualizado.");
        return;
    }

    loggerMigration.LogInformation(
        "Aplicando {Quantidade} migration(s): {Migrations}",
        pendentes.Count,
        string.Join(", ", pendentes));

    await contextoMigration.Database.MigrateAsync();

    loggerMigration.LogInformation("Migrations aplicadas com sucesso.");
    return;
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "GarageOS API v1");
        options.RoutePrefix = "swagger";
    });
}

if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseGarageOSMiddlewares();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGarageOSHealthChecks();

// Migration no startup: DESLIGADA por padrao.
//
// Em producao quem migra e o Job. Ligar isto de volta traria exatamente a
// corrida entre replicas que o Job existe para evitar.
//
// Fica ligada no docker compose (Database__MigrateOnStartup=true), onde ha uma
// instancia so e e conveniente subir o ambiente inteiro com um comando.
if (builder.Configuration.GetValue("Database:MigrateOnStartup", false))
{
    using var escopoStartup = app.Services.CreateScope();
    var contextoStartup = escopoStartup.ServiceProvider.GetRequiredService<GarageOSDbContext>();
    await contextoStartup.Database.MigrateAsync();
}

await app.RunAsync();

// Necessário para WebApplicationFactory nos testes de integração
/// <summary>Entry point da aplicação GarageOS</summary>
public partial class Program
{
    /// <summary>Construtor protegido para uso pelo WebApplicationFactory nos testes</summary>
    protected Program() { }
}
