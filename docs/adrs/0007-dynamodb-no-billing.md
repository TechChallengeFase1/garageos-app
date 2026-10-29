# ADR 0007 — DynamoDB como Banco do Billing Service

| | |
|---|---|
| **Status** | Proposto |
| **Data** | 2026-10-28 |
| **Contexto** | Tech Challenge — Fase 4 |
| **Repositórios afetados** | `garageos-billing-service` |
| **Relacionadas** | [RFC 0002](../rfcs/0002-escolha-banco-de-dados.md), [ADR 0004](0004-divisao-em-microsservicos.md), [ADR 0006](0006-mensageria-sqs-masstransit.md) |

## Contexto

O enunciado exige **pelo menos um banco relacional e pelo menos um não relacional**, um por serviço. A [RFC 0002](../rfcs/0002-escolha-banco-de-dados.md) escolheu PostgreSQL para o domínio inteiro, e o motivo continua válido para OS e Execução: unicidade de CPF, placa e número da OS, reserva de várias peças numa transação e o aging como agregação SQL.

A pergunta da Fase 4 é outra: onde o NoSQL obrigatório custa menos e se encaixa melhor.

## Decisão

**Amazon DynamoDB**, on-demand, no **Billing Service**. OS e Execução continuam em PostgreSQL.

O Billing é o melhor encaixe porque:

- **Todo acesso é por chave:** orçamento por id ou por OS, pagamento por orçamento ou pelo id do Mercado Pago. Não há join com outro agregado.
- **O orçamento é um documento imutável:** os itens chegam como snapshot no comando `GerarOrcamento`, com preço congelado.
- **A idempotência sai de graça:** escritas condicionais (`attribute_not_exists`) deduplicam webhooks do Mercado Pago e mensagens repetidas.

### Modelo (tabela única)

| PK | SK | Conteúdo |
|---|---|---|
| `ORC#<id>` | `META` | orçamento com os itens e o status |
| `ORC#<id>` | `PAG#<id>` | pagamento ligado ao orçamento |
| `WEBHOOK#<paymentId>#<status>` | — | deduplicação de notificações |
| `INBOX#<messageId>` | — | deduplicação de mensagens (com TTL) |
| `OUTBOX#<id>` | — | eventos a publicar |

- GSIs por `osId` e por `mpPaymentId`, e um GSI esparso para o outbox pendente.
- Atributo `version` para concorrência otimista.
- `TransactWriteItems` grava a entidade, o inbox e o outbox juntos.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **DynamoDB no Execução** | A reserva de N peças tudo-ou-nada e o aging são naturalmente SQL, e o código de estoque e aging já existe em EF Core |
| **DocumentDB** | Sem free tier, ~US$ 57/mês na menor instância, mais I/O |
| **MongoDB no cluster** | Estado no cluster sem folga e sem EBS CSI, o padrão recusado pela [RFC 0004](../rfcs/0004-ferramenta-de-observabilidade.md) |
| **MongoDB Atlas (free tier)** | Fora da AWS: sem IAM/IRSA, credencial estática e fora do padrão de descoberta do [ADR 0001](0001-comunicacao-entre-repositorios.md) |
| **PostgreSQL com `jsonb`** | Continua sendo um banco relacional; não cumpre o requisito |

## Consequências

**Positivas**

- Custo fixo zero e free tier permanente; nada é cobrado com o ambiente destruído.
- Nenhum pod nem Security Group a mais: o acesso é por IRSA, restrito ao ARN da tabela.
- Os dois RDS (OS e Execução) cabem no free tier com a disciplina de destruir o ambiente fora das janelas de trabalho.

**Negativas e mitigação**

- **A equipe não tem experiência com DynamoDB.** O escopo é contido: uma tabela, padrões de acesso definidos antes do código.
- **O código do `Orcamento` é reescrito** para o novo repositório; as regras de cálculo (`GerarOrcamentoUseCase.cs:27-35`) são reaproveitadas.
- **Testes de repositório** usam `amazon/dynamodb-local` (ou LocalStack) com Testcontainers.
