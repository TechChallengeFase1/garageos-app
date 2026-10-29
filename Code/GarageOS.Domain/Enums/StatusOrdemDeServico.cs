namespace GarageOS.Domain.Enums;

/// <summary>Status da ordem de serviço</summary>
/// <remarks>
/// AguardandoPagamento e Cancelada entraram na Fase 4, no FIM da lista. O status
/// passou a ser gravado como texto, mas a ordem continua importando para quem
/// ainda trafega o número (clientes antigos da API).
/// </remarks>
public enum StatusOrdemDeServico
{
    Recebida,
    EmDiagnostico,
    AguardandoAprovacao,
    EmExecucao,
    Finalizada,
    Entregue,
    AguardandoPagamento,
    Cancelada
}
