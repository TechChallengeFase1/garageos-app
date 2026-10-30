namespace GarageOS.Domain.Exceptions;

/// <summary>A peça não existe no estoque do Execução Service</summary>
public class PecaNaoEncontradaException : Exception
{
    public PecaNaoEncontradaException(Guid estoqueId)
        : base($"Peça '{estoqueId}' não encontrada no estoque.") { }
}
