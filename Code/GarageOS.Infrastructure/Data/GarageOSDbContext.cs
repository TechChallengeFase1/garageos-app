using System.Reflection;
using GarageOS.Domain.Entities;
using GarageOS.Infrastructure.Saga;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace GarageOS.Infrastructure.Data;

/// <summary>Banco próprio do OS Service</summary>
/// <remarks>
/// Fase 4: Estoques e Orcamentos saíram (são do Execução e do Billing). As
/// peças da OS ficam como cópia, sem chave estrangeira para outro serviço.
/// </remarks>
public class GarageOSDbContext : DbContext
{
    public GarageOSDbContext(DbContextOptions<GarageOSDbContext> options) : base(options) { }

    public DbSet<Servico> Servicos { get; set; }
    public DbSet<Veiculo> Veiculos { get; set; }
    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<OrdemDeServico> OrdensDeServico { get; set; }
    public DbSet<OrdemDeServicoServico> OrdensDeServicoServicos { get; set; }
    public DbSet<OrdemDeServicoPeca> OrdensDeServicoPecas { get; set; }
    public DbSet<HistoricoStatusOS> HistoricoStatusOS { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Saga e outbox/inbox do MassTransit no mesmo banco: a transição da
        // saga, o status da OS e as mensagens a publicar vão no mesmo commit.
        new OrdemDeServicoSagaMap().Configure(modelBuilder);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        base.OnModelCreating(modelBuilder);
    }
}
