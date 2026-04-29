using GarageOS.Domain.Enums;

namespace GarageOS.Domain.Entities;

public class Estoque
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public int Quantidade { get; private set; }
    public decimal Valor { get; private set; }
    public DateTime DataEntrada { get; private set; }
    public DateTime? DataSaida { get; private set; }
    public string Fornecedor { get; private set; } = null!;
    public StatusEstoque Status { get; private set; }

    protected Estoque() { }

    public Estoque(string nome, int quantidade, decimal valor, DateTime dataEntrada, string fornecedor, DateTime? dataSaida = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do item não pode ser vazio.", nameof(nome));

        if (quantidade < 0)
            throw new ArgumentException("Quantidade não pode ser negativa.", nameof(quantidade));

        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero.", nameof(valor));

        if (string.IsNullOrWhiteSpace(fornecedor))
            throw new ArgumentException("Fornecedor não pode ser vazio.", nameof(fornecedor));

        Id = Guid.NewGuid();
        Nome = nome;
        Quantidade = quantidade;
        Valor = valor;
        DataEntrada = dataEntrada;
        DataSaida = dataSaida;
        Fornecedor = fornecedor;
        Status = quantidade > 0 ? StatusEstoque.Disponivel : StatusEstoque.Indisponivel;
    }

    public void DarBaixa(int quantidade)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.", nameof(quantidade));

        if (quantidade > Quantidade)
            throw new InvalidOperationException($"Estoque insuficiente para '{Nome}'. Disponível: {Quantidade}, Solicitado: {quantidade}.");

        Quantidade -= quantidade;
        Status = Quantidade > 0 ? StatusEstoque.Disponivel : StatusEstoque.Indisponivel;
    }

    public void Atualizar(string nome, int quantidade, decimal valor, DateTime dataEntrada, string fornecedor, DateTime? dataSaida = null)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome do item não pode ser vazio.", nameof(nome));

        if (quantidade < 0)
            throw new ArgumentException("Quantidade não pode ser negativa.", nameof(quantidade));

        if (valor <= 0)
            throw new ArgumentException("Valor deve ser maior que zero.", nameof(valor));

        if (string.IsNullOrWhiteSpace(fornecedor))
            throw new ArgumentException("Fornecedor não pode ser vazio.", nameof(fornecedor));

        Nome = nome;
        Quantidade = quantidade;
        Valor = valor;
        DataEntrada = dataEntrada;
        DataSaida = dataSaida;
        Fornecedor = fornecedor;
        Status = quantidade > 0 ? StatusEstoque.Disponivel : StatusEstoque.Indisponivel;
    }
}
