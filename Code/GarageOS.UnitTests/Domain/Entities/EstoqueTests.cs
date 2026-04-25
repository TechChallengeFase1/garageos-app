using FluentAssertions;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;

namespace GarageOS.UnitTests.Domain.Entities;

public class EstoqueTests
{
    [Fact]
    public void Construtor_ComDadosValidos_DeveCriarEstoqueDisponivel()
    {
        // Arrange
        var dataEntrada = DateTime.Now;

        // Act
        var estoque = new Estoque("Pneu", 10, 150.00m, dataEntrada, "Goodyear");

        // Assert
        estoque.Nome.Should().Be("Pneu");
        estoque.Quantidade.Should().Be(10);
        estoque.Valor.Should().Be(150.00m);
        estoque.Fornecedor.Should().Be("Goodyear");
        estoque.Status.Should().Be(StatusEstoque.Disponivel);
        estoque.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Construtor_ComQuantidadeZero_DeveDefinirStatusIndisponivel()
    {
        // Arrange
        var dataEntrada = DateTime.Now;

        // Act
        var estoque = new Estoque("Pneu", 0, 150.00m, dataEntrada, "Goodyear");

        // Assert
        estoque.Quantidade.Should().Be(0);
        estoque.Status.Should().Be(StatusEstoque.Indisponivel);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Construtor_ComNomeVazio_DeveLancarArgumentException(string nome)
    {
        // Arrange
        var dataEntrada = DateTime.Now;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Estoque(nome, 10, 150.00m, dataEntrada, "Goodyear"));
        ex.Message.Should().Contain("Nome");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Construtor_ComQuantidadeNegativa_DeveLancarArgumentException(int quantidade)
    {
        // Arrange
        var dataEntrada = DateTime.Now;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Estoque("Pneu", quantidade, 150.00m, dataEntrada, "Goodyear"));
        ex.Message.Should().Contain("Quantidade");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50.00)]
    public void Construtor_ComValorInvalido_DeveLancarArgumentException(decimal valor)
    {
        // Arrange
        var dataEntrada = DateTime.Now;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Estoque("Pneu", 10, valor, dataEntrada, "Goodyear"));
        ex.Message.Should().Contain("Valor");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Construtor_ComFornecedorVazio_DeveLancarArgumentException(string fornecedor)
    {
        // Arrange
        var dataEntrada = DateTime.Now;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Estoque("Pneu", 10, 150.00m, dataEntrada, fornecedor));
        ex.Message.Should().Contain("Fornecedor");
    }

    [Fact]
    public void Construtor_ComDataSaida_DeveArmazenarDataSaida()
    {
        // Arrange
        var dataEntrada = DateTime.Now.AddDays(-10);
        var dataSaida = DateTime.Now;

        // Act
        var estoque = new Estoque("Pneu", 5, 150.00m, dataEntrada, "Goodyear", dataSaida);

        // Assert
        estoque.DataSaida.Should().Be(dataSaida);
        estoque.DataEntrada.Should().Be(dataEntrada);
    }
}
