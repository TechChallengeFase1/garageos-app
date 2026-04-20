using GarageOS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace GarageOS.Infrastructure.Mappings
{
    public class ClienteMapping : IEntityTypeConfiguration<Cliente>
    {
        public void Configure(EntityTypeBuilder<Cliente> builder)
        {
            builder.ToTable("Clientes");
            builder.HasKey(c => c.Id);

            builder.Property(c => c.Nome).IsRequired().HasMaxLength(150);
            builder.Property(c => c.Email).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Telefone).HasMaxLength(20);

            // Owned Type — salva na mesma tabela, sem FK separada
            builder.OwnsOne(c => c.Documento, documento =>
            {
                documento.Property(d => d.Valor).HasColumnName("DocumentoValor")
                        .IsRequired().HasMaxLength(20);
                documento.Property(d => d.Tipo).HasColumnName("DocumentoTipo")
                        .IsRequired().HasMaxLength(20);
            });

            // Owned Type — salva na mesma tabela, sem FK separada
            builder.OwnsOne(c => c.Endereco, endereco =>
            {
                endereco.Property(e => e.Logradouro).HasColumnName("Logradouro")
                        .IsRequired().HasMaxLength(200);
                endereco.Property(e => e.Numero).HasColumnName("Numero")
                        .IsRequired().HasMaxLength(10);
                endereco.Property(e => e.Complemento).HasColumnName("Complemento")
                        .HasMaxLength(100);
                endereco.Property(e => e.Bairro).HasColumnName("Bairro")
                        .IsRequired().HasMaxLength(100);
                endereco.Property(e => e.Cidade).HasColumnName("Cidade")
                        .IsRequired().HasMaxLength(100);
                endereco.Property(e => e.Estado).HasColumnName("Estado")
                        .IsRequired().HasMaxLength(2);
                endereco.Property(e => e.Cep).HasColumnName("Cep")
                        .IsRequired().HasMaxLength(8);
            });
        }
    }
}
