using GarageOS.Domain.Utils;

namespace GarageOS.Domain.Entities;

/// <summary>Peça incluída na OS, com nome e valor copiados do estoque</summary>
/// <remarks>
/// O estoque pertence ao Execução Service. Aqui fica só o ID da peça e a cópia
/// do que foi orçado, sem chave estrangeira: este serviço nunca lê o banco de
/// outro. No monólito esta tabela se chamava OrdensDeServicoEstoques e apontava
/// para Estoques por FK.
/// </remarks>
public class OrdemDeServicoPeca
{
    public Guid Id { get; private set; }
    public Guid OrdemDeServicoId { get; private set; }
    public Guid EstoqueId { get; private set; }
    public string Nome { get; private set; } = null!;
    public decimal ValorUnitario { get; private set; }
    public int Quantidade { get; private set; }
    public DateTime CriadoEm { get; private set; }

    public decimal Subtotal => ValorUnitario * Quantidade;

    protected OrdemDeServicoPeca() { }

    public OrdemDeServicoPeca(Guid ordemDeServicoId, Guid estoqueId, string nome, decimal valorUnitario, int quantidade)
    {
        if (estoqueId == Guid.Empty)
            throw new ArgumentException("Peça é obrigatória.", nameof(estoqueId));

        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome da peça é obrigatório.", nameof(nome));

        if (valorUnitario < 0)
            throw new ArgumentException("Valor não pode ser negativo.", nameof(valorUnitario));

        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

        Id = Guid.NewGuid();
        OrdemDeServicoId = ordemDeServicoId;
        EstoqueId = estoqueId;
        Nome = nome;
        ValorUnitario = valorUnitario;
        Quantidade = quantidade;
        CriadoEm = BrasiliaTime.Agora;
    }
}
