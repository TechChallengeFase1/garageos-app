namespace GarageOS.Domain.Exceptions;

public class ServicoNaOSNaoEncontradoException : Exception
{
    public ServicoNaOSNaoEncontradoException(Guid servicoItemId)
        : base($"Serviço com ID '{servicoItemId}' não encontrado nesta Ordem de Serviço.") { }
}
