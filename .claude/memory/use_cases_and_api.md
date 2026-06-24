---
name: use-cases-and-api
description: Use cases implementados e endpoints da API GarageOS com fluxo de negócio da Ordem de Serviço
metadata: 
  node_type: memory
  type: project
  originSessionId: 30c16610-3cae-44f4-be13-18f823207726
---

## Use Cases e API (Code/GarageOS.Application/ + Code/GarageOS.Api/)

### Controllers e Autenticação

| Controller | Endpoints | Auth |
|---|---|---|
| `AuthController` | POST `/api/Auth/login` | Público |
| `ClientesController` | GET/POST/PUT/DELETE `/api/Clientes[/{id}]` | [Authorize] |
| `VeiculosController` | GET/POST/PUT/DELETE `/api/Veiculos[/{id}]` | [Authorize] |
| `ServicosController` | GET/POST/PUT/DELETE `/api/Servicos[/{id}]` | [Authorize] |
| `EstoquesController` | GET/POST/PUT/DELETE `/api/Estoques[/{id}]` | [Authorize] |
| `OrdensDeServicoController` | 12 endpoints complexos | Maioria [Authorize] |

### Endpoints de OrdensDeServico

```
POST   /api/OrdensDeServico                                              → Criar OS
GET    /api/OrdensDeServico                                              → Listar todas
GET    /api/OrdensDeServico/{id:guid}                                    → Obter por ID
POST   /api/OrdensDeServico/{id:guid}/servicos                           → Adicionar serviço
POST   /api/OrdensDeServico/{id:guid}/estoques                           → Adicionar peça
PATCH  /api/OrdensDeServico/{id:guid}/status                             → Alterar status (Finalizada/Entregue)
POST   /api/OrdensDeServico/{id:guid}/orcamento                          → Gerar orçamento
POST   /api/OrdensDeServico/{id:guid}/orcamento/enviar                   → Enviar orçamento ao cliente
PATCH  /api/OrdensDeServico/{id:guid}/orcamento/resposta                 → Aprovar ou rejeitar
PATCH  /api/OrdensDeServico/{numeroOS}/servicos/{servicoItemId:guid}/status → Status de execução do serviço
GET    /api/OrdensDeServico/aging                                        → Tempo médio por serviço
GET    /api/OrdensDeServico/acompanhar/{numeroOS}                        → Público (cliente acompanha)
```

### Fluxo de Negócio Principal (Ordem de Serviço)

```
1. Criar OS                → Status: Recebida
2. Adicionar Serviços/Estoques → Status: EmDiagnostico
3. Gerar Orçamento         → Calcula total
4. Enviar Orçamento        → Status: AguardandoAprovacao
5. Cliente Responde:
   - Aprova                → Status: EmExecucao + desconta estoque
   - Rejeita               → Status: Finalizada
6. Durante Execução        → Marcar serviços Iniciado/Finalizado (com timestamps)
7. Encerrar                → Status: Finalizada ou Entregue
8. Aging                   → Calcula tempo médio (IniciadaEm → FinalizadaEm) por serviço
```

### Use Cases por Domínio

**Cliente (5):** Listar, Cadastrar, Obter, Alterar, Deletar (soft delete)

**Veiculo (6):** Listar, Obter, Cadastrar, Alterar, Deletar, VincularVeiculoCliente

**Servico (4):** Listar, Obter, Cadastrar, Alterar

**Estoque (5):** Listar, Obter, Cadastrar, Alterar, Deletar

**OrdemDeServico (12):**
- `CriarOrdemDeServicoUseCase`
- `ListarOrdensDeServicoUseCase`
- `ObterOrdemDeServicoUseCase`
- `AdicionarServicoNaOSUseCase`
- `AdicionarEstoqueNaOSUseCase`
- `AlterarStatusOrdemDeServicoUseCase`
- `AcompanharOrdemDeServicoUseCase` (público)
- `GerarOrcamentoUseCase`
- `EnviarOrcamentoUseCase`
- `ResponderOrcamentoUseCase`
- `AlterarStatusServicoNaOSUseCase`
- `CalcularAgingServicosUseCase`

### Infraestrutura

**DbContext:** `Code/GarageOS.Infrastructure/Data/GarageOSDbContext.cs`
- 8 DbSets: Servicos, Veiculos, Clientes, Estoques, OrdensDeServico, OrdensDeServicoServicos, OrdensDeServicoEstoques, Orcamentos

**Mappings:** `Code/GarageOS.Infrastructure/Mappings/*.cs`
- Um arquivo de configuração EF por entidade

**Migrations:** `Code/GarageOS.Infrastructure/Migrations/` (6 migrations)
- Initial → AddTableVeiculos → AddTableClientes → AddClienteIdToVeiculo → AdicionarEstoque → AdicionarOrdensDeServico

**Repositórios:** `Code/GarageOS.Infrastructure/Repositories/*.cs`
- Implementações de todos os `IXxxRepository` do Domain
