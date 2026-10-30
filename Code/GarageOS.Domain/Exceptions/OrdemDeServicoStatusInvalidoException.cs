namespace GarageOS.Domain.Exceptions;

public class OrdemDeServicoStatusInvalidoException : Exception
{
    public OrdemDeServicoStatusInvalidoException()
        : base("Status inválido para a Ordem de Serviço.")
    {
    }

    public OrdemDeServicoStatusInvalidoException(string message)
        : base(message)
    {
    }

    public OrdemDeServicoStatusInvalidoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
