# 🎯 Camada de Infraestrutura (Infrastructure)

## Propósito
Implementa os contratos definidos no Domínio. Responsável por conversar com o mundo externo: banco de dados, APIs, emails, sistemas de arquivo, etc.

## Dependências
- ✅ Depende de Domain e Application
- ✅ Aqui ficam as implementações concretas de interfaces
- ✅ Usa frameworks como Entity Framework Core, bibliotecas de email, etc

## O que deve conter

### 📁 Data (Banco de Dados)
Contexto EF Core e configurações de banco.

```csharp
// Exemplo:
public class GarageOSDbContext : DbContext
{
    public DbSet<Veiculo> Veiculos { get; set; }
    public DbSet<OrdemServico> OrdensServico { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configurações do banco
    }
}
```

### 📁 Repositories (Implementações de Repositório)
Implementação das interfaces definidas no Domínio.

```csharp
// Exemplo:
public class VeiculoRepository : IVeiculoRepository
{
    private readonly GarageOSDbContext _dbContext;
    
    public VeiculoRepository(GarageOSDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<Veiculo> ObterPorIdAsync(Guid id)
    {
        return await _dbContext.Veiculos.FindAsync(id);
    }
    
    public async Task AdicionarAsync(Veiculo veiculo)
    {
        _dbContext.Veiculos.Add(veiculo);
        await _dbContext.SaveChangesAsync();
    }
}
```

### 📁 ExternalServices (Serviços Externos)
Integrações com APIs de terceiros, serviços de email, SMS, etc.

```csharp
// Exemplo:
public class EmailService : IEmailService
{
    public async Task EnviarAsync(string destinatario, string assunto, string corpo)
    {
        // Implementação com SendGrid, SMTP, etc
    }
}
```

### 📁 Mappings (Mapeamentos)
Configurações de Entity Framework ou AutoMapper.

```csharp
// Exemplo: Fluent API de EF Core
modelBuilder.Entity<Veiculo>()
    .HasKey(v => v.Id);
```

### 📁 Migrations (Migrações de Banco)
Histórico de mudanças no schema do banco de dados.

## Regra de Ouro 🏆
A lógica de acesso a dados e tecnologias específicas ficam **RESTRITAS AQUI**. O resto da aplicação fica protegido de mudanças de infraestrutura.

Exemplo: Trocar SQL Server por PostgreSQL? Apenas esta camada muda.
