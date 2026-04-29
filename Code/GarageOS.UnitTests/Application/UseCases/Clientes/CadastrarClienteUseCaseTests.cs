using FluentAssertions;
using Moq;
using GarageOS.Application.DTOs.Clientes;
using GarageOS.Application.UseCases.Clientes;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.ValueObjects;

namespace GarageOS.UnitTests.Application.UseCases.Clientes;

public class CadastrarClienteUseCaseTests
{
    private readonly Mock<IClienteRepository> _repositoryMock = new();
    private readonly CadastrarClienteUseCase _useCase;

    public CadastrarClienteUseCaseTests()
    {
        _useCase = new CadastrarClienteUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveCriarClienteERetornarResponse()
    {
        // Arrange
        var request = CriarRequest();
        _repositoryMock.Setup(r => r.ObterPorDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _repositoryMock.Setup(r => r.ObterPorTelefoneAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be(request.Nome);
        result.Email.Should().Be(request.Email);
        result.Telefone.Should().Be(request.Telefone);
        result.Ativo.Should().BeTrue();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComDocumentoDuplicado_DeveLancarClienteJaCadastradoException()
    {
        // Arrange
        var request = CriarRequest();
        var clienteExistente = CriarCliente();
        _repositoryMock.Setup(r => r.ObterPorDocumentoAsync(It.IsAny<string>()))
            .ReturnsAsync(clienteExistente);

        // Act & Assert
        await Assert.ThrowsAsync<ClienteJaCadastradoException>(() => _useCase.ExecutarAsync(request));
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComEmailDuplicado_DeveLancarClienteJaCadastradoException()
    {
        // Arrange
        var request = CriarRequest();
        var clienteExistente = CriarCliente();
        _repositoryMock.Setup(r => r.ObterPorDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync(clienteExistente);

        // Act & Assert
        await Assert.ThrowsAsync<ClienteJaCadastradoException>(() => _useCase.ExecutarAsync(request));
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComTelefoneDuplicado_DeveLancarClienteJaCadastradoException()
    {
        // Arrange
        var request = CriarRequest();
        var clienteExistente = CriarCliente();
        _repositoryMock.Setup(r => r.ObterPorDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _repositoryMock.Setup(r => r.ObterPorTelefoneAsync(It.IsAny<string>())).ReturnsAsync(clienteExistente);

        // Act & Assert
        await Assert.ThrowsAsync<ClienteJaCadastradoException>(() => _useCase.ExecutarAsync(request));
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_RetornaResponseComDadosCorretos()
    {
        // Arrange
        var request = CriarRequest();
        _repositoryMock.Setup(r => r.ObterPorDocumentoAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _repositoryMock.Setup(r => r.ObterPorTelefoneAsync(It.IsAny<string>())).ReturnsAsync((Cliente?)null);
        _repositoryMock.Setup(r => r.AdicionarAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(request);

        // Assert
        result.Id.Should().NotBe(Guid.Empty);
        result.Endereco.Should().NotBeNull();
        result.Endereco.Logradouro.Should().Be(request.Logradouro);
        result.TipoDocumento.Should().Be("CPF");
    }

    private static CriarClienteRequest CriarRequest() =>
        new()
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

    private static Endereco CriarEndereco() =>
        new("Rua Teste", "123", "Centro", "São Paulo", "SP", "01234567");

    private static Cliente CriarCliente() =>
        new("João Silva", "00000000191", "joao@email.com", "11999999999", CriarEndereco());
}
