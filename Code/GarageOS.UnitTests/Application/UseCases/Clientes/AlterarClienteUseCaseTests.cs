using FluentAssertions;
using Moq;
using GarageOS.Application.DTOs.Clientes;
using GarageOS.Application.UseCases.Clientes;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.ValueObjects;

namespace GarageOS.UnitTests.Application.UseCases.Clientes;

public class AlterarClienteUseCaseTests
{
    private readonly Mock<IClienteRepository> _repositoryMock = new();
    private readonly AlterarClienteUseCase _useCase;

    public AlterarClienteUseCaseTests()
    {
        _useCase = new AlterarClienteUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveAtualizarCliente()
    {
        // Arrange
        var id = Guid.NewGuid();
        var clienteExistente = CriarCliente();
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

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(clienteExistente);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente)null);
        _repositoryMock.Setup(r => r.ObterPorTelefoneAsync(It.IsAny<string>())).ReturnsAsync((Cliente)null);
        _repositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<Cliente>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(id, request);

        // Assert
        result.Should().NotBeNull();
        result.Nome.Should().Be(request.Nome);
        _repositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Cliente>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComClienteNaoEncontrado_DeveLancarClienteNaoEncontradoException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new AtualizarClienteRequest();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Cliente)null);

        // Act & Assert
        await Assert.ThrowsAsync<ClienteNaoEncontradoException>(() => _useCase.ExecutarAsync(id, request));
    }

    [Fact]
    public async Task ExecutarAsync_ComEmailDuplicadoEmOutroCliente_DeveLancarClienteJaCadastradoException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var clienteExistente = CriarCliente();
        var outroCliente = CriarCliente();
        var request = new AtualizarClienteRequest
        {
            Nome = "Maria",
            Email = "outro@email.com",
            Telefone = "11988888888",
            Logradouro = "Rua",
            Numero = "123",
            Bairro = "Bairro",
            Cidade = "Cidade",
            Estado = "SP",
            Cep = "01234567"
        };

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(clienteExistente);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(request.Email)).ReturnsAsync(outroCliente);

        // Act & Assert
        await Assert.ThrowsAsync<ClienteJaCadastradoException>(() => _useCase.ExecutarAsync(id, request));
    }

    [Fact]
    public async Task ExecutarAsync_ComTelefoneDuplicadoEmOutroCliente_DeveLancarClienteJaCadastradoException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var clienteExistente = CriarCliente();
        var outroCliente = CriarCliente();
        var request = new AtualizarClienteRequest
        {
            Nome = "Maria",
            Email = "maria@email.com",
            Telefone = "11977777777",
            Logradouro = "Rua",
            Numero = "123",
            Bairro = "Bairro",
            Cidade = "Cidade",
            Estado = "SP",
            Cep = "01234567"
        };

        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(clienteExistente);
        _repositoryMock.Setup(r => r.ObterPorEmailAsync(It.IsAny<string>())).ReturnsAsync((Cliente)null);
        _repositoryMock.Setup(r => r.ObterPorTelefoneAsync(request.Telefone)).ReturnsAsync(outroCliente);

        // Act & Assert
        await Assert.ThrowsAsync<ClienteJaCadastradoException>(() => _useCase.ExecutarAsync(id, request));
    }

    private static Endereco CriarEndereco() =>
        new("Rua Teste", "123", "Centro", "São Paulo", "SP", "01234567");

    private static Cliente CriarCliente() =>
        new("João Silva", "00000000191", "joao@email.com", "11999999999", CriarEndereco());
}
