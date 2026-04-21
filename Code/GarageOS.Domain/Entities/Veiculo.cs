using System.Text.RegularExpressions;

namespace GarageOS.Domain.Entities;


public class Veiculo
{
    private static readonly Regex PlacaRegex = new(
        @"^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$",
        RegexOptions.Compiled
    );

    public Guid Id { get; set; }
    public string MarcaVeiculo { get; set; } = string.Empty;
    public string ModeloVeiculo { get; set; } = string.Empty;
    public string PlacaVeiculo { get; set; } = string.Empty;
    public int AnoVeiculo { get; set; }
    public decimal PrecoVeiculo { get; set; }

    public Guid? ClienteId { get; private set; }
    public Cliente? Cliente { get; private set; }

    protected Veiculo() { }

    public Veiculo(string marcaVeiculo, string modeloVeiculo, string placaVeiculo, int anoVeiculo, decimal precoVeiculo)
    {
        if (string.IsNullOrWhiteSpace(marcaVeiculo))
            throw new ArgumentException("A marca do veiculo não pode ser vazio.", nameof(marcaVeiculo));

        if (string.IsNullOrWhiteSpace(modeloVeiculo))
            throw new ArgumentException("O modelo do veiculo não pode ser vazio.", nameof(modeloVeiculo));

        if (!ValidarPlaca(placaVeiculo))
            throw new ArgumentException("Placa do veículo inválida.");

        if (anoVeiculo <= 0)
            throw new ArgumentException("O ano do veiculo não pode ser vazio.", nameof(anoVeiculo));

        if (precoVeiculo <= 0)
            throw new ArgumentException("Preço deve ser maior que zero.", nameof(precoVeiculo));

        Id = Guid.NewGuid();
        MarcaVeiculo = marcaVeiculo;
        ModeloVeiculo = modeloVeiculo;
        PlacaVeiculo = placaVeiculo.ToUpper().Trim();
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

    private static bool ValidarPlaca(string placa)
    {
        if (string.IsNullOrWhiteSpace(placa))
            return false;

        placa = placa.Replace("-", "").ToUpper().Trim();

        return PlacaRegex.IsMatch(placa);
    }

   public void VincularCliente(Guid clienteId)
    {
        if (clienteId == Guid.Empty)
            throw new ArgumentException("ClienteId inválido");

        ClienteId = clienteId;
    }
}