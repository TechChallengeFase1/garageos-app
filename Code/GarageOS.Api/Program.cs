using GarageOS.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "GarageOS API", Version = "v1" });
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();

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

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

// Necessário para WebApplicationFactory nos testes de integração
public partial class Program { }
