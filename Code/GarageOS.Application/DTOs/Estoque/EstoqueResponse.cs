namespace GarageOS.Application.DTOs.Estoques;

public class EstoqueResponse
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int Quantidade { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataEntrada { get; set; }
    public DateTime? DataSaida { get; set; }
    public string Fornecedor { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
