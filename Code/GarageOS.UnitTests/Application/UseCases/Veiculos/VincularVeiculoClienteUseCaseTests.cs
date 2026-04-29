using FluentAssertions;
using Moq;
using GarageOS.Application.UseCases.Veiculos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.ValueObjects;

namespace GarageOS.UnitTests.Application.UseCases.Veiculos;

public class VincularVeiculoClienteUseCaseTests
{
    private readonly Mock<IVeiculoRepository> _veiculoRepositoryMock = new();
    private readonly Mock<IClienteRepository> _clienteRepositoryMock = new();
    private readonly VincularVeiculoClienteUseCase _useCase;

    public VincularVeiculoClienteUseCaseTests()
    {
        _useCase = new VincularVeiculoClienteUseCase(_veiculoRepositoryMock.Object, _clienteRepositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComVeiculoEClienteValidos_DeveVincularClienteAoVeiculo()
    {
        // Arrange
        var veiculoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);
        var cliente = CriarCliente();
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync(veiculoId)).ReturnsAsync(veiculo);
        _clienteRepositoryMock.Setup(r => r.ObterPorIdAsync(clienteId)).ReturnsAsync(cliente);
        _veiculoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<Veiculo>())).Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(veiculoId, clienteId);

        // Assert
        veiculo.ClienteId.Should().Be(clienteId);
        _veiculoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Veiculo>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComVeiculoNaoEncontrado_DeveLancarVeiculoNaoEncontradoException()
    {
        // Arrange
        var veiculoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync(veiculoId)).ReturnsAsync((Veiculo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<VeiculoNaoEncontradoException>(() => _useCase.ExecutarAsync(veiculoId, clienteId));
        _clienteRepositoryMock.Verify(r => r.ObterPorIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComClienteNaoEncontrado_DeveLancarClienteNaoEncontradoException()
    {
        // Arrange
        var veiculoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync(veiculoId)).ReturnsAsync(veiculo);
        _clienteRepositoryMock.Setup(r => r.ObterPorIdAsync(clienteId)).ReturnsAsync((Cliente?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ClienteNaoEncontradoException>(() => _useCase.ExecutarAsync(veiculoId, clienteId));
        _veiculoRepositoryMock.Verify(r => r.AtualizarAsync(It.IsAny<Veiculo>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_VincularCorreto_VeiculoDeveConterClienteId()
    {
        // Arrange
        var veiculoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);
        var cliente = CriarCliente();
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync(veiculoId)).ReturnsAsync(veiculo);
        _clienteRepositoryMock.Setup(r => r.ObterPorIdAsync(clienteId)).ReturnsAsync(cliente);
        _veiculoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<Veiculo>())).Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(veiculoId, clienteId);

        // Assert
        veiculo.ClienteId.Should().Be(clienteId);
    }

    [Fact]
    public async Task ExecutarAsync_ConsultaAmbosRepositoriosUmaVez()
    {
        // Arrange
        var veiculoId = Guid.NewGuid();
        var clienteId = Guid.NewGuid();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);
        var cliente = CriarCliente();
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync(veiculoId)).ReturnsAsync(veiculo);
        _clienteRepositoryMock.Setup(r => r.ObterPorIdAsync(clienteId)).ReturnsAsync(cliente);
        _veiculoRepositoryMock.Setup(r => r.AtualizarAsync(It.IsAny<Veiculo>())).Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(veiculoId, clienteId);

        // Assert
        _veiculoRepositoryMock.Verify(r => r.ObterPorIdAsync(veiculoId), Times.Once);
        _clienteRepositoryMock.Verify(r => r.ObterPorIdAsync(clienteId), Times.Once);
    }

    private static Endereco CriarEndereco() =>
        new("Rua Teste", "123", "Centro", "São Paulo", "SP", "01234567");

    private static Cliente CriarCliente() =>
        new("João Silva", "00000000191", "joao@email.com", "11999999999", CriarEndereco());
}
