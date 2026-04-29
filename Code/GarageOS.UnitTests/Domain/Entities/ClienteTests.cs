using FluentAssertions;
using GarageOS.Domain.Entities;
using GarageOS.Domain.ValueObjects;

namespace GarageOS.UnitTests.Domain.Entities;

public class ClienteTests
{
    [Fact]
    public void Construtor_ComDadosValidos_DeveCriarClienteAtivo()
    {
        // Arrange
        var endereco = CriarEndereco();

        // Act
        var cliente = new Cliente("João Silva", "00000000191", "joao@email.com", "11999999999", endereco);

        // Assert
        cliente.Nome.Should().Be("João Silva");
        cliente.Documento.Valor.Should().Be("00000000191");
        cliente.Email.Should().Be("joao@email.com");
        cliente.Telefone.Should().Be("11999999999");
        cliente.Ativo.Should().BeTrue();
        cliente.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Construtor_ComNomeVazio_DevelanarArgumentException(string? nome)
    {
        // Arrange
        var endereco = CriarEndereco();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Cliente(nome!, "00000000191", "joao@email.com", "11999999999", endereco));
        ex.Message.Should().Contain("Nome");
    }

    [Theory]
    [InlineData("joao.email")]
    [InlineData("joao")]
    [InlineData("")]
    public void Construtor_ComEmailInvalido_DevelanarArgumentException(string email)
    {
        // Arrange
        var endereco = CriarEndereco();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Cliente("João", "00000000191", email, "11999999999", endereco));
        ex.Message.Should().Contain("mail");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Construtor_ComTelefoneVazio_DeveLancarArgumentException(string telefone)
    {
        // Arrange
        var endereco = CriarEndereco();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Cliente("João", "00000000191", "joao@email.com", telefone, endereco));
        ex.Message.Should().Contain("Telefone");
    }

    [Fact]
    public void Atualizar_ComDadosValidos_DeveAtualizarCliente()
    {
        // Arrange
        var cliente = CriarCliente();
        var novoEndereco = new Endereco("Avenida Paulista", "1000", "Centro", "São Paulo", "SP", "01311100", "Apto 101");

        // Act
        cliente.Atualizar("Maria Silva", "maria@email.com", "11988888888", novoEndereco);

        // Assert
        cliente.Nome.Should().Be("Maria Silva");
        cliente.Email.Should().Be("maria@email.com");
        cliente.Telefone.Should().Be("11988888888");
        cliente.Endereco.Logradouro.Should().Be("Avenida Paulista");
    }

    [Fact]
    public void Desativar_QuandoAtivoETrue_DeveDesativar()
    {
        // Arrange
        var cliente = CriarCliente();

        // Act
        cliente.Desativar();

        // Assert
        cliente.Ativo.Should().BeFalse();
    }

    [Fact]
    public void Desativar_QuandoJaEstaInativo_DeveLancarArgumentException()
    {
        // Arrange
        var cliente = CriarCliente();
        cliente.Desativar();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => cliente.Desativar());
        ex.Message.Should().Contain("inativo");
    }

    [Fact]
    public void Ativar_QuandoInativoEFalse_DeveAtivar()
    {
        // Arrange
        var cliente = CriarCliente();
        cliente.Desativar();

        // Act
        cliente.Ativar();

        // Assert
        cliente.Ativo.Should().BeTrue();
    }

    [Fact]
    public void Ativar_QuandoJaEstaAtivo_DeveLancarArgumentException()
    {
        // Arrange
        var cliente = CriarCliente();

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => cliente.Ativar());
        ex.Message.Should().Contain("ativo");
    }

    private static Endereco CriarEndereco() =>
        new("Rua Teste", "123", "Centro", "São Paulo", "SP", "01234567");

    private static Cliente CriarCliente() =>
        new("João Silva", "00000000191", "joao@email.com", "11999999999", CriarEndereco());
}
