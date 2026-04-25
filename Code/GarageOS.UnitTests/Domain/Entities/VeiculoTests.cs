using FluentAssertions;
using GarageOS.Domain.Entities;

namespace GarageOS.UnitTests.Domain.Entities;

public class VeiculoTests
{
    [Fact]
    public void Construtor_ComDadosValidos_DeveCriarVeiculo()
    {
        // Act
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);

        // Assert
        veiculo.MarcaVeiculo.Should().Be("Toyota");
        veiculo.ModeloVeiculo.Should().Be("Corolla");
        veiculo.PlacaVeiculo.Should().Be("ABC1234");
        veiculo.AnoVeiculo.Should().Be(2022);
        veiculo.PrecoVeiculo.Should().Be(95000.00m);
        veiculo.Id.Should().NotBe(Guid.Empty);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Construtor_ComMarcaVazia_DeveLancarArgumentException(string marca)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Veiculo(marca, "Corolla", "ABC1234", 2022, 95000.00m));
        ex.Message.Should().Contain("marca");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Construtor_ComModeloVazio_DeveLancarArgumentException(string modelo)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            new Veiculo("Toyota", modelo, "ABC1234", 2022, 95000.00m));
        ex.Message.Should().Contain("modelo");
    }

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCD1234")]
    [InlineData("123456")]
    [InlineData("")]
    public void Construtor_ComPlacaInvalida_DeveLancarArgumentException(string placa)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Veiculo("Toyota", "Corolla", placa, 2022, 95000.00m));
    }

    [Fact]
    public void Construtor_ComPlacaMercosul_DeveAceitarFormatoValido()
    {
        // Act
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1A23", 2022, 95000.00m);

        // Assert
        veiculo.PlacaVeiculo.Should().Be("ABC1A23");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Construtor_ComAnoInvalido_DeveLancarArgumentException(int ano)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Veiculo("Toyota", "Corolla", "ABC1234", ano, 95000.00m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100.00)]
    public void Construtor_ComPrecoInvalido_DeveLancarArgumentException(decimal preco)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new Veiculo("Toyota", "Corolla", "ABC1234", 2022, preco));
    }

    [Fact]
    public void AtualizarParcial_ComMarcaNula_NaoDeveMudaMarca()
    {
        // Arrange
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);

        // Act
        veiculo.AtualizarParcial(null, "Civic", null, null, null);

        // Assert
        veiculo.MarcaVeiculo.Should().Be("Toyota");
        veiculo.ModeloVeiculo.Should().Be("Civic");
    }

    [Fact]
    public void AtualizarParcial_ComModeloNulo_NaoDeveAtualizarModelo()
    {
        // Arrange
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);

        // Act
        veiculo.AtualizarParcial("Honda", null, "XYZ9999", 2023, null);

        // Assert
        veiculo.MarcaVeiculo.Should().Be("Honda");
        veiculo.ModeloVeiculo.Should().Be("Corolla");
        veiculo.PlacaVeiculo.Should().Be("XYZ9999");
        veiculo.AnoVeiculo.Should().Be(2023);
    }

    [Fact]
    public void VincularCliente_ComClienteIdValido_DeveVincular()
    {
        // Arrange
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);
        var clienteId = Guid.NewGuid();

        // Act
        veiculo.VincularCliente(clienteId);

        // Assert
        veiculo.ClienteId.Should().Be(clienteId);
    }

    [Fact]
    public void VincularCliente_ComGuidEmpty_DeveLancarArgumentException()
    {
        // Arrange
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            veiculo.VincularCliente(Guid.Empty));
        ex.Message.Should().Contain("ClienteId");
    }
}
