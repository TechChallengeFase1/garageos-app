using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Mappings;

public class OrdemDeServicoEstoqueConfiguration : IEntityTypeConfiguration<OrdemDeServicoEstoque>
{
    public void Configure(EntityTypeBuilder<OrdemDeServicoEstoque> builder)
    {
        builder.ToTable("OrdensDeServicoEstoques");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Quantidade)
            .IsRequired();

        builder.HasOne(x => x.Estoque)
            .WithMany()
            .HasForeignKey(x => x.EstoqueId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
