namespace GarageOS.Application.DTOs.Estoques;

public class CriarEstoqueRequest
{
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataEntrada { get; set; }
    public DateTime? DataSaida { get; set; }
    public string Fornecedor { get; set; } = string.Empty;
}
