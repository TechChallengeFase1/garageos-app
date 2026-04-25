# Plano de Testes de Integração - GarageOS

## 1. Contexto e Motivação

Atualmente, o projeto GarageOS possui **260 testes unitários** (com mocks) que cobrem aproximadamente **60-70%** do código. Para atingir a meta de **80%+ de cobertura** no SonarQube, é necessário adicionar testes de integração que:

1. **Executem repositórios reais** contra PostgreSQL (atualmente mockados)
2. **Validem fluxos completos end-to-end** do negócio
3. **Testem interações entre múltiplas camadas** (Application → Infrastructure → Database)
4. **Identifiquem bugs de integração** que testes com mocks não conseguem detectar

### Cobertura esperada
- Testes unitários: ~70% (lógica de domínio + use cases)
- **Testes de integração: +15-20%** (código Repository + DbContext + mapping)
- **Total esperado: 85-90%** após implementação

---

## 2. Arquitetura dos Testes de Integração

### 2.1 Setup do banco de dados para testes

Criar arquivo: `GarageOS.IntegrationTests/Fixtures/DatabaseFixture.cs`

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    private readonly IHost _host;
    private GarageOSDbContext _dbContext;
    
    public GarageOSDbContext DbContext => _dbContext;
    
    public async Task InitializeAsync()
    {
        // 1. Criar banco de testes isolado (ex: garageos_test_123)
        // 2. Aplicar migrations
        // 3. Semear dados iniciais se necessário
        // 4. Disponibilizar DbContext limpo para cada teste
    }
    
    public async Task DisposeAsync()
    {
        // 1. Limpar dados de teste
        // 2. Desconectar do banco
        // 3. Deletar banco de testes (opcional)
    }
    
    public async Task ResetDatabaseAsync()
    {
        // Limpar todas as tabelas entre testes (transaction rollback)
    }
}
```

### 2.2 Padrão de teste com collection fixture

```csharp
[Collection("Database collection")]
public class XxxIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private readonly GarageOSDbContext _dbContext;
    private readonly IClienteRepository _repository;
    
    public XxxIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _dbContext = new GarageOSDbContext(_fixture.DbContextOptions);
        _repository = new ClienteRepository(_dbContext);
    }
    
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }
    
    public Task DisposeAsync() => Task.CompletedTask;
    
    [Fact]
    public async Task Fluxo_Complete_DeveRetornarSucesso()
    {
        // Arrange: usar repositório real
        var cliente = new Cliente("João", "00000000191", "joao@test.com", "11999999999", endereco);
        
        // Act: executar operação contra BD real
        await _repository.AdicionarAsync(cliente);
        var resultado = await _repository.ObterPorIdAsync(cliente.Id);
        
        // Assert
        resultado.Should().NotBeNull();
        resultado.Id.Should().Be(cliente.Id);
    }
}
```

---

## 3. Fluxos Críticos a Testar

### 3.1 **Fluxo 1: Cadastrar Cliente Completo** ⭐⭐⭐ (Crítico)

**Arquivo:** `GarageOS.IntegrationTests/Application/UseCases/Clientes/CadastrarClienteIntegrationTests.cs`

**Cenários:**
```
1. Cadastrar novo cliente com dados válidos
   ✓ Cliente é persistido no BD
   ✓ Documento é validado via Documento VO
   ✓ Email é único por repository check
   ✓ Telefone é único por repository check
   ✓ Response mapeia corretamente todos os campos

2. Erro: tentar cadastrar com documento duplicado
   ✓ Lança ClienteJaCadastradoException
   ✓ Banco não é alterado (transação rollback)

3. Erro: tentar cadastrar com email duplicado
   ✓ Lança ClienteJaCadastradoException
   ✓ Cliente anterior permanece intacto

4. Cadastrar cliente A, depois cliente B com mesmo telefone
   ✓ Ambos persistem
   ✓ Telefone duplicado é permitido? (valide regra de negócio)
   
5. Endereço é persistido como value object
   ✓ Endereço_Logradouro = "Rua Teste"
   ✓ Endereço_Numero = "123"
   ✓ Endereço_CEP = "01234567"
```

**Impacto na cobertura:**
- ClienteRepository: +5% (AdicionarAsync, ObterPorDocumentoAsync, ObterPorEmailAsync, ObterPorTelefoneAsync)
- DbContext: +3% (SaveChanges, change tracking)
- CadastrarClienteUseCase: +2% (mapper + validações do repositório)
- **Total: ~10%**

---

### 3.2 **Fluxo 2: Criar Ordem de Serviço com Cliente e Veículo** ⭐⭐⭐ (Crítico)

**Arquivo:** `GarageOS.IntegrationTests/Application/UseCases/OrdensDeServico/CriarOrdemDeServicoIntegrationTests.cs`

**Cenários:**
```
1. Criar OS com cliente e veículo válidos
   ✓ OS é persistida com NumeroOS = "OS-2026-00001"
   ✓ Sequencial é incrementado no BD
   ✓ Segunda OS do ano gera "OS-2026-00002"
   ✓ Status inicial = Aberta (ou equivalente)

2. Erro: cliente não encontrado
   ✓ Lança ClienteNaoEncontradoException
   ✓ OS não é criada

3. Erro: veículo não encontrado
   ✓ Lança VeiculoNaoEncontradoException
   ✓ OS não é criada

4. Criar OS em anos diferentes
   ✓ 2026: OS-2026-00001
   ✓ 2027: OS-2027-00001 (sequencial reinicia)
   
5. Response mapeia corretamente
   ✓ Inclui cliente e veículo relacionados
   ✓ Coleções Servicos e Estoques iniciam vazias
```

**Impacto na cobertura:**
- OrdemDeServicoRepository: +6% (ObterUltimoSequencialDoAnoAsync, AdicionarAsync, queries)
- ClienteRepository + VeiculoRepository: +2% (validações de existência)
- CriarOrdemDeServicoUseCase: +3% (mapper + BrasiliaTime.Agora)
- DbContext: +2% (relacionamentos)
- **Total: ~13%**

---

### 3.3 **Fluxo 3: Adicionar Estoque em Ordem de Serviço** ⭐⭐ (Importante)

**Arquivo:** `GarageOS.IntegrationTests/Application/UseCases/OrdensDeServico/AdicionarEstoqueNaOSIntegrationTests.cs`

**Cenários:**
```
1. Adicionar estoque a uma OS existente
   ✓ OrdemDeServicoEstoque é criado com quantidade
   ✓ Relacionamento é persistido
   ✓ Estoque.Quantidade não é alterado (apenas referência)

2. Erro: OS não encontrada
   ✓ Lança OrdemDeServicoNaoEncontradaException

3. Erro: Estoque não encontrado
   ✓ Lança EstoqueNaoEncontradoException

4. Adicionar múltiplos estoques à mesma OS
   ✓ Todos persistem
   ✓ Coleção Estoques retorna todos os itens

5. Response inclui itens da OS
   ✓ EstoqueId está correto
   ✓ EstoqueNome é carregado via relationship (Estoque?.Nome)
```

**Impacto na cobertura:**
- EstoqueRepository: +4% (ObterPorIdAsync)
- OrdemDeServicoRepository: +3% (AdicionarEstoque + AtualizarAsync com collections)
- AdicionarEstoqueNaOSUseCase: +2% (mapper)
- **Total: ~9%**

---

### 3.4 **Fluxo 4: Vincular Veículo a Cliente** ⭐⭐ (Importante)

**Arquivo:** `GarageOS.IntegrationTests/Application/UseCases/Veiculos/VincularVeiculoClienteIntegrationTests.cs`

**Cenários:**
```
1. Vincular veículo existente a cliente existente
   ✓ Veiculo.ClienteId é atualizado
   ✓ Mudança é persistida no BD
   ✓ Relationship é estabelecido

2. Erro: veículo não encontrado
   ✓ Lança VeiculoNaoEncontradoException

3. Erro: cliente não encontrado
   ✓ Lança ClienteNaoEncontradoException

4. Vincular e depois remover vínculo
   ✓ ClienteId = null é válido
   ✓ Persistir resultado nulo
```

**Impacto na cobertura:**
- VeiculoRepository: +4% (ObterPorIdAsync, AtualizarAsync)
- ClienteRepository: +2% (validação de existência)
- VincularVeiculoClienteUseCase: +2%
- **Total: ~8%**

---

### 3.5 **Fluxo 5: Deletar Estoque** ⭐⭐ (Importante)

**Arquivo:** `GarageOS.IntegrationTests/Application/UseCases/Estoque/DeletarEstoqueIntegrationTests.cs`

**Cenários:**
```
1. Deletar estoque existente
   ✓ Estoque é removido do BD
   ✓ ObterPorIdAsync retorna null após deleção

2. Tentar deletar estoque não encontrado
   ✓ Não lança exceção
   ✓ Retorna false (conforme teste unitário)

3. Deletar estoque e validar integridade referencial
   ✓ Caso exista OS com esse estoque: validar cascata
   ✓ Ou: validar que não permite deletar se vinculado
```

**Impacto na cobertura:**
- EstoqueRepository: +5% (RemoverAsync, queries)
- DeletarEstoqueUseCase: +2%
- **Total: ~7%**

---

### 3.6 **Fluxo 6: Atualizar Cliente (Soft Delete via Desativar)** ⭐⭐ (Importante)

**Arquivo:** `GarageOS.IntegrationTests/Application/UseCases/Clientes/AlterarClienteIntegrationTests.cs`

**Cenários:**
```
1. Alterar dados do cliente
   ✓ Nome, Email, Telefone são atualizados
   ✓ Endereço é atualizado como value object
   ✓ AtualizadoEm é atualizado
   ✓ Changes são persistidos

2. Desativar cliente (soft delete)
   ✓ Ativo = false
   ✓ Cliente permanece no BD
   ✓ ObterPorIdAsync ainda retorna o cliente
   ✓ ListarTodosAsync não filtra por Ativo (valide)

3. Tentar alterar email para duplicado
   ✓ Lança ClienteJaCadastradoException
   ✓ Ignora seu próprio email (same-client check)

4. Reativar cliente
   ✓ Ativo = true
   ✓ Validar que não lança exceção se já ativo
```

**Impacto na cobertura:**
- ClienteRepository: +3% (AtualizarAsync patterns)
- AlterarClienteUseCase: +3%
- Cliente entity (Desativar, Ativar): +2%
- **Total: ~8%**

---

### 3.7 **Fluxo 7: Listar com Pagination e Filtering** ⭐ (Complementar)

**Arquivo:** `GarageOS.IntegrationTests/Application/UseCases/Clientes/ListarClientesIntegrationTests.cs`

**Cenários:**
```
1. Listar 100 clientes com pagination
   ✓ Primeira página retorna 10 itens
   ✓ Segunda página começa no índice correto
   ✓ TotalCount é acurado

2. Filtrar por nome (case-insensitive)
   ✓ Busca "joao" retorna "João Silva"

3. Ordenar por CriadoEm
   ✓ Ordenação funciona corretamente no BD
```

**Impacto na cobertura:**
- ClienteRepository: +2%
- ListarClientesUseCase: +2%
- **Total: ~4%**

---

## 4. Estrutura de Pastas Proposta

```
GarageOS.IntegrationTests/
├── Fixtures/
│   ├── DatabaseFixture.cs           # Setup/Teardown do BD de teste
│   └── TestDataBuilder.cs           # Builders para criar dados de teste
├── Application/
│   ├── UseCases/
│   │   ├── Clientes/
│   │   │   ├── CadastrarClienteIntegrationTests.cs
│   │   │   ├── AlterarClienteIntegrationTests.cs
│   │   │   └── ListarClientesIntegrationTests.cs
│   │   ├── OrdensDeServico/
│   │   │   ├── CriarOrdemDeServicoIntegrationTests.cs
│   │   │   ├── AdicionarEstoqueNaOSIntegrationTests.cs
│   │   │   └── AdicionarServicoNaOSIntegrationTests.cs
│   │   ├── Veiculos/
│   │   │   ├── VincularVeiculoClienteIntegrationTests.cs
│   │   │   └── DeletarVeiculoIntegrationTests.cs
│   │   └── Estoque/
│   │       ├── CadastrarEstoqueIntegrationTests.cs
│   │       └── DeletarEstoqueIntegrationTests.cs
│   └── Validators/ (opcional - validadores geralmente já têm boa cobertura)
└── Infrastructure/
    └── Repositories/
        ├── ClienteRepositoryIntegrationTests.cs
        ├── VeiculoRepositoryIntegrationTests.cs
        └── EstoqueRepositoryIntegrationTests.cs
```

---

## 5. Padrão de Implementação

### 5.1 Fixture (DatabaseFixture.cs)

```csharp
using GarageOS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

[CollectionDefinition("Database collection")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    // Marker class - não tem lógica
}

public class DatabaseFixture : IAsyncLifetime
{
    private readonly DbContextOptions<GarageOSDbContext> _dbContextOptions;
    private GarageOSDbContext _dbContext = null!;
    
    public DatabaseFixture()
    {
        // Usar banco de testes em container Docker ou arquivo (SQLite para testes)
        _dbContextOptions = new DbContextOptionsBuilder<GarageOSDbContext>()
            .UseNpgsql(GetTestConnectionString())
            .Options;
    }
    
    public async Task InitializeAsync()
    {
        _dbContext = new GarageOSDbContext(_dbContextOptions);
        
        // Aplicar migrations
        await _dbContext.Database.MigrateAsync();
        
        // Limpar dados anteriores
        await ResetDatabaseAsync();
    }
    
    public async Task DisposeAsync()
    {
        await ResetDatabaseAsync();
        await _dbContext.DisposeAsync();
    }
    
    public async Task ResetDatabaseAsync()
    {
        // Truncate todas as tabelas
        var tables = new[]
        {
            "\"OrdemDeServicoEstoques\"",
            "\"OrdemDeServicoServicos\"",
            "\"OrdensDeServico\"",
            "\"Orcamentos\"",
            "\"Estoques\"",
            "\"Veiculos\"",
            "\"Clientes\"",
            "\"Servicos\""
        };
        
        foreach (var table in tables)
        {
            await _dbContext.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE {table} CASCADE");
        }
    }
    
    public DbContext CreateContext()
    {
        return new GarageOSDbContext(_dbContextOptions);
    }
    
    private string GetTestConnectionString()
    {
        return "Host=localhost;Database=garageos_test;Username=postgres;Password=password";
        // Ou usar testcontainers para Docker
    }
}
```

### 5.2 Teste de Use Case

```csharp
using FluentAssertions;
using GarageOS.Application.DTOs.Clientes;
using GarageOS.Application.UseCases.Clientes;
using GarageOS.Domain.Entities;
using GarageOS.Domain.Exceptions;
using GarageOS.Domain.ValueObjects;
using GarageOS.Infrastructure.Data;
using GarageOS.Infrastructure.Repositories;
using Xunit;

[Collection("Database collection")]
public class CadastrarClienteIntegrationTests
{
    private readonly DatabaseFixture _fixture;
    private readonly GarageOSDbContext _dbContext;
    private readonly ClienteRepository _repository;
    private readonly CadastrarClienteUseCase _useCase;
    
    public CadastrarClienteIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _dbContext = new GarageOSDbContext(fixture.CreateContext().Model);
        _repository = new ClienteRepository(_dbContext);
        _useCase = new CadastrarClienteUseCase(_repository);
    }
    
    [Fact]
    public async Task ExecutarAsync_ComDadosValidos_DevePersistirNoeBD()
    {
        // Arrange
        var request = new CriarClienteRequest
        {
            Nome = "João Silva",
            Documento = "00000000191",
            Email = "joao@test.com",
            Telefone = "11999999999",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01234567"
        };
        
        // Act
        var response = await _useCase.ExecutarAsync(request);
        
        // Assert
        response.Should().NotBeNull();
        response.Id.Should().NotBeEmpty();
        response.Nome.Should().Be("João Silva");
        response.Email.Should().Be("joao@test.com");
        
        // Verificar persistência no BD
        var clienteBD = await _repository.ObterPorIdAsync(response.Id);
        clienteBD.Should().NotBeNull();
        clienteBD!.Nome.Should().Be("João Silva");
        clienteBD.Endereco.Logradouro.Should().Be("Rua Teste");
    }
    
    [Fact]
    public async Task ExecutarAsync_ComDocumentoDuplicado_DeveLancarExcecao()
    {
        // Arrange: criar cliente 1
        var cliente1 = CriarClientePadrao();
        await _repository.AdicionarAsync(cliente1);
        
        var request = new CriarClienteRequest
        {
            Nome = "Outro Cliente",
            Documento = "00000000191", // Mesmo do cliente1
            Email = "outro@test.com",
            Telefone = "11888888888",
            Logradouro = "Rua Teste",
            Numero = "123",
            Bairro = "Centro",
            Cidade = "São Paulo",
            Estado = "SP",
            Cep = "01234567"
        };
        
        // Act & Assert
        await Assert.ThrowsAsync<ClienteJaCadastradoException>(
            () => _useCase.ExecutarAsync(request));
        
        // Garantir que não foi criado
        var clientes = await _repository.ListarTodosAsync();
        clientes.Should().HaveCount(1);
    }
    
    private static Cliente CriarClientePadrao()
    {
        var endereco = new Endereco("Rua A", "1", "Bairro", "SP", "SP", "01234567");
        return new Cliente("Cliente A", "00000000191", "a@test.com", "11999999999", endereco);
    }
}
```

---

## 6. Configuração do appsettings para Testes

Criar arquivo: `GarageOS.IntegrationTests/appsettings.integrationtest.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=garageos_test;Username=postgres;Password=password;Pooling=false;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Error"
    }
  }
}
```

---

## 7. Impacto Total na Cobertura

| Fluxo | Cenários | Estimativa de Cobertura |
|-------|----------|------------------------|
| 1. Cadastrar Cliente | 5 | +10% |
| 2. Criar Ordem de Serviço | 5 | +13% |
| 3. Adicionar Estoque na OS | 5 | +9% |
| 4. Vincular Veículo a Cliente | 4 | +8% |
| 5. Deletar Estoque | 3 | +7% |
| 6. Alterar Cliente | 4 | +8% |
| 7. Listar Clientes | 3 | +4% |
| **TOTAL** | **29** | **+59%** |

Cobertura estimada após implementação:
- Atual (unitários): **70%**
- **+ Integração: +10-15%** (conservador)
- **= Final: 80-85%** ✅

---

## 8. Roadmap de Implementação

### Fase 1: Setup (1-2 dias)
- [ ] Criar projeto `GarageOS.IntegrationTests`
- [ ] Implementar `DatabaseFixture.cs`
- [ ] Configurar `appsettings.integrationtest.json`
- [ ] Setup de banco PostgreSQL para testes (Docker Compose recomendado)

### Fase 2: Fluxos Críticos (3-4 dias)
- [ ] CadastrarClienteIntegrationTests.cs (10% cobertura)
- [ ] CriarOrdemDeServicoIntegrationTests.cs (13% cobertura)
- [ ] AdicionarEstoqueNaOSIntegrationTests.cs (9% cobertura)

### Fase 3: Fluxos Complementares (2-3 dias)
- [ ] VincularVeiculoClienteIntegrationTests.cs (8% cobertura)
- [ ] DeletarEstoqueIntegrationTests.cs (7% cobertura)
- [ ] AlterarClienteIntegrationTests.cs (8% cobertura)

### Fase 4: Validação (1 dia)
- [ ] ListarClientesIntegrationTests.cs (4% cobertura)
- [ ] Rodar SonarQube e validar cobertura ≥80%
- [ ] Documentar resultados

---

## 9. Alternativas de Setup de Banco para Testes

### Opção A: TestContainers (Recomendado)
```csharp
// Cria container Docker automaticamente
var container = new PostgreSqlTestcontainer(...)
    .Start()
    .Result;
var connectionString = container.ConnectionString;
```
✅ Isolado | ✅ Real | ✅ Cleanup automático | ❌ Mais lento

### Opção B: PostgreSQL Local em Container
```bash
docker run --name garageos-test -e POSTGRES_PASSWORD=password -e POSTGRES_DB=garageos_test -p 5433:5432 -d postgres:latest
```
✅ Rápido | ❌ Manual cleanup | ❌ Necessita setup manual

### Opção C: SQLite para Testes (Não recomendado)
```
var dbContextOptions = new DbContextOptionsBuilder<GarageOSDbContext>()
    .UseSqlite("Data Source=:memory:")
    .Options;
```
✅ Muito rápido | ❌ Diferente de PostgreSQL | ❌ Pode não detectar bugs específicos do Postgres

**Recomendação:** Use TestContainers (Opção A) para máxima confiabilidade.

---

## 10. Próximos Passos

1. **Criar projeto** `GarageOS.IntegrationTests`
2. **Implementar DatabaseFixture.cs** com setup PostgreSQL
3. **Implementar CadastrarClienteIntegrationTests.cs** como piloto
4. **Rodar testes** e validar que conseguem conectar ao BD
5. **Expandir** para outros fluxos críticos
6. **Validar cobertura** no SonarQube (target: 80%+)

---

## Observações Importantes

### DateTime e Timezone
- Use `BrasiliaTime.Agora` que já retorna `DateTime.UtcNow`
- PostgreSQL `timestamp with time zone` requer `DateTime.Kind = Utc`
- ✅ Já corrigido em versão anterior

### Relacionamentos e Lazy Loading
- Cuidado com `N+1 queries` ao mapear responses
- Usar `.AsNoTracking()` onde apropriado
- Validar uso de `Include()` para eager loading

### Transações e Isolation
- Cada teste deve deixar o BD limpo após execução
- Usar `TRUNCATE TABLE ... CASCADE` para reset rápido
- Considerar nested transactions para testes paralelos

### Dados de Teste
- Criar `TestDataBuilder.cs` para builders fluentes
- Evitar hardcoding de IDs
- Reutilizar builders entre testes

