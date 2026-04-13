# 🧪 GarageOS.UnitTests

## Propósito
Testes unitários do **Domain** e **Application** layers - regras de negócio puras, sem I/O externo, rápidos de executar.

## 📦 Pacotes Instalados
- **xUnit** - Framework de testes
- **Moq** - Mocking de interfaces (repositórios, serviços)
- **FluentAssertions** - Assertions mais legíveis
- **Bogus** - Geração de dados falsos para fixtures

## 📁 Estrutura

```
GarageOS.UnitTests/
├── Domain/
│   ├── Entities/          ← Testes das Entidades do domínio
│   ├── ValueObjects/      ← Testes dos Objetos de Valor
│   └── Aggregates/        ← Testes dos Agregados
└── Application/
    ├── UseCases/          ← Testes dos casos de uso
    └── Validators/        ← Testes dos validadores
```

## ✍️ Exemplo: Teste de Entity

```csharp
// GarageOS.UnitTests/Domain/Entities/VeiculoTests.cs
namespace GarageOS.UnitTests.Domain.Entities;

public class VeiculoTests
{
    [Fact]
    public void CriarVeiculo_ComDadosValidos_DeveRetornarVeiculoComId()
    {
        // Arrange
        var placa = "ABC1234";
        var marca = "Toyota";
        
        // Act
        var veiculo = new Veiculo(placa, marca);
        
        // Assert
        veiculo.Id.Should().NotBeEmpty();
        veiculo.Placa.Should().Be(placa);
        veiculo.Marca.Should().Be(marca);
    }
    
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void CriarVeiculo_ComPlacaInvalida_DeveLancarExcecao(string placaInvalida)
    {
        // Act & Assert
        var act = () => new Veiculo(placaInvalida, "Toyota");
        act.Should().Throw<DomainException>().WithMessage("*placa*");
    }
}
```

## ✍️ Exemplo: Teste de Use Case com Mock

```csharp
// GarageOS.UnitTests/Application/UseCases/CadastrarVeiculoUseCaseTests.cs
namespace GarageOS.UnitTests.Application.UseCases;

public class CadastrarVeiculoUseCaseTests
{
    private readonly Mock<IVeiculoRepository> _mockRepository;
    private readonly CadastrarVeiculoUseCase _useCase;
    
    public CadastrarVeiculoUseCaseTests()
    {
        _mockRepository = new Mock<IVeiculoRepository>();
        _useCase = new CadastrarVeiculoUseCase(_mockRepository.Object);
    }
    
    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DeveAdicionarVeiculoERetornarId()
    {
        // Arrange
        var request = new CadastrarVeiculoRequest { Placa = "ABC1234", Marca = "Toyota" };
        
        // Act
        var resultado = await _useCase.ExecutarAsync(request);
        
        // Assert
        resultado.Should().NotBeEmpty();
        _mockRepository.Verify(r => r.AdicionarAsync(It.IsAny<Veiculo>()), Times.Once);
    }
    
    [Fact]
    public async Task ExecutarAsync_ComPlacaDuplicada_DeveThrow()
    {
        // Arrange
        var request = new CadastrarVeiculoRequest { Placa = "ABC1234", Marca = "Toyota" };
        _mockRepository.Setup(r => r.ExistePorPlacaAsync(request.Placa))
            .ReturnsAsync(true);
        
        // Act & Assert
        await _useCase.ExecutarAsync(request).Should().ThrowAsync<DomainException>();
    }
}
```

## ✍️ Exemplo: Usando Bogus para Fixtures

```csharp
// Gerar dados aleatórios para testes
var faker = new Faker();
var placa = faker.Random.AlphaNumeric(7).ToUpper();
var marca = faker.PickRandom(new[] { "Toyota", "Honda", "Ford" });
```

## 🏃 Executando os Testes

```bash
# Rodar todos os testes unitários
dotnet test GarageOS.UnitTests

# Rodar testes de uma classe específica
dotnet test GarageOS.UnitTests --filter "ClassName=VeiculoTests"

# Com output detalhado
dotnet test GarageOS.UnitTests --logger "console;verbosity=detailed"

# Modo watch (re-executa ao salvar)
dotnet watch test GarageOS.UnitTests
```

## ✅ Boas Práticas

1. **Isolamento**: Cada teste não depende de outros
2. **Nomenclatura**: `Metodo_Cenario_ResultadoEsperado`
3. **AAA Pattern**: Arrange → Act → Assert
4. **Mocking**: Mock apenas dependências externas (repositórios, serviços)
5. **Dados**: Use Bogus para dados aleatórios, não hardcode
6. **Rápido**: Testes unitários devem rodar em < 1s
7. **Determinístico**: Mesmo resultado sempre

## 📊 Cobertura de Testes

Para gerar relatório de cobertura:

```bash
dotnet test GarageOS.UnitTests /p:CollectCoverage=true /p:CoverageFormat=opencover
```

Depois use ferramentas como ReportGenerator para visualizar.
