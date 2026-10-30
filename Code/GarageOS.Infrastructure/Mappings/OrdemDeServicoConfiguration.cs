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

        // Único: a sequência garante, o índice confirma.
        builder.HasIndex(x => x.NumeroOS)
            .IsUnique();

        // Texto, e não inteiro: um status novo no meio do enum não muda o
        // significado do que já está gravado.
        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.CriadoEm)
            .IsRequired();

        builder.Property(x => x.AtualizadoEm)
            .IsRequired();

        builder.Ignore(x => x.ValorEstimado);

        // Token de concorrência (xmin do PostgreSQL): duas mudanças simultâneas
        // na mesma OS não se sobrescrevem em silêncio.
        builder.Property<uint>("Versao")
            .IsRowVersion();

        // Cliente e veículo são do próprio OS Service: a FK continua.
        builder.HasOne<Cliente>()
            .WithMany()
            .HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Veiculo>()
            .WithMany()
            .HasForeignKey(x => x.VeiculoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Servicos)
            .WithOne()
            .HasForeignKey(x => x.OrdemDeServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Pecas)
            .WithOne()
            .HasForeignKey(x => x.OrdemDeServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Historico)
            .WithOne()
            .HasForeignKey(x => x.OrdemDeServicoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Servicos).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Pecas).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Historico).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
