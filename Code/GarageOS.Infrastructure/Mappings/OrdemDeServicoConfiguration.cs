using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Mappings;

public class OrdemDeServicoConfiguration : IEntityTypeConfiguration<OrdemDeServico>
{
    public void Configure(EntityTypeBuilder<OrdemDeServico> builder)
    {
        builder.ToTable("OrdensDeServico");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.NumeroOS)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CriadoEm)
            .IsRequired();

        builder.Property(x => x.AtualizadoEm)
            .IsRequired();

        builder.HasOne(x => x.Cliente)
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Veiculo)
            .WithMany()
            .HasForeignKey(x => x.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Orcamento)
            .WithOne()
            .HasForeignKey<Orcamento>(x => x.OrdemDeServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Servicos)
            .WithOne()
            .HasForeignKey(x => x.OrdemDeServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Estoques)
            .WithOne()
            .HasForeignKey(x => x.OrdemDeServicoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
