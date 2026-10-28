# ADR 0004 — Divisão em Microsserviços

| | |
|---|---|
| **Status** | Proposto |
| **Data** | 2026-10-28 |
| **Contexto** | Tech Challenge — Fase 4 |
| **Repositórios afetados** | `garageos-app` (vira `garageos-os-service`), `garageos-billing-service`, `garageos-execucao-service` |
| **Relacionadas** | [RFC 0002](../rfcs/0002-escolha-banco-de-dados.md), [ADR 0005](0005-saga-orquestrada.md), [ADR 0006](0006-mensageria-sqs-masstransit.md), [ADR 0007](0007-dynamodb-no-billing.md) |

## Contexto

O enunciado da Fase 4 pede no mínimo **3 microsserviços independentes**, cada um com repositório, infraestrutura e banco próprios, e proíbe que um serviço acesse o banco de outro. Sugere três responsabilidades: Ordem de Serviço, Orçamento e Pagamento, e Execução e Produção.

Hoje o GarageOS é um monólito com um único `GarageOSDbContext` e 8 tabelas no mesmo RDS. Há foreign keys ligando contextos que vão se separar (`OrdemDeServicoConfiguration.cs:32-55`, `OrdemDeServicoEstoqueConfiguration.cs:21`), e a aprovação do orçamento (`ResponderOrcamentoUseCase.cs:27-41`) aprova o orçamento, muda o status da OS e dá baixa no estoque num único `SaveChanges`. É essa transação que, depois da divisão, vira uma saga.

## Decisão

Três serviços, seguindo a sugestão do enunciado e as fronteiras que o domínio já mostra:

| Serviço | Responsabilidade | Banco | Dono dos dados |
|---|---|---|---|
| `garageos-os-service` | Abertura e consulta da OS, status e histórico, acompanhamento público, login admin. **Hospeda o orquestrador da saga** | PostgreSQL (RDS próprio) | `OrdemDeServico` e itens (com snapshot de nome e preço), `HistoricoStatusOS`, `Cliente`, `Veiculo`, `Servico`, estado da saga |
| `garageos-billing-service` | Geração e envio do orçamento, aprovação/recusa, pagamento via Mercado Pago, webhook | DynamoDB ([ADR 0007](0007-dynamodb-no-billing.md)) | `Orcamento`, `Pagamento`, notificações de webhook |
| `garageos-execucao-service` | Estoque e reserva de peças, fila de execução, status de diagnóstico e reparo, aging | PostgreSQL (RDS próprio) | `Estoque`, `ReservaPecas`, `OrdemDeExecucao` e itens |

### Onde fica cada catálogo

- **Cliente, Veículo e Serviço (mão de obra) ficam no OS.** A abertura da OS depende deles, e o login por CPF lê clientes.
- **Estoque fica no Execução.** A peça é consumida na produção, e reservar/liberar peça é um passo compensável da saga.

### Como um serviço usa dado de outro sem ler o banco alheio

- O OS valida e precifica uma peça com `GET /api/estoques/{id}` no Execução (REST síncrono, com timeout e retry) e guarda um **snapshot** de nome e valor no item da OS.
- O Billing nunca consulta catálogo: recebe no comando `GerarOrcamento` os itens já com preço. O preço do orçamento fica congelado.
- O Execução recebe no comando `IniciarExecucao` os itens de serviço que precisa executar.
- As foreign keys entre contextos são removidas; cada serviço guarda só o ID do dado alheio.

### Mapa do código atual

| Vai para | O que move |
|---|---|
| OS | `Cliente`, `Veiculo`, `Servico`, `Documento`, `Endereco`; use cases `Clientes/*`, `Veiculos/*`, `Servicos/*`, `AbrirOrdemDeServicoCompleta`, `Listar`, `Obter`, `AdicionarServico/EstoqueNaOS`, `AlterarStatus` (só `Entregue`), `Acompanhar`; `AuthController` |
| Billing | `Orcamento`, `StatusOrcamento`; `GerarOrcamento`, `EnviarOrcamento`, `ResponderOrcamento` (sem a baixa de estoque) |
| Execução | `Estoque` (+ reserva), `OrdemDeServicoServico` como base do item de execução; `Estoque/*`, `AlterarStatusServicoNaOS`, `CalcularAgingServicos` |

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **4º serviço só de cadastros** (cliente, veículo, catálogo) | Mais um repositório, pipeline, banco e gate de 80% para atender um requisito que 3 serviços já atendem. E transformaria as FKs Cliente → Veículo → OS em chamadas REST sem garantia |
| **Um serviço por agregado** | Multiplica o custo acima e não cabe no cluster atual |
| **Orquestrador em serviço próprio** | Seria um 4º serviço sem domínio, consultando o OS a cada passo. O OS já é dono do ciclo de vida da OS |
| **Divisão por camada técnica** (API / worker) | Não são microsserviços de domínio e nenhum teria banco próprio com sentido |

## Consequências

**Positivas**

- Atende literalmente o enunciado: 3 repositórios, 3 infraestruturas, 3 bancos, com SQL e NoSQL.
- Os use cases atuais se dividem em três grupos quase sem sobreposição, e a maior parte move sem reescrita.

**Negativas e mitigação**

- **O OS fica maior que os outros dois.** É assimetria deliberada: ele é o dono do ciclo de vida e hospeda o orquestrador.
- **Perde-se integridade referencial entre serviços.** Mitigação: validação síncrona na escrita (o `AbrirOrdemDeServicoCompletaUseCase` já valida cliente, veículo, serviços e peças) e compensação pela saga quando um passo posterior falha.
- **3 serviços × 2 réplicas não cabem nos 2× t3.small atuais**, que já operam a ~56% de memória com uma aplicação ([ADR 0002](0002-uso-do-hpa.md)). Pré-condição: `node_instance_type = "t3.medium"` em `garageos-infra-k8s/producao.tfvars`.
- **A Lambda de autenticação lê a tabela `Clientes` direto do RDS.** Depois da divisão, ela passa a usar uma role Postgres somente leitura (`auth_reader`, só `SELECT` em `"Clientes"`) e é documentada como componente de borda do OS Service. Chamar o OS por HTTP exigiria um LB interno, porque a Lambda está numa subnet privada sem NAT.
