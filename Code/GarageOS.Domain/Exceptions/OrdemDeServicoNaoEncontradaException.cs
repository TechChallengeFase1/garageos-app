namespace GarageOS.Domain.Exceptions;

public class OrdemDeServicoNaoEncontradaException : Exception
{
    public OrdemDeServicoNaoEncontradaException()
        : base("Ordem de Serviço não encontrada.")
    {
    }

    public OrdemDeServicoNaoEncontradaException(string message)
        : base(message)
    {
    }
}
