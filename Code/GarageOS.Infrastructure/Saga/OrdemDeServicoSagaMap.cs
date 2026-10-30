using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GarageOS.Infrastructure.Saga;

public class OrdemDeServicoSagaMap : SagaClassMap<OrdemDeServicoSagaState>
{
    protected override void Configure(EntityTypeBuilder<OrdemDeServicoSagaState> entity, ModelBuilder model)
    {
        entity.ToTable("SagasOrdemDeServico");

        entity.Property(x => x.CurrentState).HasMaxLength(40).IsRequired();
        entity.Property(x => x.NumeroOS).HasMaxLength(20).IsRequired();
        entity.Property(x => x.Total).HasColumnType("numeric(18,2)");
        entity.Property(x => x.PecasJson).HasColumnType("jsonb").IsRequired();
        entity.Property(x => x.ServicosJson).HasColumnType("jsonb").IsRequired();
        entity.Property(x => x.Motivo).HasMaxLength(500);

        entity.Property(x => x.Versao).IsRowVersion();

        entity.HasIndex(x => x.CurrentState);
    }
}
