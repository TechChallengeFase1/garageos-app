namespace GarageOS.Infrastructure.Saga;

/// <summary>Prazos da saga (seção "Saga")</summary>
/// <remarks>
/// Curtos de propósito: o agendamento usa o atraso nativo do SQS, que vai no
/// máximo a 15 minutos. Prazos reais (24 a 48 horas para o cliente aprovar)
/// pediriam um agendador persistido, como o Quartz no próprio PostgreSQL.
/// </remarks>
public class SagaOptions
{
    public const string Secao = "Saga";

    public double PrazoAprovacaoMinutos { get; set; } = 10;
    public double PrazoPagamentoMinutos { get; set; } = 10;
}
