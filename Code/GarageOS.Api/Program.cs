using System.Text.Json.Serialization;
using GarageOS.Api.Extensions;
using GarageOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerWithJwt();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

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

// Aplica migrations pendentes na inicializacao (necessario para deploy em K8s,
// onde o banco sobe vazio). Idempotente: nao faz nada se ja estiver atualizado.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GarageOSDbContext>();
    await db.Database.MigrateAsync();
}

await app.RunAsync();

// Necessário para WebApplicationFactory nos testes de integração
/// <summary>Entry point da aplicação GarageOS</summary>
public partial class Program
{
    /// <summary>Construtor protegido para uso pelo WebApplicationFactory nos testes</summary>
    protected Program() { }
}
