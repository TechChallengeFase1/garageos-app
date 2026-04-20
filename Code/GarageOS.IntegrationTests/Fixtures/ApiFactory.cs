using GarageOS.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace GarageOS.IntegrationTests.Fixtures;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // 🔥 REMOVE DB REAL
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

            // 🔥 IN MEMORY DB
            var dbName = "GarageOS_Tests_" + Guid.NewGuid();
            services.AddDbContext<GarageOSDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // 🔐 MOCK AUTH
            services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Test";
    options.DefaultChallengeScheme = "Test";
})
.AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
    "Test", options => { });
        });
    }
}