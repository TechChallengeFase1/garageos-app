using FluentAssertions;
using Moq;
using GarageOS.Application.UseCases.Veiculos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;

namespace GarageOS.UnitTests.Application.UseCases.Veiculos;

public class DeletarVeiculoUseCaseTests
{
    private readonly Mock<IVeiculoRepository> _repositoryMock = new();
    private readonly DeletarVeiculoUseCase _useCase;

    public DeletarVeiculoUseCaseTests()
    {
        _useCase = new DeletarVeiculoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdValido_DeveRetornarTrueEDeletarVeiculo()
    {
        // Arrange
        var id = Guid.NewGuid();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync(veiculo);
        _repositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecutarAsync(id);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.RemoverAsync(id), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComIdNaoEncontrado_DeveRetornarFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Veiculo?)null);

        // Act
        var result = await _useCase.ExecutarAsync(id);

        // Assert
        result.Should().BeFalse();
        _repositoryMock.Verify(r => r.RemoverAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_NaoLancaExcecaoQuandoNaoEncontrado()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.ObterPorIdAsync(id)).ReturnsAsync((Veiculo?)null);

        // Act & Assert
        var result = await _useCase.ExecutarAsync(id);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExecutarAsync_ComMultiplosChamados_DeveRetornarFalseNaSegundaVez()
    {
        // Arrange
        var id = Guid.NewGuid();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 95000.00m);
        var sequence = _repositoryMock.SetupSequence(r => r.ObterPorIdAsync(id));
        sequence.ReturnsAsync(veiculo);
        sequence.ReturnsAsync((Veiculo?)null);
        _repositoryMock.Setup(r => r.RemoverAsync(id)).Returns(Task.CompletedTask);

        // Act
        var result1 = await _useCase.ExecutarAsync(id);
        var result2 = await _useCase.ExecutarAsync(id);

        // Assert
        result1.Should().BeTrue();
        result2.Should().BeFalse();
    }
}
