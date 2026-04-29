using FluentAssertions;
using Moq;
using GarageOS.Application.UseCases.Clientes;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.ValueObjects;

namespace GarageOS.UnitTests.Application.UseCases.Clientes;

public class DeletarClienteUseCaseTests
{
    private readonly Mock<IClienteRepository> _repositoryMock = new();
    private readonly DeletarClienteUseCase _useCase;

    public DeletarClienteUseCaseTests()
    {
        _useCase = new DeletarClienteUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdValido_DeveDesativarCliente()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cliente = CriarCliente();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(cliente);
        _repositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(id);

        // Assert
        cliente.Ativo.Should().BeFalse();
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Cliente>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdNaoEncontrado_DeveLancarClienteNaoEncontradoException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Cliente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ClienteNaoEncontradoException>(() => _useCase.ExecutarAsync(id));
    }

    [Fact]
    public async Task ExecutarAsync_UsaSoftDelete()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cliente = CriarCliente();
        cliente.Ativo.Should().BeTrue();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(cliente);
        _repositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Cliente>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ChamaRepositorioUmaVez()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cliente = CriarCliente();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(cliente);
        _repositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(id);

        // Assert
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Cliente>()), Times.Once);
    }

    private static Endereco CriarEndereco() =>
        new("Rua Teste", "123", "Centro", "São Paulo", "SP", "01234567");

    private static Cliente CriarCliente() =>
        new("João Silva", "00000000191", "joao@email.com", "11999999999", CriarEndereco());
}
