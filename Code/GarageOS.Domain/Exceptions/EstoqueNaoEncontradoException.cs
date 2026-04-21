namespace GarageOS.Domain.Exceptions;

public class EstoqueNaoEncontradoException : Exception
{
    public EstoqueNaoEncontradoException(Guid id)
        : base($"Item de estoque com Id '{id}' não encontrado.") { }
}
