namespace GarageOS.Application.DTOs.OrdensDeServico;

/// <summary>O que o cliente vê no acompanhamento público, sem dados sensíveis</summary>
public class AcompanhamentoOSResponse
{
    public string NumeroOS { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<AcompanhamentoServicoResponse> Servicos { get; set; } = [];
    public List<AcompanhamentoEtapaResponse> Historico { get; set; } = [];
}

public class AcompanhamentoServicoResponse
{
    public string NomeServico { get; set; } = string.Empty;
}

public class AcompanhamentoEtapaResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Em { get; set; }
}
