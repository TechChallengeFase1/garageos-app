using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Mappings;

public class OrdemDeServicoPecaConfiguration : IEntityTypeConfiguration<OrdemDeServicoPeca>
{
    public void Configure(EntityTypeBuilder<OrdemDeServicoPeca> builder)
    {
        builder.ToTable("OrdensDeServicoPecas");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        // Só o ID: a peça vive no banco do Execução Service. Sem FK.
        builder.Property(x => x.EstoqueId)
            .IsRequired();

        builder.Property(x => x.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.ValorUnitario)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.Quantidade)
            .IsRequired();

        builder.Property(x => x.CriadoEm)
            .IsRequired();

        builder.Ignore(x => x.Subtotal);
    }
}
