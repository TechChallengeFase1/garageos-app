using GarageOS.Api.Middlewares;
using GarageOS.Application.UseCases.Clientes;
using GarageOS.Application.UseCases.Estoques;
using GarageOS.Application.UseCases.Servicos;
using GarageOS.Application.UseCases.Veiculos;
using GarageOS.Application.Validators.Veiculos;
using GarageOS.Domain.Repositories;
using GarageOS.Infrastructure.Data;
using GarageOS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

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
        services.AddScoped<IVeiculoRepository, VeiculoRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IEstoqueRepository, EstoqueRepository>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services)
    {
        services.AddScoped<ListarServicosUseCase>();
        services.AddScoped<CadastrarServicoUseCase>();
        services.AddScoped<ObterServicoUseCase>();
        services.AddScoped<AlterarServicoUseCase>();

        services.AddScoped<ListarVeiculosUseCase>();
        services.AddScoped<ObterVeiculoUseCase>();
        services.AddScoped<CadastrarVeiculoUseCase>();
        services.AddScoped<AlterarVeiculoUseCase>();
        services.AddScoped<CriarVeiculoValidator>();
        
        services.AddScoped<ListarClientesUseCase>();
        services.AddScoped<CadastrarClienteUseCase>();
        services.AddScoped<ObterClienteUseCase>();
        services.AddScoped<AlterarClienteUseCase>();
        services.AddScoped<DeletarClienteUseCase>();

        services.AddScoped<ListarEstoquesUseCase>();
        services.AddScoped<CadastrarEstoqueUseCase>();
        services.AddScoped<ObterEstoqueUseCase>();
        services.AddScoped<AlterarEstoqueUseCase>();

        return services;
    }

    public static IApplicationBuilder UseGarageOSMiddlewares(
        this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionMiddleware>();

        return app;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var secretKey = configuration["Jwt:SecretKey"]!;
        var issuer = configuration["Jwt:Issuer"]!;
        var audience = configuration["Jwt:Audience"]!;

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                                                  Encoding.UTF8.GetBytes(secretKey))
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddSwaggerWithJwt(
        this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "GarageOS API", Version = "v1" });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Informe o token JWT: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });
        });

        return services;
    }
}
