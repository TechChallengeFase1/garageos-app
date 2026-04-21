using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Mappings;

public class EstoqueConfiguration : IEntityTypeConfiguration<Estoque>
{
    public void Configure(EntityTypeBuilder<Estoque> builder)
    {
        builder.ToTable("Estoques");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Quantidade)
            .IsRequired();

        builder.Property(e => e.Valor)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(e => e.DataEntrada)
            .IsRequired();

        builder.Property(e => e.DataSaida);

        builder.Property(e => e.Fornecedor)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasMaxLength(20);
    }
}
