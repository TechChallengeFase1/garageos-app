using GarageOS.Domain.Enums;
using GarageOS.Domain.Utils;

namespace GarageOS.Domain.Entities;

public class OrdemDeServico
{
    public Guid Id { get; private set; }
    public string NumeroOS { get; private set; } = null!;
    public StatusOrdemDeServico Status { get; private set; }
    public DateTime CriadoEm { get; private set; }
    public DateTime? FinalizadaEm { get; private set; }
    public DateTime AtualizadoEm { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid VeiculoId { get; private set; }
    public Cliente? Cliente { get; private set; } // NOSONAR - EF Core usa via reflection
    public Veiculo? Veiculo { get; private set; } // NOSONAR - EF Core usa via reflection
    public List<OrdemDeServicoServico> Servicos { get; private set; } = [];
    public List<OrdemDeServicoEstoque> Estoques { get; private set; } = [];
    public Orcamento? Orcamento { get; private set; }

    protected OrdemDeServico() { }

    public OrdemDeServico(string numeroOS, Guid clienteId, Guid veiculoId)
    {
        Id = Guid.NewGuid();
        NumeroOS = numeroOS;
        ClienteId = clienteId;
        VeiculoId = veiculoId;
        Status = StatusOrdemDeServico.Recebida;
        CriadoEm = BrasiliaTime.Agora;
        AtualizadoEm = BrasiliaTime.Agora;
    }

    public void AdicionarServico(OrdemDeServicoServico servico)
    {
        Servicos.Add(servico);
        Status = StatusOrdemDeServico.EmDiagnostico;
        AtualizadoEm = BrasiliaTime.Agora;
    }

    public void AdicionarEstoque(OrdemDeServicoEstoque estoque)
    {
        Estoques.Add(estoque);
        Status = StatusOrdemDeServico.EmDiagnostico;
        AtualizadoEm = BrasiliaTime.Agora;
    }

    public void AvancarParaAguardandoAprovacao()
    {
        Status = StatusOrdemDeServico.AguardandoAprovacao;
        AtualizadoEm = BrasiliaTime.Agora;
    }

    public void AvancarParaEmExecucao()
    {
        Status = StatusOrdemDeServico.EmExecucao;
        AtualizadoEm = BrasiliaTime.Agora;
    }

    public void AlterarStatus(StatusOrdemDeServico novoStatus)
    {
        if (novoStatus != StatusOrdemDeServico.Finalizada &&
            novoStatus != StatusOrdemDeServico.Entregue)
            throw new ArgumentException("Apenas status Finalizada e Entregue podem ser alterados manualmente.");

        Status = novoStatus;
        FinalizadaEm = BrasiliaTime.Agora;
        AtualizadoEm = BrasiliaTime.Agora;
    }

    public void VincularOrcamento(Orcamento orcamento)
    {
        Orcamento = orcamento;
        AtualizadoEm = BrasiliaTime.Agora;
    }
}
