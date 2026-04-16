namespace GarageOS.Domain.Entities;

public class Veiculo
{
    public Guid Id { get; set; }
    public string MarcaVeiculo { get; set; } = string.Empty;
    public string ModeloVeiculo { get; set; } = string.Empty;
    public string PlacaVeiculo { get; set; } = string.Empty;
    public int AnoVeiculo { get; set; }
    public decimal PrecoVeiculo { get; set; }

    protected Veiculo() { }

    public Veiculo(string marcaVeiculo, string modeloVeiculo, string placaVeiculo, int anoVeiculo, decimal precoVeiculo)
    {
        if (string.IsNullOrWhiteSpace(marcaVeiculo))
            throw new ArgumentException("A marca do veiculo não pode ser vazio.", nameof(marcaVeiculo));

        if (string.IsNullOrWhiteSpace(modeloVeiculo))
            throw new ArgumentException("O modelo do veiculo não pode ser vazio.", nameof(modeloVeiculo));

        if (string.IsNullOrWhiteSpace(placaVeiculo))
            throw new ArgumentException("A placa do veiculo não pode ser vazia.", nameof(placaVeiculo));

        if (anoVeiculo <= 0)
            throw new ArgumentException("O ano do veiculo não pode ser vazio.", nameof(anoVeiculo));

        if (precoVeiculo <= 0)
            throw new ArgumentException("Preço deve ser maior que zero.", nameof(precoVeiculo));

        Id = Guid.NewGuid();
        MarcaVeiculo = marcaVeiculo;
        ModeloVeiculo = modeloVeiculo;
        PlacaVeiculo = placaVeiculo;
        AnoVeiculo = anoVeiculo;
        PrecoVeiculo = precoVeiculo;
    }

    public void AtualizarParcial(string? marcaVeiculo, string? modeloVeiculo, string? placaVeiculo, int? anoVeiculo, decimal? precoVeiculo)
    {
        if (marcaVeiculo != null)
        {
            if (string.IsNullOrWhiteSpace(marcaVeiculo))
                throw new ArgumentException("Marca inválida");

            MarcaVeiculo = marcaVeiculo;
        }

        if (modeloVeiculo != null)
        {
            if (string.IsNullOrWhiteSpace(modeloVeiculo))
                throw new ArgumentException("Modelo inválido");

            ModeloVeiculo = modeloVeiculo;
        }

        if (placaVeiculo != null)
        {
            if (string.IsNullOrWhiteSpace(placaVeiculo))
                throw new ArgumentException("Placa inválida");

            PlacaVeiculo = placaVeiculo;
        }

        if (anoVeiculo.HasValue)
        {
            if (anoVeiculo <= 0)
                throw new ArgumentException("Ano inválido");

            AnoVeiculo = anoVeiculo.Value;
        }

        if (precoVeiculo.HasValue)
        {
            if (precoVeiculo <= 0)
                throw new ArgumentException("Preço inválido");

            PrecoVeiculo = precoVeiculo.Value;
        }
    }
}