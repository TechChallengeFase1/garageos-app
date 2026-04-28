namespace GarageOS.Application.DTOs.OrdensDeServico;

public class AgingServicoResponse
{
    public Guid ServicoId { get; set; }
    public string ServicoNome { get; set; } = string.Empty;
    public int TotalExecucoes { get; set; }
    public double TempoMedioMinutos { get; set; }
    public string TempoMedioFormatado { get; set; } = string.Empty;
}
