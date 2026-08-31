# RFC 0003 — Estratégia de Autenticação

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-08-27 |
| **Autores** | Equipe GarageOS (Tech Challenge — Fase 3) |
| **Repositórios afetados** | `garageos-lambda-auth`, `garageos-app`, `garageos-infra-database` |
| **Relacionadas** | [RFC 0001](0001-escolha-provedor-cloud.md), [RFC 0002](0002-escolha-banco-de-dados.md), [ADR 0001](../adrs/0001-comunicacao-entre-repositorios.md) |

## Contexto

Até a Fase 2, a única autenticação do GarageOS era um login administrativo (`POST /api/Auth/login` com usuário e senha), emitindo um JWT validado pela própria API .NET.

A Fase 3 muda o requisito: as rotas sensíveis passam a ser protegidas por **autenticação do cliente via CPF**, e essa autenticação deve ser implementada como **Function Serverless** atrás de um **API Gateway** — ambos requisitos obrigatórios do enunciado.

O esclarecimento do professor definiu o escopo: o CPF é o identificador do cliente já cadastrado na oficina; não há cadastro de senha para o cliente final. Autenticar significa **provar que aquele CPF corresponde a um cliente ativo** e, a partir disso, emitir uma credencial de curta duração.

## Problema

Como autenticar o cliente pelo CPF em uma função serverless, emitir uma credencial que a API em Kubernetes saiba validar, e proteger as rotas sensíveis sem que a API precise conhecer a Lambda nem a Lambda conhecer a API?

Três subproblemas:

1. **Formato da credencial** — o que a Lambda devolve ao cliente?
2. **Confiança compartilhada** — como a API .NET valida um token emitido por uma função Node.js, sem acoplamento entre os dois repositórios?
3. **Ponto de validação** — quem barra a requisição sem token válido: o gateway ou a aplicação?

## Opções consideradas

### 1. Formato da credencial

| Opção | A favor | Contra |
|---|---|---|
| **JWT HS256 (escolhida)** | A API .NET já valida JWT desde a Fase 1 — o pipeline de autenticação do ASP.NET Core não muda; chave simétrica é simples de distribuir entre dois consumidores | Chave simétrica precisa existir nos dois lados; quem valida também consegue assinar |
| JWT RS256 | Só o emissor tem a chave privada; validadores usam a pública | Exigiria gerar, guardar e rotacionar um par de chaves, e expor um JWKS. Complexidade sem ganho no cenário: emissor e validador são do mesmo time e da mesma conta AWS |
| Token opaco + introspecção | Revogação imediata | Toda requisição protegida viraria uma chamada extra ao emissor. Latência e ponto de falha adicionais |
| Amazon Cognito | Serviço gerenciado, JWKS pronto, integra com o authorizer JWT nativo do API Gateway | O login é por CPF contra a base própria da oficina, não por usuário/senha em um diretório de identidades. Modelar isso no Cognito exigiria custom auth flow com Lambda triggers — mais peças para o mesmo resultado |

### 2. Runtime da Lambda

| Opção | Cold start | Observação |
|---|---|---|
| **Node.js 22 (escolhida)** | ~200 ms | Dependência externa única (`pg`). O HS256 sai do módulo `crypto` nativo |
| .NET 10 | 1 a 2 s sem Native AOT | Numa autenticação, essa é a diferença entre o login parecer instantâneo ou travado |

O HS256 é idêntico nas duas linguagens, então a API .NET valida o token sem saber quem o assinou.

### 3. Ponto de validação

| Opção | A favor | Contra |
|---|---|---|
| **Lambda Authorizer no API Gateway (escolhida)** | A requisição sem token válido é recusada **antes** de chegar ao cluster; resposta cacheada por 5 min por valor do header `Authorization` | Uma invocação a mais (amortizada pelo cache) |
| Authorizer JWT nativo do HTTP API | Zero código | Exige um provedor OIDC/OAuth2 que exponha JWKS. Um token HS256 não tem chave pública para o gateway buscar — **tecnicamente inviável** aqui |
| Validar só na API .NET | Nenhuma peça nova | O tráfego não autenticado consumiria capacidade do cluster; o gateway deixaria de ser ponto de controle |

## Decisão

Autenticação por **CPF em AWS Lambda**, emitindo **JWT HS256**, validado por um **Lambda Authorizer** no API Gateway HTTP API e, em segunda instância, pela própria API .NET.

### Arquitetura

```text
Cliente
   │ POST /auth { "cpf": "123.456.789-09" }
   ▼
API Gateway (rota pública)
   ▼
Lambda garageos-<env>-auth        (DENTRO da VPC, subnets privadas)
   │ 1. valida dígitos verificadores do CPF
   │ 2. SELECT em "Clientes" WHERE "DocumentoValor" = cpf
   │ 3. confere se o cliente está ativo
   ▼
RDS PostgreSQL
   │
   ▼ JWT HS256 (1 h)
Cliente
   │ ANY /{proxy+} com Authorization: Bearer <token>
   ▼
API Gateway  ──►  Lambda garageos-<env>-authorizer   (FORA da VPC)
   │                 confere assinatura, exp, iss, aud
   ▼ isAuthorized
Network Load Balancer  ──►  API .NET no EKS
```

### Duas funções, um mesmo arquivo

| Função | Handler | VPC | Por quê |
|---|---|---|---|
| `garageos-<env>-auth` | `index.handler` | **Dentro**, subnets privadas, com o crachá do RDS | Precisa consultar o banco |
| `garageos-<env>-authorizer` | `index.authorizer` | **Fora** | Só confere uma assinatura HMAC. Sem ENI para criar, o cold start é muito menor — e ela roda em toda requisição protegida |

### A chave compartilhada

`JWT_SECRET_KEY`, `JWT_ISSUER` e `JWT_AUDIENCE` vivem em **um único segredo no AWS Secrets Manager**, criado pelo bootstrap (`garageos/app/secrets`), e são lidos por:

- a **Lambda**, no momento do `terraform apply`, injetados como variável de ambiente;
- a **API .NET**, pela pipeline de CD, que lê o Secrets Manager e cria o Secret do Kubernetes.

O segredo fica no *bootstrap*, e não no repositório do banco, de propósito: destruir o banco não pode invalidar todos os tokens em circulação. É fundação compartilhada, muda raramente e serve mais de um repositório.

> **O modo de falha que isso evita:** se a chave da Lambda e a da API divergirem, a API rejeita silenciosamente **todo** token emitido pela Lambda — sem erro claro, sem log útil, só 401 em tudo. Ler as duas da mesma fonte elimina a classe inteira de bug. O mesmo vale para `issuer` e `audience`: divergência ali quebra a validação do mesmo jeito.

### Por que a Lambda não lê o Secrets Manager em tempo de execução

A Lambda de autenticação fica em subnet privada e o bootstrap **não criou NAT Gateway** (economia de ~US$ 32/mês). Sem rota para a internet, ela não alcança a API do Secrets Manager. As alternativas seriam um VPC endpoint de interface (~US$ 14/mês) ou o NAT. A fonte da verdade continua sendo o cofre; o que muda é *quando* o valor é lido — no apply, não na invocação.

### Decisões de implementação

- **Validação de CPF antes de tocar no banco.** 11 dígitos, não todos iguais (`111.111.111-11` passa no cálculo dos dígitos verificadores mas não é CPF real) e dígitos verificadores conferidos. Evita consulta desnecessária e devolve `400` em vez de `404`.
- **`crypto.timingSafeEqual` na verificação da assinatura.** Uma comparação com `===` sai no primeiro byte diferente, e medir esse tempo permite descobrir a assinatura byte a byte.
- **Pool de conexões fora do handler.** A AWS reaproveita o container entre invocações, então a conexão só é aberta na primeira chamada fria.
- **Identidade repassada por header.** O authorizer devolve `clienteId` e `cpf` no `context`, e o API Gateway os injeta como `X-Cliente-Id` e `X-Cliente-Cpf`. A aplicação não precisa decodificar o token de novo.
- **Respostas distintas por cenário:** `400` CPF inválido, `404` cliente não encontrado, `403` cliente inativo, `503` banco indisponível.

## Consequências

**Positivas**

- Requisitos de API Gateway, Function Serverless e proteção de rotas por CPF atendidos com uma peça só.
- Tráfego sem token válido é recusado antes de entrar no cluster.
- A API .NET não mudou seu pipeline de autenticação: ela continua validando JWT como já fazia.
- O `authorizer_result_ttl_in_seconds = 300` evita invocar a Lambda em toda requisição do mesmo cliente.

**Trade-offs conscientes**

- **Chave simétrica.** Quem valida também consegue assinar. Aceitável porque emissor e validador pertencem ao mesmo time e à mesma conta AWS. Em um cenário com validadores de terceiros, o correto seria RS256 com JWKS.
- **Sem revogação antes da expiração.** Um token roubado vale até 1 hora. Mitigações: validade curta e HTTPS no gateway. Revogação real exigiria lista de bloqueio consultada a cada requisição, o que anularia a vantagem do JWT.
- **Segredos como variável de ambiente da Lambda.** Ficam visíveis para quem tiver `lambda:GetFunctionConfiguration`. Trade-off consciente da ausência de NAT Gateway.
- **O NLB é `internet-facing`.** A aplicação continua acessível diretamente, sem passar pelo gateway. A alternativa correta seria NLB interno com VPC Link — registrado como trade-off aceito em [ADR 0001](../adrs/0001-comunicacao-entre-repositorios.md) e no README do `garageos-lambda-auth`.
- **Autenticação por CPF sozinha não é forte.** O CPF não é segredo. O escopo é o do enunciado — identificar o cliente já cadastrado para acompanhar a própria OS —, não proteger operação financeira. Um cenário real exigiria segundo fator ou senha.

## Referências

- [Diagrama de sequência — fluxo de autenticação](../diagramas/sequencia-autenticacao.md)
- Implementação: `garageos-lambda-auth/lambda/index.mjs`, `lambda.tf`, `apigateway.tf`
- Enunciado do Tech Challenge — Fase 3, requisitos "API Gateway", "Function Serverless" e "autenticação via CPF"
