namespace GarageOS.Application.Abstractions;

/// <summary>Publicação de eventos de integração para os outros serviços</summary>
/// <remarks>
/// Os use cases conhecem só esta interface; o MassTransit e o SQS ficam na
/// Infrastructure, no mesmo padrão do IMetricasDeNegocio.
///
/// No OS a publicação passa pelo outbox do MassTransit: a mensagem é gravada na mesma transação da OS e só sai depois do commit. Se o banco recusar, nada é publicado; se o broker estiver fora, a mensagem espera no outbox.
/// </remarks>
public interface IPublicadorDeEventos
{
    Task PublicarAsync<T>(T mensagem, CancellationToken cancellationToken = default) where T : class;
}
