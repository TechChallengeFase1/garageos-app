using GarageOS.Application.Abstractions;
using GarageOS.Domain.Exceptions;

namespace GarageOS.Application.UseCases.OrdensDeServico;

/// <summary>Confere no Execução Service se a peça existe e tem quantidade</summary>
internal static class ValidadorDePecas
{
    public static async Task<PecaDoEstoque> ObterDisponivelAsync(this IEstoqueClient estoque, Guid estoqueId, int quantidade)
    {
        var peca = await estoque.ObterPecaAsync(estoqueId)
            ?? throw new PecaNaoEncontradaException(estoqueId);

        // Aviso antecipado, não reserva: a reserva de verdade acontece depois
        // da aprovação do orçamento, no próprio Execução.
        if (peca.QuantidadeDisponivel < quantidade)
            throw new EstoqueInsuficienteException(peca.Nome, peca.QuantidadeDisponivel, quantidade);

        return peca;
    }
}
