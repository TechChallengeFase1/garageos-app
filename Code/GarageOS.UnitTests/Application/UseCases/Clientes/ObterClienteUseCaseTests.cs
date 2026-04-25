using FluentAssertions;
using Moq;
using GarageOS.Application.UseCases.Clientes;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.ValueObjects;

namespace GarageOS.UnitTests.Application.UseCases.Clientes;

public class ObterClienteUseCaseTests
{
    private readonly Mock<IClienteRepository> _repositoryMock = new();
    private readonly ObterClienteUseCase _useCase;

    public ObterClienteUseCaseTests()
    {
        _useCase = new ObterClienteUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdValido_DeveRetornarClienteResponse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cliente = CriarCliente();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(cliente);

        // Act
        var result = await _useCase.ExecutarAsync(id);

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be(cliente.Nome);
        result.Email.Should().Be(cliente.Email);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdNaoEncontrado_DeveLancarClienteNaoEncontradoException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Cliente)null);

        // Act & Assert
        await Assert.ThrowsAsync<ClienteNaoEncontradoException>(() => _useCase.ExecutarAsync(id));
    }

    [Fact]
    public async Task ExecutarAsync_RetornaResponseComEnderecoMapeado()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cliente = CriarCliente();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(cliente);

        // Act
        var result = await _useCase.ExecutarAsync(id);

        // Assert
        result.Endereco.Should().NotBeNull();
        result.Endereco.Logradouro.Should().Be(cliente.Endereco.Logradouro);
        result.Endereco.Cidade.Should().Be(cliente.Endereco.Cidade);
    }

    [Fact]
    public async Task ExecutarAsync_RetornaResponseComAtivoCorreto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cliente = CriarCliente();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(cliente);

        // Act
        var result = await _useCase.ExecutarAsync(id);

        // Assert
        result.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task ExecutarAsync_RetornaResponseComTipoDocumento()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cliente = CriarCliente();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(cliente);

        // Act
        var result = await _useCase.ExecutarAsync(id);

        // Assert
        result.TipoDocumento.Should().Be("CPF");
    }

    private static Endereco CriarEndereco() =>
        new("Rua Teste", "123", "Centro", "São Paulo", "SP", "01234567");

    private static Cliente CriarCliente() =>
        new("João Silva", "00000000191", "joao@email.com", "11999999999", CriarEndereco());
}
