# Plano de Implementação — Ordem de Serviço

## 1. Validação da Demanda

### 1.1 Entidades envolvidas

| Entidade | Tipo | Descrição |
|---|---|---|
| `OrdemDeServico` | Principal | Representa a OS completa |
| `Orcamento` | Dependente | Gerado a partir de uma OS |
| `OrdemDeServicoServico` | Tabela intermediária | Relaciona OS com Serviço + monitora tempo de execução |
| `OrdemDeServicoEstoque` | Tabela intermediária | Relaciona OS com itens de Estoque (peças/insumos) |

As entidades `Cliente`, `Veiculo`, `Servico` e `Estoque` já existem e serão referenciadas por FK.

---

### 1.2 Relacionamentos

```
OrdemDeServico
├── FK → Cliente         (N:1)
├── FK → Veiculo         (N:1)
├── FK → Orcamento       (1:0..1)
├── List<OrdemDeServicoServico>  (1:N via tabela intermediária)
└── List<OrdemDeServicoEstoque>  (1:N via tabela intermediária)

OrdemDeServicoServico
├── FK → OrdemDeServico
└── FK → Servico

OrdemDeServicoEstoque
├── FK → OrdemDeServico
└── FK → Estoque

Orcamento
└── FK → OrdemDeServico
```

---

### 1.3 Enums necessários

**`StatusOrdemDeServico`**
```
Recebida            → OS criada
EmDiagnostico       → Serviços ou peças adicionados
AguardandoAprovacao → Orçamento gerado
EmExecucao          → Orçamento aprovado
Finalizada          → Alteração manual via endpoint
Entregue            → Alteração manual via endpoint
```

**`StatusOrcamento`**
```
Aprovado
Rejeitado
```

**`StatusExecucaoServico`** (na tabela intermediária OrdemDeServicoServico)
```
Iniciado    → seta IniciadaEm = DateTime.UtcNow
Finalizado  → seta FinalizadaEm = DateTime.UtcNow
```

---

### 1.4 Regras de negócio críticas

| Regra | Onde implementar |
|---|---|
| `NumeroOS` gerado automaticamente (alfanumérico) | Construtor de `OrdemDeServico` |
| Status inicia sempre em `Recebida` | Construtor de `OrdemDeServico` |
| Ao adicionar serviços ou estoque → status vai para `EmDiagnostico` | Método `AdicionarServico` / `AdicionarEstoque` na entidade |
| `Orcamento.Preco` calculado automaticamente com soma dos `Servico.Preco` | Use case `GerarOrcamento` |
| Ao gerar orçamento → status da OS vai para `AguardandoAprovacao` | Use case `GerarOrcamento` |
| Ao aprovar orçamento → status da OS vai para `EmExecucao` | Use case `AprovarOrcamento` |
| `Finalizada` e `Entregue` → apenas via endpoint específico de alteração de status | Use case `AlterarStatusOrdemDeServico` |
| Ao iniciar serviço → seta `IniciadaEm` | Método `IniciarExecucao` em `OrdemDeServicoServico` |
| Ao finalizar serviço → seta `FinalizadaEm` | Método `FinalizarExecucao` em `OrdemDeServicoServico` |
| Endpoint de acompanhamento por `NumeroOS` → público (sem JWT) | Controller com `[AllowAnonymous]` |

---

### 1.5 Pontos em aberto — requerem confirmação antes da implementação

> **Estes pontos precisam ser validados antes de prosseguir:**

1. **Geração do NumeroOS**: qual o formato? Sugestão: `OS-{ANO}-{SEQUENCIAL:D5}` (ex: `OS-2026-00001`). Isso exige buscar o último número no banco. Confirmar formato e se pode ser simples (GUID parcial) ou sequencial.

2. **Rejeição do orçamento**: se o orçamento for rejeitado, o status da OS volta para `EmDiagnostico`? Ou a OS fica bloqueada? Pode gerar um novo orçamento após rejeição?

3. **Múltiplos orçamentos**: a OS pode ter mais de um orçamento (histórico de versões) ou sempre sobrescreve?

4. **Estoque na OS**: a relação com estoque é apenas referencial (registrar quais peças foram usadas) ou ao vincular uma peça à OS, a `Quantidade` do `Estoque` deve ser decrementada automaticamente?

5. **Aprovação do cliente (#14)**: a issue menciona possível envio de e-mail via SMTP. Implementar o endpoint de aprovação primeiro (sem e-mail) e deixar SMTP como incremento futuro?

6. **Tempo médio dos serviços (#19)**: o cálculo de tempo médio é feito no back-end e retornado em um endpoint específico. Confirmar: é `(FinalizadaEm - IniciadaEm)` por execução, com média sobre todas execuções do mesmo `ServicoId`?

---

## 2. Plano de Implementação

### Ordem de execução (por dependência)

```
Issue #11 + #12  →  Issue #13  →  Issue #14  →  Issue #16  →  Issue #19
  CRUD OS             Orçamento     Aprovação     Acompanhamento  Monitoramento
```

---

### ETAPA 1 — Estrutura base e CRUD da OS (Issues #11 e #12)

#### Domain

**Novos Enums**
- `Enums/StatusOrdemDeServico.cs`
- `Enums/StatusOrcamento.cs`
- `Enums/StatusExecucaoServico.cs`

**Nova Entidade: `OrdemDeServico`**
```
Entidades/OrdemDeServico.cs

Campos:
- Guid Id
- string NumeroOS              ← gerado no construtor
- StatusOrdemDeServico Status  ← inicia em Recebida
- DateTime CriadoEm
- DateTime? FinalizadaEm
- DateTime AtualizadoEm
- Guid ClienteId               ← FK
- Guid VeiculoId               ← FK
- Cliente? Cliente             ← navigation property
- Veiculo? Veiculo             ← navigation property
- List<OrdemDeServicoServico> Servicos
- List<OrdemDeServicoEstoque> Estoques
- Orcamento? Orcamento         ← navigation property

Métodos:
- AdicionarServico(OrdemDeServicoServico item)  → status → EmDiagnostico
- AdicionarEstoque(OrdemDeServicoEstoque item)  → status → EmDiagnostico
- AvancarParaAguardandoAprovacao()
- AvancarParaEmExecucao()
- AlterarStatus(StatusOrdemDeServico novoStatus)  ← apenas Finalizada e Entregue
```

**Nova Entidade: `OrdemDeServicoServico`** (tabela intermediária com monitoramento)
```
Campos:
- Guid Id
- Guid OrdemDeServicoId  ← FK
- Guid ServicoId         ← FK
- StatusExecucaoServico Status
- DateTime CriadoEm
- DateTime? IniciadaEm
- DateTime? FinalizadaEm
- Servico? Servico       ← navigation property

Métodos:
- IniciarExecucao()   → Status = Iniciado, IniciadaEm = UtcNow
- FinalizarExecucao() → Status = Finalizado, FinalizadaEm = UtcNow
```

**Nova Entidade: `OrdemDeServicoEstoque`** (tabela intermediária simples)
```
Campos:
- Guid Id
- Guid OrdemDeServicoId  ← FK
- Guid EstoqueId         ← FK
- int Quantidade         ← quantidade usada nesta OS
- Estoque? Estoque       ← navigation property
```

**Novas Exceções**
- `Exceptions/OrdemDeServicoNaoEncontradaException.cs`
- `Exceptions/OrdemDeServicoStatusInvalidoException.cs`

**Nova Interface de Repositório**
- `Repositories/IOrdemDeServicoRepository.cs`
```csharp
Task<IEnumerable<OrdemDeServico>> ListarTodosAsync();
Task<OrdemDeServico?> ObterPorIdAsync(Guid id);
Task<OrdemDeServico?> ObterPorNumeroOSAsync(string numeroOS);
Task AdicionarAsync(OrdemDeServico ordemDeServico);
Task AtualizarAsync(OrdemDeServico ordemDeServico);
Task<int> ObterUltimoSequencialAsync(int ano); ← para gerar NumeroOS
```

---

#### Application

**DTOs** (`DTOs/OrdensDeServico/`)
- `CriarOrdemDeServicoRequest` → `{ ClienteId, VeiculoId }`
- `AdicionarServicoRequest` → `{ ServicoId }`
- `AdicionarEstoqueRequest` → `{ EstoqueId, Quantidade }`
- `AlterarStatusRequest` → `{ Status }`
- `OrdemDeServicoResponse` → todos os campos + listas de serviços e estoque
- `AcompanhamentoOSResponse` → NumeroOS, Status, lista de serviços (campos reduzidos, público)

**Use Cases** (`UseCases/OrdensDeServico/`)
- `CriarOrdemDeServicoUseCase` → cria OS com status Recebida
- `ListarOrdensDeServicoUseCase`
- `ObterOrdemDeServicoUseCase`
- `AdicionarServicoNaOSUseCase` → adiciona serviço + muda status para EmDiagnostico
- `AdicionarEstoqueNaOSUseCase` → adiciona peça + muda status para EmDiagnostico
- `AlterarStatusOrdemDeServicoUseCase` → apenas Finalizada e Entregue
- `AcompanharOrdemDeServicoUseCase` → busca por NumeroOS, sem autenticação

**Validators** (`Validators/OrdensDeServico/`)
- `CriarOrdemDeServicoValidator`
- `AdicionarServicoValidator`
- `AdicionarEstoqueValidator`

---

#### Infrastructure

- `Mappings/OrdemDeServicoConfiguration.cs`
- `Mappings/OrdemDeServicoServicoConfiguration.cs`
- `Mappings/OrdemDeServicoEstoqueConfiguration.cs`
- `Repositories/OrdemDeServicoRepository.cs`
- Atualizar `GarageOSDbContext` com os novos `DbSet`

---

#### API

**Controller: `OrdensDeServicoController`**
```
POST   /api/ordensdeservico                       → Criar OS               [Authorize]
GET    /api/ordensdeservico                       → Listar OS              [Authorize]
GET    /api/ordensdeservico/{id}                  → Obter OS por ID        [Authorize]
POST   /api/ordensdeservico/{id}/servicos         → Adicionar serviço      [Authorize]
POST   /api/ordensdeservico/{id}/estoques         → Adicionar peça         [Authorize]
PATCH  /api/ordensdeservico/{id}/status           → Alterar status         [Authorize]
GET    /api/ordensdeservico/acompanhar/{numeroOS} → Acompanhar OS          [AllowAnonymous]
```

- Atualizar `ServiceCollectionExtensions`
- Atualizar `ExceptionMiddleware`
- Migration: `AdicionarOrdemDeServico`

---

### ETAPA 2 — Geração de Orçamento (Issue #13)

**Nova Entidade: `Orcamento`**
```
Campos:
- Guid Id
- StatusOrcamento Status
- decimal Preco          ← calculado (soma de Servico.Preco)
- DateTime CriadoEm
- DateTime AtualizadoEm
- Guid OrdemDeServicoId  ← FK

Métodos:
- Aprovar()  → Status = Aprovado
- Rejeitar() → Status = Rejeitado
```

**Nova Interface:** `IOrcamentoRepository`

**Use Cases:**
- `GerarOrcamentoUseCase` → soma `Servico.Preco` dos serviços da OS → cria `Orcamento` → avança OS para `AguardandoAprovacao`

**Endpoint:**
```
POST  /api/ordensdeservico/{id}/orcamento  → Gerar orçamento  [Authorize]
```

- Migration: `AdicionarOrcamento`

---

### ETAPA 3 — Aprovação do Cliente (Issue #14)

**Use Cases:**
- `AprovarOrcamentoUseCase` → `Orcamento.Aprovar()` → OS avança para `EmExecucao`
- `RejeitarOrcamentoUseCase` → `Orcamento.Rejeitar()` → *(definir comportamento da OS com base na resposta do ponto em aberto #2)*

**Endpoints:**
```
PATCH  /api/ordensdeservico/{id}/orcamento/aprovar   [Authorize ou AllowAnonymous?]
PATCH  /api/ordensdeservico/{id}/orcamento/rejeitar  [Authorize ou AllowAnonymous?]
```

> E-mail via SMTP: implementar como incremento posterior. Endpoints funcionam independentemente do e-mail.

---

### ETAPA 4 — API de Acompanhamento (Issue #16)

Já coberta pelo endpoint `[AllowAnonymous]` criado na Etapa 1.

**`AcompanhamentoOSResponse`** deve retornar:
- `NumeroOS`
- `Status` (texto amigável)
- Lista de serviços incluídos (nome, status de execução)

---

### ETAPA 5 — Monitoramento de tempo de execução (Issue #19)

**Use Cases:**
- `IniciarExecucaoServicoUseCase` → busca `OrdemDeServicoServico` → chama `IniciarExecucao()` → salva
- `FinalizarExecucaoServicoUseCase` → chama `FinalizarExecucao()` → salva
- `ObterTempoMedioServicoUseCase` → busca todas `OrdemDeServicoServico` com `ServicoId` e `Status = Finalizado` → calcula média de `(FinalizadaEm - IniciadaEm)`
- `ListarTemposMediosUseCase` → mesmo cálculo, sem filtro de `ServicoId`

**Endpoints:**
```
PATCH  /api/ordensdeservico/{id}/servicos/{itemId}/iniciar    [Authorize]
PATCH  /api/ordensdeservico/{id}/servicos/{itemId}/finalizar  [Authorize]
GET    /api/servicos/{servicoId}/tempo-medio                  [Authorize]
GET    /api/servicos/tempos-medios                            [Authorize]
```

---

## 3. Resumo do que será criado

| Camada | Novos arquivos |
|---|---|
| Domain | 3 enums, 4 entidades, 2 exceções, 1 interface de repositório |
| Application | ~8 DTOs, ~10 use cases, ~3 validators |
| Infrastructure | 4 configurations, 1 repository, atualizar DbContext |
| API | 1 controller (~10 endpoints), atualizar Extensions e Middleware |
| Banco | 3 migrations (OrdemDeServico, Orcamento, tabelas intermediárias) |

---

> **Próximo passo:** confirmar os 6 pontos em aberto da seção 1.5 antes de iniciar a implementação.
