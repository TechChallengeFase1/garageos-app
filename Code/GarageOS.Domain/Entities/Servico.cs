namespace GarageOS.Domain.Entities;

public class Servico
{
    public Guid Id { get; private set; }
    public string NomeServico { get; private set; } = null!;
    public decimal Preco { get; private set; }

    // Construtor protegido para EF Core
    protected Servico() { }

    public Servico(string nomeServico, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(nomeServico))
            throw new ArgumentException("Nome do serviço não pode ser vazio.", nameof(nomeServico));

        if (preco <= 0)
            throw new ArgumentException("Preço deve ser maior que zero.", nameof(preco));

        Id = Guid.NewGuid();
        NomeServico = nomeServico;
        Preco = preco;
    }

    public void Atualizar(string nomeServico, decimal preco)
    {
        if (string.IsNullOrWhiteSpace(nomeServico))
            throw new ArgumentException("Nome do serviço não pode ser vazio.", nameof(nomeServico));

        if (preco <= 0)
            throw new ArgumentException("Preço deve ser maior que zero.", nameof(preco));

        NomeServico = nomeServico;
        Preco = preco;
    }
}
