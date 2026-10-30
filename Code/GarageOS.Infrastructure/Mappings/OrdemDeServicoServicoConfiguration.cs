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

        builder.Property(x => x.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.PrecoUnitario)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.CriadoEm)
            .IsRequired();

        // O catálogo de serviços é do próprio OS Service: a FK continua.
        builder.HasOne<Servico>()
            .WithMany()
            .HasForeignKey(x => x.ServicoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
