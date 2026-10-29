namespace GarageOS.Application.DTOs.OrdensDeServico;

public class HistoricoStatusResponse
{
    public string? StatusAnterior { get; set; }
    public string StatusNovo { get; set; } = string.Empty;
    public string Origem { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public DateTime OcorridoEm { get; set; }
}
