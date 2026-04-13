# 🎯 Camada de Domínio (Domain)

## Propósito
Esta é a camada mais interna e o coração do seu software. Contém **APENAS regras de negócio puras**, sem nenhuma dependência de frameworks externos ou outros projetos da solução.

## Dependências
- ❌ Nenhuma dependência de outras camadas
- ❌ Nenhuma dependência de frameworks (Entity Framework, etc)
- ✅ Apenas .NET base

## O que deve conter

### 📁 Entities (Entidades)
Objetos que representam conceitos do domínio com identidade única.

```csharp
// Exemplo:
public class Veiculo
{
    public Guid Id { get; set; }
    public string Placa { get; set; }
    public string Marca { get; set; }
    // ... propriedades do negócio
}
```

### 📁 ValueObjects (Objetos de Valor)
Objetos que representam valores específicos sem identidade própria.

```csharp
// Exemplo:
public class Endereco
{
    public string Rua { get; set; }
    public string Numero { get; set; }
    public string Cidade { get; set; }
}
```

### 📁 Aggregates (Agregados)
Agrupamentos de entidades e value objects que formam uma unidade de coesão.

```csharp
// Exemplo:
public class OrdemServico
{
    public Guid Id { get; set; }
    public Veiculo Veiculo { get; set; }
    public List<Servico> Servicos { get; set; }
}
```

### 📁 Repositories (Interfaces de Repositório)
Contratos/interfaces que definem como os dados serão persistidos.

```csharp
// Exemplo:
public interface IVeiculoRepository
{
    Task<Veiculo> ObterPorIdAsync(Guid id);
    Task AdicionarAsync(Veiculo veiculo);
    Task AtualizarAsync(Veiculo veiculo);
}
```

### 📁 Exceptions (Exceções do Domínio)
Exceções específicas de regras de negócio.

```csharp
// Exemplo:
public class VeiculoNaoEncontradoException : Exception
{
    public VeiculoNaoEncontradoException(string mensagem) : base(mensagem) { }
}
```

## Regra de Ouro 🏆
Se você precisar mudar o banco de dados de SQL Server para MongoDB, essa camada **NÃO DEVE SOFRER UMA ÚNICA ALTERAÇÃO**.
