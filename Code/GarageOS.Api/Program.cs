using GarageOS.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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

await app.RunAsync();

// Necessário para WebApplicationFactory nos testes de integração
public partial class Program
{
    /// <summary>Construtor protegido para uso pelo WebApplicationFactory nos testes</summary>
    protected Program() { }
}
