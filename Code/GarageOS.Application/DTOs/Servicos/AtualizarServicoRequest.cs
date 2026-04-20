namespace GarageOS.Application.DTOs.Servicos;

public class AtualizarServicoRequest
{
    public string NomeServico { get; set; } = string.Empty;
    public decimal Preco { get; set; }
}
