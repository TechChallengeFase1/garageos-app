using FluentAssertions;
using GarageOS.Domain.Entities;

namespace GarageOS.UnitTests.Domain.Entities;

public class ServicoTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveRetornarServicoComId()
    {
        // Arrange & Act
        var servico = new Servico("Troca de óleo", 150.00m);

        // Assert
        servico.Id.Should().NotBeEmpty();
        servico.NomeServico.Should().Be("Troca de óleo");
        servico.Preco.Should().Be(150.00m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_ComNomeVazio_DeveLancarArgumentException(string nome)
    {
        // Act
        var act = () => new Servico(nome, 100m);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*nome*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Criar_ComPrecoInvalido_DeveLancarArgumentException(decimal preco)
    {
        // Act
        var act = () => new Servico("Revisão", preco);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*preco*");
    }

    [Fact]
    public void Atualizar_ComDadosValidos_DeveAlterarPropriedades()
    {
        // Arrange
        var servico = new Servico("Troca de óleo", 150.00m);

        // Act
        servico.Atualizar("Alinhamento", 200.00m);

        // Assert
        servico.NomeServico.Should().Be("Alinhamento");
        servico.Preco.Should().Be(200.00m);
    }

    [Fact]
    public void Atualizar_ComNomeVazio_DeveLancarArgumentException()
    {
        // Arrange
        var servico = new Servico("Troca de óleo", 150.00m);

        // Act
        var act = () => servico.Atualizar("", 200.00m);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Atualizar_ComPrecoZero_DeveLancarArgumentException()
    {
        // Arrange
        var servico = new Servico("Troca de óleo", 150.00m);

        // Act
        var act = () => servico.Atualizar("Alinhamento", 0);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
