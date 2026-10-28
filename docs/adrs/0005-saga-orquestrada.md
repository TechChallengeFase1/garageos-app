# ADR 0005 — Saga Orquestrada no OS Service

| | |
|---|---|
| **Status** | Proposto |
| **Data** | 2026-10-28 |
| **Contexto** | Tech Challenge — Fase 4 |
| **Repositórios afetados** | `garageos-os-service` (orquestrador), `garageos-billing-service`, `garageos-execucao-service` (participantes) |
| **Relacionadas** | [ADR 0004](0004-divisao-em-microsservicos.md), [ADR 0006](0006-mensageria-sqs-masstransit.md), [RFC 0004](../rfcs/0004-ferramenta-de-observabilidade.md) |

## Contexto

O enunciado exige o **Saga Pattern** para coordenar o fluxo da OS (abrir → orçar → aprovar → executar), com **rollback e compensação em caso de falha em qualquer etapa**, aplicado de forma orquestrada ou coreografada, e com a escolha **documentada e justificada no README**.

Com a divisão do [ADR 0004](0004-divisao-em-microsservicos.md), o que hoje é um único `SaveChanges` passa a envolver três bancos (dois PostgreSQL e um DynamoDB) e o Mercado Pago. Não há transação distribuída possível entre eles.

## Decisão

**Saga orquestrada**, com a máquina de estados no **OS Service**, estado persistido no PostgreSQL dele e correlação pelo **Id da OS**. Implementação com a state machine do MassTransit ([ADR 0006](0006-mensageria-sqs-masstransit.md)).

### Fluxo

```text
OS:       abre a OS (Recebida) ─ saga começa
OS:       atendente solicita orçamento
Billing:  gera e envia o orçamento ─────────── OS AguardandoAprovacao
Cliente:  aprova
Execução: reserva as peças
Billing:  cobra via Mercado Pago ───────────── OS AguardandoPagamento
          (webhook confirma)
Execução: enfileira, diagnóstico, reparo ───── OS EmExecucao
Execução: conclui, baixa definitiva das peças ─ OS Finalizada
OS:       entrega manual ───────────────────── OS Entregue
```

O **pagamento vem depois da aprovação e da reserva, e antes da execução**. Assim os passos baratos e reversíveis acontecem primeiro, e o estorno só é necessário se a execução falhar.

### Compensações

| Falha | Compensações, em ordem | OS final |
|---|---|---|
| Cliente recusa o orçamento | nenhuma | `Cancelada` |
| Prazo de aprovação estourado | `CancelarOrcamento` | `Cancelada` |
| Peças indisponíveis | `CancelarOrcamento` | `Cancelada` |
| Pagamento recusado ou expirado | `LiberarReservaPecas` → `CancelarOrcamento` | `Cancelada` |
| Falha na execução | `EstornarPagamento` → `DevolverPecas` | `Cancelada` |
| Webhook de pagamento chega com a OS já cancelada | `EstornarPagamento` | continua `Cancelada` |
| A própria compensação falha | retry, DLQ e alerta | `CompensacaoPendente` |

### Regras que acompanham a decisão

- Comandos vão para a fila do participante; eventos são publicados. Uma falha técnica de comando (`Fault<T>`) conta como falha do passo.
- **Todo consumidor é idempotente**, compensações inclusive: o SQS entrega pelo menos uma vez. Chaves naturais com índice único (reserva por OS, pagamento por orçamento) e outbox/inbox.
- Timeouts de aprovação e de pagamento são agendados pela própria saga.
- `GET /api/ordensdeservico/{id}/saga` expõe o estado atual, o passo e a última compensação.
- Toda transição grava `HistoricoStatusOS` e emite as métricas de negócio já existentes (`OrdemDeServicoStatus`, `OrdemDeServicoFalha`).
- Novos status entram no enum, que passa a ser gravado como texto: `AguardandoPagamento` e `Cancelada`. A recusa deixa de ir para `Finalizada`.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **Coreografia** (eventos, sem coordenador) | Menos acoplamento, mas o estado da saga deixa de ter dono e a ordem das compensações fica espalhada por três repositórios. Demonstrar e testar "o que acontece se o pagamento falhar" fica muito mais difícil |
| **Orquestrador em serviço próprio** | Um 4º serviço sem domínio, recusado no [ADR 0004](0004-divisao-em-microsservicos.md) |
| **Híbrido** | Duas estratégias para documentar, testar e explicar em um vídeo de 15 minutos |
| **AWS Step Functions** | Tira a lógica de negócio do código .NET, e com isso do gate de cobertura e do Sonar |
| **Transação distribuída (2PC)** | Sem suporte entre RDS, DynamoDB e Mercado Pago |

## Consequências

**Positivas**

- O fluxo e as compensações ficam num só lugar, legíveis e testáveis com o test harness do MassTransit.
- O estado da saga é uma linha consultável, o que facilita a demonstração e o rastreamento no New Relic.
- O OS já é dono do status da OS, então o orquestrador não cria acoplamento novo.

**Negativas e mitigação**

- **O OS Service vira ponto central:** se ele cair, novas sagas não avançam. Mitigação: `minReplicas: 2` ([ADR 0002](0002-uso-do-hpa.md)) e mensagens paradas na fila em vez de perdidas.
- **O OS conhece os contratos dos outros dois serviços.** Aceito como custo da orquestração.
- **Consistência eventual:** logo depois de um comando, a consulta pode mostrar o status anterior. A API responde `202` e os clientes acompanham o status.
