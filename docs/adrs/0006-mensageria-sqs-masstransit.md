# ADR 0006 — Mensageria com Amazon SQS/SNS e MassTransit

| | |
|---|---|
| **Status** | Proposto |
| **Data** | 2026-10-28 |
| **Contexto** | Tech Challenge — Fase 4 |
| **Repositórios afetados** | os 3 serviços |
| **Relacionadas** | [ADR 0001](0001-comunicacao-entre-repositorios.md), [ADR 0005](0005-saga-orquestrada.md), [RFC 0004](../rfcs/0004-ferramenta-de-observabilidade.md) |

## Contexto

O enunciado pede mensageria assíncrona (RabbitMQ, Kafka, SQS etc.) para eventos e para a orquestração da saga, além de REST síncrono quando necessário.

As restrições herdadas da Fase 3 pesam aqui: o cluster opera perto do limite de memória, não tem driver EBS CSI (nenhum PVC sobe) e o crédito da conta é limitado. A [RFC 0004](../rfcs/0004-ferramenta-de-observabilidade.md) já recusou rodar estado dentro do cluster porque viraria o maior risco operacional do projeto.

## Decisão

- **Broker:** Amazon **SQS**, com **SNS** para eventos publicados. **DLQ em toda fila.**
- **Biblioteca:** **MassTransit 8.x**, com a versão fixada (`[8.*,9.0)`), transporte Amazon SQS, state machine da saga e outbox com EF Core nos serviços PostgreSQL.
- **Acesso:** IRSA por serviço, com permissão restrita ao prefixo das filas e tópicos dele. Nenhuma credencial estática.
- **Nomes:** `garageos-<env>-<serviço>-<fila>` para filas e `garageos-<env>-<Contrato>` para tópicos, para que as policies montem ARNs por convenção, no padrão do [ADR 0001](0001-comunicacao-entre-repositorios.md).
- **Contratos:** uma pasta `Contracts/V1` copiada nos três repositórios, com o mesmo namespace e URN explícita em cada mensagem. Status trafegam como texto e datas em UTC.
- **Rastreamento:** `traceparent` e `X-Correlation-Id` viajam nos headers das mensagens.
- **Local e testes:** LocalStack no `docker-compose` e o test harness do MassTransit nos testes.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **RabbitMQ no EKS** | Estado e PVC num cluster sem folga e sem EBS CSI. Sem volume, as filas se perdem quando o pod reinicia |
| **Amazon MQ (RabbitMQ gerenciado)** | ~US$ 20-25/mês sem free tier e ~20 min para criar |
| **Kafka / MSK** | Custo base alto, e o domínio não precisa de log particionado nem de replay |
| **AWSSDK.SQS puro, sem biblioteca** | Máquina de estados, outbox, retry, timeout e idempotência escritos e testados por nós. Fica como plano B se a licença da 8.x mudar |
| **NServiceBus** | Licença comercial |

## Consequências

**Positivas**

- Zero pod de broker no cluster e custo praticamente zero no free tier.
- A DLQ nativa é evidência visível de tratamento de falhas, útil no vídeo.
- Trocar de broker depois é configuração do MassTransit, não reescrita.

**Negativas e mitigação**

- **Entrega pelo menos uma vez:** mensagem duplicada vai acontecer. Idempotência em todo consumidor é requisito ([ADR 0005](0005-saga-orquestrada.md)).
- **Sem ordenação global** nas filas padrão. A ordem é dada pela máquina de estados, não pela fila.
- **O atraso máximo de mensagem no SQS é de 15 minutos.** Timeouts maiores precisam de um scheduler persistido. Na demonstração, os prazos são curtos e configuráveis.
- **Licença:** a v9 do MassTransit é comercial. Antes do primeiro commit com o pacote, confirmar que a 8.x segue Apache-2.0.
