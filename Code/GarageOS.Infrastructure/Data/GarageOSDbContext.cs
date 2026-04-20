using System.Reflection;
using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data;

public class GarageOSDbContext : DbContext
{
    public GarageOSDbContext(DbContextOptions<GarageOSDbContext> options) : base(options) { }

    public DbSet<Servico> Servicos { get; set; }
    public DbSet<Cliente> Clientes { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
