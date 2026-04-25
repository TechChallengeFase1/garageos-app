using FluentAssertions;
using GarageOS.Domain.Enums;
using GarageOS.Domain.ValueObjects;

namespace GarageOS.UnitTests.Domain.ValueObjects;

public class DocumentoTests
{
    [Fact]
    public void Construtor_ComCPFValido_DeveDefinirTipoCPF()
    {
        // Act
        var documento = new Documento("00000000191");

        // Assert
        documento.Tipo.Should().Be(TipoDocumento.CPF);
        documento.Valor.Should().Be("00000000191");
    }

    [Fact]
    public void Construtor_ComCPFComMascara_DeveExtrairApenasDigitos()
    {
        // Act
        var documento = new Documento("000.000.001-91");

        // Assert
        documento.Tipo.Should().Be(TipoDocumento.CPF);
        documento.Valor.Should().Be("00000000191");
    }

    [Theory]
    [InlineData("12345678900")]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    public void Construtor_ComCPFInvalido_DeveLancarArgumentException(string cpf)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Documento(cpf));
    }

    [Fact]
    public void Construtor_ComCNPJValido_DeveDefinirTipoCNPJ()
    {
        // Act - CNPJ válido de teste: 11.222.333/0001-81
        var documento = new Documento("11222333000181");

        // Assert
        documento.Tipo.Should().Be(TipoDocumento.CNPJ);
        documento.Valor.Should().Be("11222333000181");
    }

    [Fact]
    public void Construtor_ComCNPJComMascara_DeveExtrairApenasDigitos()
    {
        // Act
        var documento = new Documento("11.222.333/0001-81");

        // Assert
        documento.Tipo.Should().Be(TipoDocumento.CNPJ);
        documento.Valor.Should().Be("11222333000181");
    }

    [Theory]
    [InlineData("11111111111111")]
    [InlineData("00000000000000")]
    [InlineData("12345678901234")]
    public void Construtor_ComCNPJInvalido_DeveLancarArgumentException(string cnpj)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Documento(cnpj));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("123456789")]
    [InlineData("12345678901")]
    [InlineData("123456789012345")]
    public void Construtor_ComTamanhoInvalido_DeveLancarArgumentException(string documento)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Documento(documento));
    }

    [Fact]
    public void Construtor_ComApenasLetras_DeveLancarArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Documento("ABCDEFGHIJ"));
    }
}
