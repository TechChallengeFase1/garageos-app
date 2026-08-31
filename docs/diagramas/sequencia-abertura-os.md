# Diagrama de Sequência — Abertura de Ordem de Serviço

O caminho de uma OS, da abertura ao acompanhamento pelo cliente.

Rota principal: `POST /api/OrdensDeServico/abertura-completa` (requer autenticação).

---

## 1. Abertura da OS

```mermaid
sequenceDiagram
    autonumber
    actor A as Atendente
    participant GW as API Gateway
    participant API as OrdensDeServicoController
    participant MW as CorrelationIdMiddleware
    participant UC as AbrirOrdemDeServico<br/>CompletaUseCase
    participant RC as IClienteRepository
    participant RV as IVeiculoRepository
    participant RS as IServicoRepository
    participant RE as IEstoqueRepository
    participant RO as IOrdemDeServicoRepository
    participant DB as RDS PostgreSQL

    A->>GW: POST /api/OrdensDeServico/abertura-completa<br/>{ clienteId, veiculoId, servicosIds, pecas }
    Note over GW: Lambda authorizer ja validou o token.
    GW->>API: encaminha com X-Cliente-Id
    API->>MW: pipeline
    MW->>MW: resolve X-Correlation-Id<br/>e publica no LogContext do Serilog
    MW->>API: segue
    API->>UC: ExecutarAsync(request)

    UC->>RC: ObterPorIdAsync(clienteId)
    RC->>DB: SELECT
    alt Cliente inexistente
        RC-->>UC: null
        UC--xAPI: ClienteNaoEncontradoException
        API-->>A: 404
    end
    RC-->>UC: Cliente

    UC->>RV: ObterPorIdAsync(veiculoId)
    RV->>DB: SELECT
    RV-->>UC: Veiculo

    loop para cada servicoId
        UC->>RS: ObterPorIdAsync(servicoId)
        RS->>DB: SELECT
        RS-->>UC: Servico
    end

    loop para cada peca
        UC->>RE: ObterPorIdAsync(estoqueId)
        RE->>DB: SELECT
        RE-->>UC: Estoque
    end

    Note over UC: So depois de TUDO validado<br/>e que a OS e construida.

    UC->>RO: ObterUltimoSequencialDoAnoAsync(2026)
    RO->>DB: SELECT
    RO-->>UC: ultimo sequencial
    UC->>UC: numeroOS = "OS-2026-00042"

    UC->>UC: new OrdemDeServico(...)<br/>status inicial: Recebida
    UC->>UC: AdicionarServico() por item<br/>status: Criada
    UC->>UC: AdicionarEstoque() por peca

    UC->>RO: AdicionarAsync(os)
    RO->>DB: INSERT OrdensDeServico<br/>+ itens de servico e de estoque
    DB-->>RO: ok
    RO-->>UC: ok
    UC-->>API: OrdemDeServicoResponse
    API-->>GW: 201 Created + X-Correlation-Id
    GW-->>A: 201 { id, numeroOS, status: "Recebida", ... }
```

**Pontos que valem a leitura**

- **Validação antes de construir.** Cliente, veículo, todos os serviços e todas as peças são verificados **antes** de a OS ser instanciada. Nenhuma OS parcial chega ao banco.
- **Numeração legível.** `OS-2026-00042` vem do último sequencial do ano mais um. Depende de leitura consistente — uma das razões da escolha de um banco relacional ([RFC 0002](../rfcs/0002-escolha-banco-de-dados.md)).
- **Correlation ID em tudo.** Publicado no `LogContext` do Serilog, ele aparece automaticamente em toda linha de log da requisição, e volta ao chamador no header `X-Correlation-Id`.
- **Baixa de estoque não acontece aqui.** As peças ficam apenas *vinculadas*; a baixa só ocorre quando o orçamento é aprovado.

---

## 2. Orçamento, aprovação e execução

```mermaid
sequenceDiagram
    autonumber
    actor A as Atendente
    actor C as Cliente
    participant API as API .NET
    participant DB as RDS PostgreSQL

    A->>API: POST /api/OrdensDeServico/{id}/orcamento
    API->>DB: carrega OS com servicos e pecas
    API->>API: preco = soma(servicos)<br/>+ soma(peca.valor * quantidade)
    API->>DB: INSERT Orcamento (Pendente)
    API-->>A: 200 OS com orcamento

    A->>API: POST /api/OrdensDeServico/{id}/orcamento/enviar
    API->>API: AvancarParaAguardandoAprovacao()
    API->>DB: UPDATE status = AguardandoAprovacao
    API-->>A: 200

    C->>API: PATCH /api/OrdensDeServico/{id}/orcamento/resposta<br/>{ aprovado }

    alt Aprovado
        API->>API: Orcamento.Aprovar()
        API->>API: OS.AvancarParaEmExecucao()
        loop para cada peca da OS
            API->>API: Estoque.DarBaixa(quantidade)
        end
        API->>DB: UPDATE orcamento, OS e estoques
        API-->>C: 200 status = EmExecucao
    else Rejeitado
        API->>API: Orcamento.Rejeitar()
        API->>API: OS.AlterarStatus(Finalizada)
        API->>DB: UPDATE
        API-->>C: 200 status = Finalizada
        Note over API: Sem baixa de estoque:<br/>nada foi consumido.
    end
```

**Ponto que vale a leitura:** a **baixa de estoque só ocorre na aprovação**. É o momento em que a peça deixa de estar reservada e passa a estar consumida — e é por isso que a operação é uma transação única com a mudança de status.

---

## 3. Ciclo de vida do status

```mermaid
stateDiagram-v2
    [*] --> Recebida: abertura-completa
    Recebida --> EmDiagnostico
    EmDiagnostico --> AguardandoAprovacao: orcamento enviado
    Recebida --> AguardandoAprovacao: orcamento enviado
    AguardandoAprovacao --> EmExecucao: cliente aprova<br/>(baixa de estoque)
    AguardandoAprovacao --> Finalizada: cliente rejeita
    EmExecucao --> Finalizada: servicos concluidos
    Finalizada --> Entregue: veiculo retirado
    Entregue --> [*]
```

Cada item de serviço dentro da OS tem seu **próprio** ciclo — `Criada` → `Iniciado` → `Finalizado` —, com `IniciadaEm` e `FinalizadaEm` registrados. São esses timestamps que alimentam o cálculo de aging (`GET /api/OrdensDeServico/aging`) e o dashboard de tempo médio por status ([RFC 0004](../rfcs/0004-ferramenta-de-observabilidade.md)).

---

## 4. Acompanhamento público pelo cliente

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente
    participant API as API .NET
    participant DB as RDS PostgreSQL

    C->>API: GET /api/OrdensDeServico/acompanhar/OS-2026-00042
    Note over API: [AllowAnonymous] — o numero da OS<br/>e a propria credencial de consulta.
    API->>DB: SELECT OS + itens de servico
    DB-->>API: dados
    API-->>C: 200 { numeroOS, status,<br/>servicos com status e tempos }
```

Esta rota é intencionalmente pública: o cliente acompanha a própria OS pelo número, sem login. As rotas de **escrita** e de listagem geral continuam protegidas — pelo Lambda authorizer no gateway e pelo `[Authorize]` na aplicação.

---

## Documentos relacionados

- [Diagrama ER](modelo-er.md)
- [Diagrama de sequência — autenticação](sequencia-autenticacao.md)
- [Diagrama de componentes](componentes.md)
