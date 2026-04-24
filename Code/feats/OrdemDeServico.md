# Plano de Implementação — Ordem de Serviço

## 0. Objetivo

O GarageOS é um sistema de gerenciamento de oficina mecânica. Até o momento, a base já possui cadastro de **Clientes**, **Veículos**, **Serviços** e **Estoque** (peças e insumos). O próximo passo é implementar o fluxo central do negócio: a **Ordem de Serviço (OS)**.

A Ordem de Serviço representa o ciclo de vida completo de um atendimento na oficina — desde a entrada do veículo até a entrega ao cliente. Ela conecta todas as entidades existentes em um único fluxo operacional.

### O que será implementado

**CRUD da OS** — criação, consulta e gerenciamento do ciclo de vida de uma ordem de serviço, vinculando cliente, veículo, serviços e peças do estoque.

**Fluxo de status** — a OS percorre estados bem definidos (`Recebida → Em diagnóstico → Aguardando aprovação → Em execução → Finalizada → Entregue`), onde cada transição é controlada por regras de negócio específicas.

**Geração de orçamento** — ao incluir os serviços na OS, o sistema gera automaticamente um orçamento com o valor total calculado a partir dos preços dos serviços cadastrados.

**Aprovação do cliente** — o cliente recebe o orçamento e pode aprovar ou rejeitar. A aprovação avança a OS para execução; a rejeição interrompe o fluxo.

**Acompanhamento público** — um endpoint sem autenticação permite que o cliente consulte o status da sua OS pelo número único gerado no momento da abertura.

**Monitoramento de execução** — cada serviço dentro da OS tem seu tempo de execução registrado individualmente (início e fim). Com base nesses registros, dois endpoints de aging são disponibilizados: um que recebe o ID de um serviço e retorna o seu tempo médio de execução, e outro que lista todos os serviços com seus respectivos tempos médios — permitindo à oficina identificar quais serviços consomem mais tempo na prática.

---

> **Convenção de datas:** todas as datas atribuídas nesta feature devem usar **GMT-3 (horário de Brasília)**. O projeto já possui o helper estático `BrasiliaTime.Agora` em `GarageOS.Domain/Utils/BrasiliaTime.cs`, que encapsula a conversão para `America/Sao_Paulo`. Usar sempre `BrasiliaTime.Agora` no lugar de `DateTime.UtcNow`. Nunca usar `DateTime.UtcNow` diretamente para campos de negócio.

---

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
Iniciado    → seta IniciadaEm = DateTime.Now (GMT-3, horário de Brasília)
Finalizado  → seta FinalizadaEm = DateTime.Now (GMT-3, horário de Brasília)
```

---

### 1.4 Regras de negócio críticas

| Regra | Onde implementar |
|---|---|
| `NumeroOS` gerado no formato `OS-{ANO}-{SEQUENCIAL:D5}` | Use case `CriarOrdemDeServico` (busca último sequencial no banco) |
| Status inicia sempre em `Recebida` | Construtor de `OrdemDeServico` |
| Ao adicionar serviços ou estoque → status vai para `EmDiagnostico` | Método `AdicionarServico` / `AdicionarEstoque` na entidade |
| `Orcamento.Preco` calculado automaticamente com soma dos `Servico.Preco` | Use case `GerarOrcamento` |
| Gerar orçamento sempre sobrescreve o orçamento anterior | Use case `GerarOrcamento` |
| Ao gerar orçamento → status da OS vai para `AguardandoAprovacao` | Use case `GerarOrcamento` |
| Ao aprovar orçamento → status da OS vai para `EmExecucao` | Use case `AprovarOrcamento` |
| Ao aprovar orçamento → decrementar `Quantidade` de cada item de `Estoque` vinculado à OS | Use case `AprovarOrcamento` |
| Ao rejeitar orçamento → status da OS vai para `Finalizada` automaticamente | Use case `RejeitarOrcamento` |
| `Finalizada` e `Entregue` → apenas via endpoint específico de alteração de status | Use case `AlterarStatusOrdemDeServico` |
| Ao iniciar serviço → seta `IniciadaEm` | Método `IniciarExecucao` em `OrdemDeServicoServico` |
| Ao finalizar serviço → seta `FinalizadaEm` | Método `FinalizarExecucao` em `OrdemDeServicoServico` |
| Endpoint de acompanhamento por `NumeroOS` → público (sem JWT) | Controller com `[AllowAnonymous]` |

---

### 1.5 Decisões de negócio confirmadas

1. **Geração do NumeroOS**: formato confirmado `OS-{ANO}-{SEQUENCIAL:D5}` (ex: `OS-2026-00001`). O sequencial é obtido buscando o último número registrado no banco para o ano corrente, garantindo unicidade e ordem cronológica.

2. **Rejeição do orçamento**: ao rejeitar o orçamento, o status da OS vai automaticamente para `Finalizada`. Não é possível reabrir ou gerar novo orçamento após rejeição.

3. **Múltiplos orçamentos**: a OS possui sempre um único orçamento. Caso seja necessário gerar novamente, o orçamento anterior é sobrescrito. Não há histórico de versões.

4. **Estoque na OS**: o vínculo das peças à OS é referencial. O decremento da `Quantidade` no estoque ocorre **apenas no momento da aprovação do orçamento**, com base nas peças listadas na OS.

5. **Aprovação do cliente — SMTP**: desconsiderado para esta entrega. O endpoint de aprovação será implementado sem envio de e-mail. SMTP fica como incremento futuro.

6. **Tempo médio dos serviços**: cálculo confirmado no back-end como média de `(FinalizadaEm - IniciadaEm)` sobre todas as execuções do mesmo `ServicoId` com status `Finalizado`.

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
- IniciarExecucao()   → Status = Iniciado, IniciadaEm = DateTime.Now (GMT-3, horário de Brasília)
- FinalizarExecucao() → Status = Finalizado, FinalizadaEm = DateTime.Now (GMT-3, horário de Brasília)
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
Task<int> ObterUltimoSequencialDoAnoAsync(int ano); // usado para gerar NumeroOS no formato OS-{ANO}-{SEQUENCIAL:D5}
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
- `CriarOrdemDeServicoUseCase` → busca último sequencial do ano no banco, gera `NumeroOS`, cria OS com status `Recebida`
- `ListarOrdensDeServicoUseCase`
- `ObterOrdemDeServicoUseCase`
- `AdicionarServicoNaOSUseCase` → adiciona serviço + muda status para `EmDiagnostico`
- `AdicionarEstoqueNaOSUseCase` → adiciona peça + muda status para `EmDiagnostico`
- `AlterarStatusOrdemDeServicoUseCase` → apenas `Finalizada` e `Entregue`
- `AcompanharOrdemDeServicoUseCase` → busca por `NumeroOS`, sem autenticação

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
- `GerarOrcamentoUseCase` → soma `Servico.Preco` dos serviços da OS → cria ou sobrescreve o `Orcamento` existente → avança OS para `AguardandoAprovacao`

**Endpoint:**
```
POST  /api/ordensdeservico/{id}/orcamento  → Gerar orçamento  [Authorize]
```

- Migration: `AdicionarOrcamento`

---

### ETAPA 3 — Aprovação do Cliente (Issue #14)

**Use Cases:**
- `AprovarOrcamentoUseCase` → `Orcamento.Aprovar()` → OS avança para `EmExecucao` → **decrementa `Quantidade` de cada item de `Estoque` vinculado à OS**
- `RejeitarOrcamentoUseCase` → `Orcamento.Rejeitar()` → OS vai automaticamente para `Finalizada`

**Endpoints:**
```
PATCH  /api/ordensdeservico/{id}/orcamento/aprovar   [Authorize]
PATCH  /api/ordensdeservico/{id}/orcamento/rejeitar  [Authorize]
```

> SMTP desconsiderado nesta entrega. Funcionalidade de envio de e-mail será implementada como incremento futuro.

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

> **Próximo passo:** plano validado e pronto para implementação. Iniciar pela Etapa 1.
