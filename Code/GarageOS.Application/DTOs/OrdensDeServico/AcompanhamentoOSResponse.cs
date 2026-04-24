namespace GarageOS.Application.DTOs.OrdensDeServico;

public class AcompanhamentoOSResponse
{
    public string NumeroOS { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<AcompanhamentoServicoResponse> Servicos { get; set; } = [];
}

public class AcompanhamentoServicoResponse
{
    public string NomeServico { get; set; } = string.Empty;
    public string StatusExecucao { get; set; } = string.Empty;
}
