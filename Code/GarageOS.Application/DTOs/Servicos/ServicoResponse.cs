namespace GarageOS.Application.DTOs.Servicos;

public class ServicoResponse
{
    public Guid Id { get; set; }
    public string NomeServico { get; set; } = string.Empty;
    public decimal Preco { get; set; }
}
