using FluentAssertions;
using GarageOS.Application.DTOs.OrdensDeServico;
using GarageOS.Application.UseCases.OrdensDeServico;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Enums;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.Repositories;
using GarageOS.Domain.ValueObjects;
using Moq;
using Xunit;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class CriarOrdemDeServicoUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock;
    private readonly Mock<IClienteRepository> _clienteRepositoryMock;
    private readonly Mock<IVeiculoRepository> _veiculoRepositoryMock;
    private readonly CriarOrdemDeServicoUseCase _useCase;

    public CriarOrdemDeServicoUseCaseTests()
    {
        _repositoryMock = new Mock<IOrdemDeServicoRepository>();
        _clienteRepositoryMock = new Mock<IClienteRepository>();
        _veiculoRepositoryMock = new Mock<IVeiculoRepository>();
        _useCase = new CriarOrdemDeServicoUseCase(
            _repositoryMock.Object,
            _clienteRepositoryMock.Object,
            _veiculoRepositoryMock.Object);
    }

    private static Endereco CriarEndereco()
    {
        return new Endereco(
            "Rua Teste",
            "123",
            "Centro",
            "São Paulo",
            "SP",
            "01234567");
    }

    private static Cliente CriarCliente()
    {
        return new Cliente(
            "João Silva",
            "00000000191",
            "joao@email.com",
            "123456789",
            CriarEndereco());
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveRetornarOrdemDeServicoResponse()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        };

        var cliente = CriarCliente();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 100000);

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId))
            .ReturnsAsync(cliente);

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(veiculoId))
            .ReturnsAsync(veiculo);

        _repositoryMock
            .Setup(r => r.ObterUltimoSequencialDoAnoAsync(It.IsAny<int>()))
            .ReturnsAsync(5);

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(request);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().NotBeEmpty();
        resultado.NumeroOS.Should().NotBeNullOrEmpty();
        resultado.Status.Should().Be(StatusOrdemDeServico.Recebida);
        resultado.ClienteId.Should().Be(clienteId);
        resultado.VeiculoId.Should().Be(veiculoId);
    }

    [Fact]
    public async Task ExecutarAsync_ComClienteInexistente_DeveLancarClienteNaoEncontradoException()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        };

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId))
            .ReturnsAsync((Cliente?)null);

        // Act
        var act = async () => await _useCase.ExecutarAsync(request);

        // Assert
        await act.Should().ThrowAsync<ClienteNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComVeiculoInexistente_DeveLancarVeiculoNaoEncontradoException()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        };

        var cliente = CriarCliente();

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId))
            .ReturnsAsync(cliente);

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(veiculoId))
            .ReturnsAsync((Veiculo?)null);

        // Act
        var act = async () => await _useCase.ExecutarAsync(request);

        // Assert
        await act.Should().ThrowAsync<VeiculoNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_DeveChamarRepositorioUmaVez()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        };

        var cliente = CriarCliente();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 100000);

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId))
            .ReturnsAsync(cliente);

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(veiculoId))
            .ReturnsAsync(veiculo);

        _repositoryMock
            .Setup(r => r.ObterUltimoSequencialDoAnoAsync(It.IsAny<int>()))
            .ReturnsAsync(10);

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(request);

        // Assert
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_DeveGerarNumeroOSComFormatoCorreto()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        };

        var cliente = CriarCliente();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 100000);

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId))
            .ReturnsAsync(cliente);

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(veiculoId))
            .ReturnsAsync(veiculo);

        _repositoryMock
            .Setup(r => r.ObterUltimoSequencialDoAnoAsync(It.IsAny<int>()))
            .ReturnsAsync(100);

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(request);

        // Assert
        resultado.NumeroOS.Should().MatchRegex(@"^OS-\d{4}-\d{5}$");
    }

    [Fact]
    public async Task ExecutarAsync_DeveIncrementarSequencialCorretamente()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        };

        var cliente = CriarCliente();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 100000);

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId))
            .ReturnsAsync(cliente);

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(veiculoId))
            .ReturnsAsync(veiculo);

        _repositoryMock
            .Setup(r => r.ObterUltimoSequencialDoAnoAsync(It.IsAny<int>()))
            .ReturnsAsync(5);

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(request);

        // Assert
        resultado.NumeroOS.Should().EndWith("00006");
    }

    [Fact]
    public async Task ExecutarAsync_DevePreencherListasVaziasNaResponse()
    {
        // Arrange
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var request = new CriarOrdemDeServicoRequest
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId
        };

        var cliente = CriarCliente();
        var veiculo = new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 100000);

        _clienteRepositoryMock
            .Setup(r => r.ObterPorIdAsync(clienteId))
            .ReturnsAsync(cliente);

        _veiculoRepositoryMock
            .Setup(r => r.ObterPorIdAsync(veiculoId))
            .ReturnsAsync(veiculo);

        _repositoryMock
            .Setup(r => r.ObterUltimoSequencialDoAnoAsync(It.IsAny<int>()))
            .ReturnsAsync(1);

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(request);

        // Assert
        resultado.Servicos.Should().NotBeNull().And.BeEmpty();
        resultado.Estoques.Should().NotBeNull().And.BeEmpty();
    }
}
