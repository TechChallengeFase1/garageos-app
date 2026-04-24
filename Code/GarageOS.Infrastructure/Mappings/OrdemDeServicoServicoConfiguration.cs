using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Mappings;

public class OrdemDeServicoServicoConfiguration : IEntityTypeConfiguration<OrdemDeServicoServico>
{
    public void Configure(EntityTypeBuilder<OrdemDeServicoServico> builder)
    {
        builder.ToTable("OrdensDeServicoServicos");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.CriadoEm)
            .IsRequired();

        builder.HasOne(x => x.Servico)
            .WithMany()
            .HasForeignKey(x => x.ServicoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
