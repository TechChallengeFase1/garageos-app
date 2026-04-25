using FluentAssertions;
using FluentValidation;
using GarageOS.Application.DTOs.Clientes;
using GarageOS.Application.Validators.Clientes;

namespace GarageOS.UnitTests.Application.Validators.Clientes;

public class CriarClienteValidatorTests
{
    private readonly CriarClienteValidator _validator = new();

    [Fact]
    public async Task Validate_ComDadosValidos_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Documento = "00000000191",
            Email = "joao@email.com",
            Telefone = "11999999999",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01234567"
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
        var request = new CriarClienteRequest
        {
            Nome = "",
            Documento = "00000000191",
            Email = "joao@email.com",
            Telefone = "11999999999",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01234567"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Nome));
    }

    [Fact]
    public async Task Validate_ComNomeMuitoLongo_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarClienteRequest
        {
            Nome = new string('a', 151),
            Documento = "00000000191",
            Email = "joao@email.com",
            Telefone = "11999999999",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01234567"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Nome));
    }

    [Fact]
    public async Task Validate_ComDocumentoVazio_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Documento = "",
            Email = "joao@email.com",
            Telefone = "11999999999",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01234567"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Documento));
    }

    [Fact]
    public async Task Validate_ComDocumentoComDezDigitos_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Documento = "1234567890",
            Email = "joao@email.com",
            Telefone = "11999999999",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01234567"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Documento));
    }

    [Fact]
    public async Task Validate_ComEmailSemArroba_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Documento = "00000000191",
            Email = "joaoemail.com",
            Telefone = "11999999999",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01234567"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Email));
    }

    [Fact]
    public async Task Validate_ComEstadoDiferenteDeDoisCaracteres_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Documento = "00000000191",
            Email = "joao@email.com",
            Telefone = "11999999999",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SPP",
            Cep = "01234567"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Estado));
    }

    [Fact]
    public async Task Validate_ComCepComMenosDe8Digitos_DevefalharNaValidacao()
    {
        // Arrange
        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Documento = "00000000191",
            Email = "joao@email.com",
            Telefone = "11999999999",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "0123456"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(request.Cep));
    }

    [Fact]
    public async Task Validate_ComComplementoVazioOuNulo_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Documento = "00000000191",
            Email = "joao@email.com",
            Telefone = "11999999999",
            Logradouro = "Rua Teste",
            Numero = "123",
            Complemento = null,
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01234567"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_ComCNPJValido_DevePassarNaValidacao()
    {
        // Arrange
        var request = new CriarClienteRequest
        {
            Nome = "Empresa",
            Documento = "11222333000181",
            Email = "empresa@email.com",
            Telefone = "1133334444",
            Logradouro = "Av. Paulista",
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
    }
}
