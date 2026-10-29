namespace GarageOS.Domain.Exceptions;

/// <summary>Outro microsserviço não respondeu a tempo</summary>
/// <remarks>Vira 503: o pedido pode ser repetido depois.</remarks>
public class ServicoExternoIndisponivelException : Exception
{
    public ServicoExternoIndisponivelException(string servico, Exception? interna = null)
        : base($"O {servico} não está respondendo. Tente novamente em instantes.", interna) { }
}
