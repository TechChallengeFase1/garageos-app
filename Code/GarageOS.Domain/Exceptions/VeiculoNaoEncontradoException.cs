namespace GarageOS.Domain.Exceptions;

public class VeiculoNaoEncontradoException : Exception
{
    public VeiculoNaoEncontradoException(Guid id)
        : base($"Veiculo '{id}' não encontrado.") { }
}