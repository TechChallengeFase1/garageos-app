using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Mappings;

public class HistoricoStatusOSConfiguration : IEntityTypeConfiguration<HistoricoStatusOS>
{
    public void Configure(EntityTypeBuilder<HistoricoStatusOS> builder)
    {
        builder.ToTable("HistoricoStatusOS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.StatusAnterior)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.StatusNovo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Origem)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.Property(x => x.OcorridoEm)
            .IsRequired();

        builder.HasIndex(x => new { x.OrdemDeServicoId, x.OcorridoEm });
    }
}
