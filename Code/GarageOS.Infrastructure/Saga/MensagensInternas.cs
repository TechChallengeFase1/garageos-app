using MassTransit;

namespace GarageOS.Infrastructure.Saga;

// Mensagens que a saga agenda para si mesma. Não saem do OS Service.

[MessageUrn("garageos:os:prazo-aprovacao-expirado:v1")]
[EntityName("os-prazo-aprovacao-expirado")]
public record PrazoAprovacaoExpirado(Guid OrdemDeServicoId);

[MessageUrn("garageos:os:prazo-pagamento-expirado:v1")]
[EntityName("os-prazo-pagamento-expirado")]
public record PrazoPagamentoExpirado(Guid OrdemDeServicoId);
