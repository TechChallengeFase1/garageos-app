namespace GarageOS.Domain.Exceptions;

public class VeiculoNaoEncontradoException : Exception
{
    public VeiculoNaoEncontradoException(Guid placa)
        : base($"Veiculo com placa '{placa}' não encontrado.") { }
}