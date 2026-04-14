using GarageOS.Application.UseCases.Servicos;
using GarageOS.Domain.Repositories;
using GarageOS.Infrastructure.Data;
using GarageOS.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<GarageOSDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IServicoRepository, ServicoRepository>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<ListarServicosUseCase>();
        services.AddScoped<CadastrarServicoUseCase>();
        services.AddScoped<ObterServicoUseCase>();
        services.AddScoped<AlterarServicoUseCase>();

        return services;
    }
}
