namespace GarageOS.Application.DTOs.Servicos;

public class CriarServicoRequest
{
    public string NomeServico { get; set; } = string.Empty;
    public decimal Preco { get; set; }
}
