using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Mappings;

public class ServicoConfiguration : IEntityTypeConfiguration<Servico>
{
    public void Configure(EntityTypeBuilder<Servico> builder)
    {
        builder.ToTable("Servicos");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.NomeServico)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Preco)
            .IsRequired()
            .HasColumnType("numeric(18,2)");
    }
}
