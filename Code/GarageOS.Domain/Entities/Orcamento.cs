using GarageOS.Domain.Enums;
using GarageOS.Domain.Utils;

namespace GarageOS.Domain.Entities;

public class Orcamento
{
    public Guid Id { get; private set; }
    public StatusOrcamento Status { get; private set; }
    public decimal Preco { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }
    public Guid OrdemDeServicoId { get; private set; }

    protected Orcamento() { }

    public Orcamento(Guid ordemDeServicoId, decimal preco)
    {
        Id = Guid.NewGuid();
        OrdemDeServicoId = ordemDeServicoId;
        Preco = preco;
        Status = StatusOrcamento.Aprovado;
        CriadoEm = BrasiliaTime.Agora;
        AtualizadoEm = BrasiliaTime.Agora;
    }

    public void Aprovar()
    {
        Status = StatusOrcamento.Aprovado;
        AtualizadoEm = BrasiliaTime.Agora;
    }

    public void Rejeitar()
    {
        Status = StatusOrcamento.Rejeitado;
        AtualizadoEm = BrasiliaTime.Agora;
    }

    public void AtualizarPreco(decimal novoPreco)
    {
        Preco = novoPreco;
        AtualizadoEm = BrasiliaTime.Agora;
    }
}
