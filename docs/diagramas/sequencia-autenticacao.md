# Diagrama de Sequência — Fluxo de Autenticação

Como o cliente troca um CPF por um token, e como esse token é verificado nas requisições seguintes.

Decisões por trás deste fluxo: [RFC 0003 — Estratégia de Autenticação](../rfcs/0003-estrategia-de-autenticacao.md).

---

## 1. Obtenção do token (`POST /auth`)

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente
    participant GW as API Gateway<br/>HTTP API
    participant L as Lambda auth<br/>(dentro da VPC)
    participant DB as RDS PostgreSQL

    C->>GW: POST /auth<br/>{ "cpf": "123.456.789-09" }
    Note over GW: Rota publica — sem authorizer.<br/>Exigir token aqui seria circular.
    GW->>L: invoca (AWS_PROXY, payload 2.0)

    L->>L: normaliza o CPF (so digitos)
    L->>L: valida digitos verificadores

    alt CPF invalido
        L-->>GW: 400 { "erro": "CPF invalido." }
        GW-->>C: 400
    else CPF valido
        L->>DB: SELECT "Id","Nome","Email","Ativo"<br/>FROM "Clientes"<br/>WHERE "DocumentoValor" = $1
        Note over L,DB: Pool criado fora do handler:<br/>a conexao sobrevive entre invocacoes.

        alt Falha de conexao
            DB--xL: timeout / erro
            L-->>GW: 503 { "erro": "Servico de dados indisponivel." }
            GW-->>C: 503
        else Cliente nao encontrado
            DB-->>L: 0 linhas
            L-->>GW: 404 { "erro": "Cliente nao encontrado." }
            GW-->>C: 404
        else Cliente inativo
            DB-->>L: Ativo = false
            L-->>GW: 403 { "erro": "Cliente inativo." }
            GW-->>C: 403
        else Cliente ativo
            DB-->>L: { Id, Nome, Email, Ativo = true }
            L->>L: assina JWT HS256<br/>sub, name, email, cpf, iss, aud, iat, exp
            L-->>GW: 200 { accessToken, tokenType,<br/>expiresIn: 3600, cliente }
            GW-->>C: 200 + token
        end
    end
```

**Pontos que valem a leitura**

- A validação de CPF acontece **antes** de tocar no banco: evita consulta desnecessária e devolve `400` em vez de `404`. Rejeita também os 11 dígitos iguais, que passam no cálculo dos verificadores mas não são CPF real.
- A chave de assinatura vem do **AWS Secrets Manager**, injetada como variável de ambiente no `terraform apply`. A Lambda está em subnet privada e não há NAT Gateway, então ela não alcançaria a API do Secrets Manager em tempo de execução.
- Cada cenário tem um código HTTP próprio — `400`, `403`, `404` e `503` dizem coisas diferentes a quem consome.

---

## 2. Uso do token em rota protegida

```mermaid
sequenceDiagram
    autonumber
    actor C as Cliente
    participant GW as API Gateway
    participant AZ as Lambda authorizer<br/>(fora da VPC)
    participant NLB as Network<br/>Load Balancer
    participant API as API .NET<br/>no EKS
    participant DB as RDS PostgreSQL

    C->>GW: GET /api/OrdensDeServico<br/>Authorization: Bearer [token]

    alt Resposta em cache (TTL 5 min por header)
        Note over GW: identity_sources = $request.header.Authorization<br/>authorizer_result_ttl = 300s
        GW->>GW: reutiliza a autorizacao anterior
    else Sem cache
        GW->>AZ: invoca com o header Authorization
        AZ->>AZ: HMAC-SHA256 sobre "cabecalho.corpo"
        AZ->>AZ: compara com timingSafeEqual
        AZ->>AZ: confere exp, iss e aud

        alt Token invalido, expirado ou ausente
            AZ-->>GW: { isAuthorized: false }
            GW-->>C: 401 Unauthorized
            Note over GW,NLB: A requisicao NAO chega ao cluster.
        else Token valido
            AZ-->>GW: { isAuthorized: true,<br/>context: { clienteId, cpf } }
        end
    end

    GW->>NLB: repassa com<br/>X-Cliente-Id e X-Cliente-Cpf
    NLB->>API: encaminha ao pod pronto
    API->>API: CorrelationIdMiddleware<br/>resolve X-Correlation-Id
    API->>API: valida o mesmo JWT<br/>(segunda linha de defesa)
    API->>DB: consulta
    DB-->>API: dados
    API-->>NLB: 200 + X-Correlation-Id
    NLB-->>GW: 200
    GW-->>C: 200
```

**Pontos que valem a leitura**

- O authorizer é do tipo **REQUEST**, não JWT nativo. O authorizer JWT do HTTP API exige um provedor OIDC/OAuth2 que exponha JWKS; um token HS256 não tem chave pública para o gateway buscar.
- Ele roda **fora da VPC** de propósito: só confere uma assinatura HMAC, não toca no banco. Sem ENI para criar, o cold start é muito menor — e ele roda em toda requisição protegida.
- A comparação usa `crypto.timingSafeEqual`. Uma comparação com `===` sai no primeiro byte diferente, e medir esse tempo permite descobrir a assinatura byte a byte.
- A identidade extraída do token viaja como header (`X-Cliente-Id`, `X-Cliente-Cpf`), então a aplicação não precisa decodificar o JWT de novo.
- A API .NET **também** valida o token. Redundante por segurança: o NLB é `internet-facing` e continua acessível sem passar pelo gateway.

---

## 3. A chave compartilhada

```mermaid
flowchart LR
    BOOT["bootstrap/app-secrets.tf<br/>random_password"] -->|"grava"| SM["Secrets Manager<br/>garageos/app/secrets"]
    SM -->|"terraform apply"| LMB["Lambda: variavel<br/>de ambiente"]
    SM -->|"pipeline de CD"| K8S["Secret do Kubernetes<br/>Jwt__SecretKey"]
    LMB -->|"assina"| TOK(("JWT HS256"))
    K8S -->|"valida"| TOK
```

`JWT_SECRET_KEY`, `JWT_ISSUER` e `JWT_AUDIENCE` saem do **mesmo segredo**. É isso que impede o modo de falha silencioso: se os dois lados divergirem, a API rejeita **todo** token emitido pela Lambda — sem erro claro, sem log útil, só 401 em tudo.

O segredo vive no *bootstrap*, e não no repositório do banco, porque destruir o banco não pode invalidar todos os tokens em circulação.

---

## Documentos relacionados

- [RFC 0003 — Estratégia de Autenticação](../rfcs/0003-estrategia-de-autenticacao.md)
- [Diagrama de componentes](componentes.md)
- [Diagrama de sequência — abertura de OS](sequencia-abertura-os.md)
