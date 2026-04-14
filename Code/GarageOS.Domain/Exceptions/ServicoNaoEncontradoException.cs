namespace GarageOS.Domain.Exceptions;

public class ServicoNaoEncontradoException : Exception
{
    public ServicoNaoEncontradoException(Guid id)
        : base($"Serviço com Id '{id}' não encontrado.") { }
}
