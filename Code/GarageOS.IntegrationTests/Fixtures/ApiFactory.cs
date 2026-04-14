using GarageOS.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GarageOS.IntegrationTests.Fixtures;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove TODOS os descritores relacionados ao GarageOSDbContext e seus providers
            // (DbContextOptions, IDbContextOptionsConfiguration, etc.)
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<GarageOSDbContext>) ||
                    d.ServiceType == typeof(GarageOSDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericTypeDefinition().FullName != null &&
                     d.ServiceType.GetGenericTypeDefinition().FullName!
                         .Contains("IDbContextOptionsConfiguration")))
                .ToList();

            foreach (var descriptor in toRemove)
                services.Remove(descriptor);

            // Nome fixo por factory — todos os requests do mesmo teste compartilham o mesmo DB
            var dbName = "GarageOS_Tests_" + Guid.NewGuid();
            services.AddDbContext<GarageOSDbContext>(options =>
                options.UseInMemoryDatabase(dbName));
        });

        base.ConfigureWebHost(builder);
    }
}
