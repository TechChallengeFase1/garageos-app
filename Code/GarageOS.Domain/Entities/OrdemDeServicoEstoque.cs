namespace GarageOS.Domain.Entities;

public class OrdemDeServicoEstoque
{
    public Guid Id { get; private set; }
    public Guid OrdemDeServicoId { get; private set; }
    public Guid EstoqueId { get; private set; }
    public int Quantidade { get; private set; }
    public Estoque? Estoque { get; private set; }

    protected OrdemDeServicoEstoque() { }

    public OrdemDeServicoEstoque(Guid ordemDeServicoId, Guid estoqueId, int quantidade)
    {
        Id = Guid.NewGuid();
        OrdemDeServicoId = ordemDeServicoId;
        EstoqueId = estoqueId;
        Quantidade = quantidade;

        Validar();
    }

    private void Validar()
    {
        if (Quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");
    }
}
