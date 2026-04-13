# 🎯 Camada de Aplicação (Application)

## Propósito
Orquestra as operações do sistema. Traduz requisições externas em operações do domínio, coordena as regras de negócio e persiste as mudanças.

## Dependências
- ✅ Depende APENAS da Camada de Domínio
- ❌ Nunca depende de Infrastructure ou API
- ❌ Nunca contém lógica de banco de dados ou HTTP

## O que deve conter

### 📁 UseCases (Casos de Uso)
Implementação dos casos de uso do sistema.

```csharp
// Exemplo:
public class CadastrarVeiculoUseCase
{
    private readonly IVeiculoRepository _veiculoRepository;
    
    public CadastrarVeiculoUseCase(IVeiculoRepository veiculoRepository)
    {
        _veiculoRepository = veiculoRepository;
    }
    
    public async Task<Guid> ExecutarAsync(CadastrarVeiculoRequest request)
    {
        var veiculo = new Veiculo(request.Placa, request.Marca);
        await _veiculoRepository.AdicionarAsync(veiculo);
        return veiculo.Id;
    }
}
```

### 📁 Services (Serviços de Aplicação)
Coordenam a lógica de múltiplas entidades ou casos de uso.

```csharp
// Exemplo:
public interface IGerenciarOrdenService
{
    Task CriarOrdemAsync(Guid veiculoId, List<Guid> servicoIds);
}
```

### 📁 DTOs (Data Transfer Objects)
Objetos para trafegar dados entre camadas.

```csharp
// Exemplo:
public class CadastrarVeiculoRequest
{
    public string Placa { get; set; }
    public string Marca { get; set; }
}

public class VeiculoResponse
{
    public Guid Id { get; set; }
    public string Placa { get; set; }
    public string Marca { get; set; }
}
```

### 📁 Validators (Validadores)
Validação de dados de entrada.

```csharp
// Exemplo:
public class CadastrarVeiculoValidator : AbstractValidator<CadastrarVeiculoRequest>
{
    public CadastrarVeiculoValidator()
    {
        RuleFor(x => x.Placa).NotEmpty().Length(7);
        RuleFor(x => x.Marca).NotEmpty();
    }
}
```

## Regra de Ouro 🏆
Esta camada é o **maestro da orquestração**. Recebe dados, valida, chama o domínio, e coordena o resultado - mas NUNCA implementa regras de negócio complexas diretamente.
