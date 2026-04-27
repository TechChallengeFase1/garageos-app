namespace GarageOS.Domain.Exceptions;

public class OrcamentoNaoEncontradoException : Exception
{
    public OrcamentoNaoEncontradoException()
        : base("Orçamento não encontrado para esta Ordem de Serviço.")
    {
    }
}
