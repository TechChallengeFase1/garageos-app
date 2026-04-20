namespace GarageOS.Application.DTOs.Veiculos;

public class AtualizarVeiculoRequest
{
    public string? MarcaVeiculo { get; set; }
    public string? ModeloVeiculo { get; set; }
    public string? PlacaVeiculo { get; set; }
    public int? AnoVeiculo { get; set; }
    public decimal? PrecoVeiculo { get; set; }
}
