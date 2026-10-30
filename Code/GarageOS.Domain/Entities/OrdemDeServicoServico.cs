using GarageOS.Domain.Utils;

namespace GarageOS.Domain.Entities;

/// <summary>Serviço do catálogo incluído na OS, com nome e preço copiados</summary>
/// <remarks>
/// A cópia (snapshot) congela o que foi combinado com o cliente: se o preço do
/// serviço mudar no catálogo depois, esta OS não muda. É também o que vai no
/// pedido de orçamento ao Billing, que não consulta o catálogo.
///
/// O andamento da execução (iniciado, finalizado) saiu daqui na Fase 4: é do
/// Execução Service.
/// </remarks>
public class OrdemDeServicoServico
{
    public Guid Id { get; private set; }
    public Guid OrdemDeServicoId { get; private set; }
    public Guid ServicoId { get; private set; }
    public string Nome { get; private set; } = null!;
    public decimal PrecoUnitario { get; private set; }
    public DateTime CriadoEm { get; private set; }

    protected OrdemDeServicoServico() { }

    public OrdemDeServicoServico(Guid ordemDeServicoId, Guid servicoId, string nome, decimal precoUnitario)
    {
        if (servicoId == Guid.Empty)
            throw new ArgumentException("Serviço é obrigatório.", nameof(servicoId));

        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do serviço é obrigatório.", nameof(nome));

        if (precoUnitario < 0)
            throw new ArgumentException("Preço não pode ser negativo.", nameof(precoUnitario));

        Id = Guid.NewGuid();
        OrdemDeServicoId = ordemDeServicoId;
        ServicoId = servicoId;
        Nome = nome;
        PrecoUnitario = precoUnitario;
        CriadoEm = BrasiliaTime.Agora;
    }
}
