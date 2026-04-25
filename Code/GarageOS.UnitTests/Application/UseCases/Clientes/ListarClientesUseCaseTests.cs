using FluentAssertions;
using Moq;
using GarageOS.Application.UseCases.Clientes;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.ValueObjects;

namespace GarageOS.UnitTests.Application.UseCases.Clientes;

public class ListarClientesUseCaseTests
{
    private readonly Mock<IClienteRepository> _repositoryMock = new();
    private readonly ListarClientesUseCase _useCase;

    public ListarClientesUseCaseTests()
    {
        _useCase = new ListarClientesUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComClientesExistentes_DeveRetornarLista()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            CriarCliente(),
            CriarCliente(),
            CriarCliente()
        };
        _repositoryMock.Setup(r => r.ListarTodosAsync()).ReturnsAsync(clientes);

        // Act
        var result = await _useCase.ExecutarAsync();

        // Assert
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task ExecutarAsync_SemClientes_DeveRetornarListaVazia()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ListarTodosAsync()).ReturnsAsync(new List<Cliente>());

        // Act
        var result = await _useCase.ExecutarAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecutarAsync_MapeiaClientesCorretamente()
    {
        // Arrange
        var cliente = CriarCliente();
        var clientes = new List<Cliente> { cliente };
        _repositoryMock.Setup(r => r.ListarTodosAsync()).ReturnsAsync(clientes);

        // Act
        var result = await _useCase.ExecutarAsync();

        // Assert
        var response = result.First();
        response.Nome.Should().Be(cliente.Nome);
        response.Email.Should().Be(cliente.Email);
        response.Ativo.Should().Be(cliente.Ativo);
    }

    [Fact]
    public async Task ExecutarAsync_RetornaIEnumerable()
    {
        // Arrange
        var clientes = new List<Cliente> { CriarCliente() };
        _repositoryMock.Setup(r => r.ListarTodosAsync()).ReturnsAsync(clientes);

        // Act
        var result = await _useCase.ExecutarAsync();

        // Assert
        result.Should().BeAssignableTo<IEnumerable<dynamic>>();
    }

    private static Endereco CriarEndereco() =>
        new("Rua Teste", "123", "Centro", "São Paulo", "SP", "01234567");

    private static Cliente CriarCliente() =>
        new("João Silva", "00000000191", "joao@email.com", "11999999999", CriarEndereco());
}
