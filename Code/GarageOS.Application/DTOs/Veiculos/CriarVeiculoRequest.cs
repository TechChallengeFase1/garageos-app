namespace GarageOS.Application.DTOs.Veiculos;

public class CriarVeiculoRequest
{
    public string MarcaVeiculo { get; set; } = string.Empty;
    public string ModeloVeiculo { get; set; } = string.Empty;
    public string PlacaVeiculo { get; set; } = string.Empty;
    public int AnoVeiculo { get; set; }
    public decimal PrecoVeiculo { get; set; }
}
