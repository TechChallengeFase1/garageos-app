using GarageOS.Domain.Enums;

namespace GarageOS.Application.DTOs.OrdensDeServico;

public class OrdemDeServicoResponse
{
    public Guid Id { get; set; }
    public string NumeroOS { get; set; } = string.Empty;
    public StatusOrdemDeServico Status { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? FinalizadaEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public Guid ClienteId { get; set; }
    public Guid VeiculoId { get; set; }
    public List<ServicoItemResponse> Servicos { get; set; } = [];
    public List<EstoqueItemResponse> Estoques { get; set; } = [];
    public OrcamentoResponse? Orcamento { get; set; }
}

public class ServicoItemResponse
{
    public Guid Id { get; set; }
    public Guid ServicoId { get; set; }
    public string ServicoNome { get; set; } = string.Empty;
    public StatusExecucaoServico Status { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? IniciadaEm { get; set; }
    public DateTime? FinalizadaEm { get; set; }
}

public class EstoqueItemResponse
{
    public Guid Id { get; set; }
    public Guid EstoqueId { get; set; }
    public string EstoqueNome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
}

public class OrcamentoResponse
{
    public Guid Id { get; set; }
    public StatusOrcamento Status { get; set; }
    public decimal Preco { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
}
