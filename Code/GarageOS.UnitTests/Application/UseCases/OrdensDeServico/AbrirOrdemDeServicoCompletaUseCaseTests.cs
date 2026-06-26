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
using EstoqueEntity = GarageOS.Domain.Entities.Estoque;

namespace GarageOS.UnitTests.Application.UseCases.OrdensDeServico;

public class AbrirOrdemDeServicoCompletaUseCaseTests
{
    private readonly Mock<IOrdemDeServicoRepository> _repositoryMock;
    private readonly Mock<IClienteRepository> _clienteRepositoryMock;
    private readonly Mock<IVeiculoRepository> _veiculoRepositoryMock;
    private readonly Mock<IServicoRepository> _servicoRepositoryMock;
    private readonly Mock<IEstoqueRepository> _estoqueRepositoryMock;
    private readonly AbrirOrdemDeServicoCompletaUseCase _useCase;

    public AbrirOrdemDeServicoCompletaUseCaseTests()
    {
        _repositoryMock = new Mock<IOrdemDeServicoRepository>();
        _clienteRepositoryMock = new Mock<IClienteRepository>();
        _veiculoRepositoryMock = new Mock<IVeiculoRepository>();
        _servicoRepositoryMock = new Mock<IServicoRepository>();
        _estoqueRepositoryMock = new Mock<IEstoqueRepository>();
        _useCase = new AbrirOrdemDeServicoCompletaUseCase(
            _repositoryMock.Object,
            _clienteRepositoryMock.Object,
            _veiculoRepositoryMock.Object,
            _servicoRepositoryMock.Object,
            _estoqueRepositoryMock.Object);
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

    private static Veiculo CriarVeiculo()
    {
        return new Veiculo("Toyota", "Corolla", "ABC1234", 2022, 100000);
    }

    private static Servico CriarServico()
    {
        return new Servico("Troca de óleo", 100m);
    }

    private static EstoqueEntity CriarEstoque()
    {
        return new EstoqueEntity("Filtro de óleo", 10, 50m, DateTime.UtcNow, "Fornecedor X");
    }

    private void ConfigurarClienteEVeiculoValidos(Guid clienteId, Guid veiculoId)
    {
        _clienteRepositoryMock.Setup(r => r.ObterPorIdAsync(clienteId)).ReturnsAsync(CriarCliente());
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync(veiculoId)).ReturnsAsync(CriarVeiculo());
    }

    private void ConfigurarSequencial(int ultimoSequencial = 0)
    {
        _repositoryMock
            .Setup(r => r.ObterUltimoSequencialDoAnoAsync(It.IsAny<int>()))
            .ReturnsAsync(ultimoSequencial);

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()))
            .Returns(Task.CompletedTask);
    }

    private static AbrirOrdemDeServicoCompletaRequest CriarRequest(
        Guid clienteId, Guid veiculoId, List<Guid> servicosIds, List<PecaRequest> pecas)
    {
        return new AbrirOrdemDeServicoCompletaRequest
        {
            ClienteId = clienteId,
            VeiculoId = veiculoId,
            ServicosIds = servicosIds,
            Pecas = pecas
        };
    }

    [Fact]
    public async Task ExecutarAsync_ComServicoEPecasVazia_DeveCriarOSComServicoVinculadoEZeroEstoques()
    {
        // Arrange (AC1)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        ConfigurarClienteEVeiculoValidos(clienteId, veiculoId);
        _servicoRepositoryMock.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(CriarServico());
        ConfigurarSequencial();

        var request = CriarRequest(clienteId, veiculoId, [servicoId], []);

        // Act
        var resultado = await _useCase.ExecutarAsync(request);

        // Assert
        resultado.Servicos.Should().HaveCount(1);
        resultado.Estoques.Should().BeEmpty();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.Is<OrdemDeServico>(os =>
            os.Servicos.Count == 1 && os.Estoques.Count == 0)), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComPecasValidas_DeveVincularPecasSemDecrementarEstoque()
    {
        // Arrange (AC2)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var estoqueId = Guid.NewGuid();
        ConfigurarClienteEVeiculoValidos(clienteId, veiculoId);
        _servicoRepositoryMock.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(CriarServico());
        var estoque = CriarEstoque();
        var quantidadeOriginal = estoque.Quantidade;
        _estoqueRepositoryMock.Setup(r => r.ObterPorIdAsync(estoqueId)).ReturnsAsync(estoque);
        ConfigurarSequencial();

        var request = CriarRequest(clienteId, veiculoId, [servicoId], [new PecaRequest { EstoqueId = estoqueId, Quantidade = 2 }]);

        // Act
        var resultado = await _useCase.ExecutarAsync(request);

        // Assert
        resultado.Estoques.Should().HaveCount(1);
        resultado.Estoques[0].Quantidade.Should().Be(2);
        estoque.Quantidade.Should().Be(quantidadeOriginal); // estoque não decrementado
    }

    [Fact]
    public async Task ExecutarAsync_ComClienteInexistente_DeveLancarClienteNaoEncontradoExceptionENaoPersistir()
    {
        // Arrange (AC5)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        _clienteRepositoryMock.Setup(r => r.ObterPorIdAsync(clienteId)).ReturnsAsync((Cliente?)null);

        var request = CriarRequest(clienteId, veiculoId, [Guid.NewGuid()], []);

        // Act
        var act = async () => await _useCase.ExecutarAsync(request);

        // Assert
        await act.Should().ThrowAsync<ClienteNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComVeiculoInexistente_DeveLancarVeiculoNaoEncontradoExceptionENaoPersistir()
    {
        // Arrange (AC6)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        _clienteRepositoryMock.Setup(r => r.ObterPorIdAsync(clienteId)).ReturnsAsync(CriarCliente());
        _veiculoRepositoryMock.Setup(r => r.ObterPorIdAsync(veiculoId)).ReturnsAsync((Veiculo?)null);

        var request = CriarRequest(clienteId, veiculoId, [Guid.NewGuid()], []);

        // Act
        var act = async () => await _useCase.ExecutarAsync(request);

        // Assert
        await act.Should().ThrowAsync<VeiculoNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComServicoInexistente_DeveLancarServicoNaoEncontradoExceptionENaoPersistir()
    {
        // Arrange (AC7)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        ConfigurarClienteEVeiculoValidos(clienteId, veiculoId);
        _servicoRepositoryMock.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync((Servico?)null);

        var request = CriarRequest(clienteId, veiculoId, [servicoId], []);

        // Act
        var act = async () => await _useCase.ExecutarAsync(request);

        // Assert
        await act.Should().ThrowAsync<ServicoNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComServicoInvalidoAposServicoValido_DeveLancarServicoNaoEncontradoExceptionENaoPersistir()
    {
        // Arrange (AC7 - edge case: id inválido depois de um id válido na lista)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var servicoValidoId = Guid.NewGuid();
        var servicoInvalidoId = Guid.NewGuid();
        ConfigurarClienteEVeiculoValidos(clienteId, veiculoId);
        _servicoRepositoryMock.Setup(r => r.ObterPorIdAsync(servicoValidoId)).ReturnsAsync(CriarServico());
        _servicoRepositoryMock.Setup(r => r.ObterPorIdAsync(servicoInvalidoId)).ReturnsAsync((Servico?)null);

        var request = CriarRequest(clienteId, veiculoId, [servicoValidoId, servicoInvalidoId], []);

        // Act
        var act = async () => await _useCase.ExecutarAsync(request);

        // Assert
        await act.Should().ThrowAsync<ServicoNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComEstoqueInexistente_DeveLancarEstoqueNaoEncontradoExceptionENaoPersistir()
    {
        // Arrange (AC8)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var estoqueId = Guid.NewGuid();
        ConfigurarClienteEVeiculoValidos(clienteId, veiculoId);
        _servicoRepositoryMock.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(CriarServico());
        _estoqueRepositoryMock.Setup(r => r.ObterPorIdAsync(estoqueId)).ReturnsAsync((EstoqueEntity?)null);

        var request = CriarRequest(clienteId, veiculoId, [servicoId], [new PecaRequest { EstoqueId = estoqueId, Quantidade = 1 }]);

        // Act
        var act = async () => await _useCase.ExecutarAsync(request);

        // Assert
        await act.Should().ThrowAsync<EstoqueNaoEncontradoException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComServicosIdsDuplicados_DeveCriarUmItemDeServicoParaCadaOcorrencia()
    {
        // Arrange (AC1/AC2 edge case - duplicados preservados como itens separados)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        ConfigurarClienteEVeiculoValidos(clienteId, veiculoId);
        _servicoRepositoryMock.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(CriarServico());
        ConfigurarSequencial();

        var request = CriarRequest(clienteId, veiculoId, [servicoId, servicoId], []);

        // Act
        var resultado = await _useCase.ExecutarAsync(request);

        // Assert
        resultado.Servicos.Should().HaveCount(2);
        _repositoryMock.Verify(r => r.AdicionarAsync(It.Is<OrdemDeServico>(os => os.Servicos.Count == 2)), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComEstoqueIdsDuplicados_DeveCriarUmItemDeEstoqueParaCadaOcorrencia()
    {
        // Arrange (edge case - peças duplicadas com quantidades diferentes, sem somar)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        var estoqueId = Guid.NewGuid();
        ConfigurarClienteEVeiculoValidos(clienteId, veiculoId);
        _servicoRepositoryMock.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(CriarServico());
        _estoqueRepositoryMock.Setup(r => r.ObterPorIdAsync(estoqueId)).ReturnsAsync(CriarEstoque());
        ConfigurarSequencial();

        var request = CriarRequest(clienteId, veiculoId, [servicoId],
        [
            new PecaRequest { EstoqueId = estoqueId, Quantidade = 1 },
            new PecaRequest { EstoqueId = estoqueId, Quantidade = 3 }
        ]);

        // Act
        var resultado = await _useCase.ExecutarAsync(request);

        // Assert
        resultado.Estoques.Should().HaveCount(2);
        resultado.Estoques.Select(e => e.Quantidade).Should().BeEquivalentTo([1, 3]);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveGerarNumeroOSComFormatoCorreto()
    {
        // Arrange (AC9)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        ConfigurarClienteEVeiculoValidos(clienteId, veiculoId);
        _servicoRepositoryMock.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(CriarServico());
        ConfigurarSequencial(100);

        var request = CriarRequest(clienteId, veiculoId, [servicoId], []);

        // Act
        var resultado = await _useCase.ExecutarAsync(request);

        // Assert
        resultado.NumeroOS.Should().MatchRegex(@"^OS-\d{4}-\d{5}$");
        resultado.NumeroOS.Should().EndWith("00101");
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveChamarRepositorioUmaVez()
    {
        // Arrange (atomicidade - persiste exatamente uma vez no caminho de sucesso)
        var clienteId = Guid.NewGuid();
        var veiculoId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        ConfigurarClienteEVeiculoValidos(clienteId, veiculoId);
        _servicoRepositoryMock.Setup(r => r.ObterPorIdAsync(servicoId)).ReturnsAsync(CriarServico());
        ConfigurarSequencial();

        var request = CriarRequest(clienteId, veiculoId, [servicoId], []);

        // Act
        await _useCase.ExecutarAsync(request);

        // Assert
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<OrdemDeServico>()), Times.Once);
    }
}
