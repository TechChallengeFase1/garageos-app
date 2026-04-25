using FluentAssertions;
using GarageOS.Application.DTOs.Clientes;
using GarageOS.Application.Validators.Clientes;

namespace GarageOS.UnitTests.Application.Validators.Clientes;

public class AtualizarClienteValidatorTests
{
    private readonly AtualizarClienteValidator _validator = new();

    [Fact]
    public async Task Validate_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var request = new AtualizarClienteRequest
        {
            Nome = "Maria Silva",
            Email = "maria@email.com",
            Telefone = "11988888888",
            Logradouro = "Avenida Paulista",
            Numero = "1000",
            Bairro = "Bela Vista",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01311100"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Validate_ComNomeVazio_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AtualizarClienteRequest
        {
            Nome = "",
            Email = "maria@email.com",
            Telefone = "11988888888",
            Logradouro = "Avenida Paulista",
            Numero = "1000",
            Bairro = "Bela Vista",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01311100"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Nome));
    }

    [Fact]
    public async Task Validate_ComEmailInvalido_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AtualizarClienteRequest
        {
            Nome = "Maria Silva",
            Email = "mariasemail.com",
            Telefone = "11988888888",
            Logradouro = "Avenida Paulista",
            Numero = "1000",
            Bairro = "Bela Vista",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01311100"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Email));
    }

    [Fact]
    public async Task Validate_ComLogradouroVazio_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AtualizarClienteRequest
        {
            Nome = "Maria Silva",
            Email = "maria@email.com",
            Telefone = "11988888888",
            Logradouro = "",
            Numero = "1000",
            Bairro = "Bela Vista",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01311100"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Logradouro));
    }

    [Fact]
    public async Task Validate_ComEstadoComUmCaractere_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AtualizarClienteRequest
        {
            Nome = "Maria Silva",
            Email = "maria@email.com",
            Telefone = "11988888888",
            Logradouro = "Avenida Paulista",
            Numero = "1000",
            Bairro = "Bela Vista",
            Cidade = "São Paulo",
            Estado = "S",
            Cep = "01311100"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Estado));
    }

    [Fact]
    public async Task Validate_ComCepComLetras_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AtualizarClienteRequest
        {
            Nome = "Maria Silva",
            Email = "maria@email.com",
            Telefone = "11988888888",
            Logradouro = "Avenida Paulista",
            Numero = "1000",
            Bairro = "Bela Vista",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "0131110A"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Cep));
    }

    [Fact]
    public async Task Validate_ComComplementoMuitoLongo_DevefalharNaValidacao()
    {
        // Arrange
        var request = new AtualizarClienteRequest
        {
            Nome = "Maria Silva",
            Email = "maria@email.com",
            Telefone = "11988888888",
            Logradouro = "Avenida Paulista",
            Numero = "1000",
            Complemento = new string('a', 101),
            Bairro = "Bela Vista",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01311100"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Complemento));
    }

    [Fact]
    public async Task Validate_SemValidarDocumento_DevePassarComDocumentoVazio()
    {
        // Arrange - AtualizarClienteValidator não valida documento
        var request = new AtualizarClienteRequest
        {
            Nome = "Maria Silva",
            Email = "maria@email.com",
            Telefone = "11988888888",
            Logradouro = "Avenida Paulista",
            Numero = "1000",
            Bairro = "Bela Vista",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01311100"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.FirstOrDefault(e => e.PropertyName == nameof(request.Nome)).Should().BeNull();
    }
}
