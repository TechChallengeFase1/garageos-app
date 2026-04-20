using FluentAssertions;
using GarageOS.Application.DTOs.Servicos;
using GarageOS.Application.UseCases.Servicos;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Repositories;
using Moq;

namespace GarageOS.UnitTests.Application.UseCases;

public class CadastrarServicoUseCaseTests
{
    private readonly Mock<IServicoRepository> _repositoryMock;
    private readonly CadastrarServicoUseCase _useCase;

    public CadastrarServicoUseCaseTests()
    {
        _repositoryMock = new Mock<IServicoRepository>();
        _useCase = new CadastrarServicoUseCase(_repositoryMock.Object);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveRetornarServicoResponse()
    {
        // Arrange
        var request = new CriarServicoRequest
        {
            NomeServico = "Troca de óleo",
            Preco = 150.00m
        };

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Servico>()))
            .Returns(Task.CompletedTask);

        // Act
        var resultado = await _useCase.ExecutarAsync(request);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().NotBeEmpty();
        resultado.NomeServico.Should().Be(request.NomeServico);
        resultado.Preco.Should().Be(request.Preco);
    }

    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveChamarRepositorioUmaVez()
    {
        // Arrange
        var request = new CriarServicoRequest { NomeServico = "Revisão completa", Preco = 500m };

        _repositoryMock
            .Setup(r => r.AdicionarAsync(It.IsAny<Servico>()))
            .Returns(Task.CompletedTask);

        // Act
        await _useCase.ExecutarAsync(request);

        // Assert
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Servico>()), Times.Once);
    }

    [Fact]
    public async Task ExecutarAsync_ComNomeVazio_DeveLancarArgumentException()
    {
        // Arrange
        var request = new CriarServicoRequest { NomeServico = "", Preco = 150m };

        // Act
        var act = async () => await _useCase.ExecutarAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Servico>()), Times.Never);
    }

    [Fact]
    public async Task ExecutarAsync_ComPrecoNegativo_DeveLancarArgumentException()
    {
        // Arrange
        var request = new CriarServicoRequest { NomeServico = "Revisão", Preco = -10m };

        // Act
        var act = async () => await _useCase.ExecutarAsync(request);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _repositoryMock.Verify(r => r.AdicionarAsync(It.IsAny<Servico>()), Times.Never);
    }
}
