# ADR 0003 — Exposição do Load Balancer da Aplicação

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-08-31 |
| **Contexto** | Tech Challenge — Fase 3 |
| **Repositórios afetados** | `garageos-app`, `garageos-lambda-auth` |
| **Relacionadas** | [RFC 0003](../rfcs/0003-estrategia-de-autenticacao.md), [ADR 0001](0001-comunicacao-entre-repositorios.md) |

## Contexto

A [RFC 0003](../rfcs/0003-estrategia-de-autenticacao.md) estabelece o API Gateway como **porta de entrada única**: uma requisição sem token válido é recusada pelo Lambda authorizer, na borda, antes de consumir um pod.

Só que o Network Load Balancer da aplicação é criado pelo **Service do Kubernetes**, com:

```yaml
service.beta.kubernetes.io/aws-load-balancer-scheme: "internet-facing"
```

Ou seja, ele tem nome DNS público e aceita tráfego direto. Quem descobrir esse hostname alcança a API **sem passar pelo gateway** — e o authorizer deixa de ser fronteira obrigatória.

Esse trade-off já estava anotado em três lugares (`garageos-lambda-auth/apigateway.tf`, o README daquele repositório e a RFC 0003), sempre remetendo a um ADR que ainda não existia. Este documento fecha essa lacuna.

## Problema

O NLB deve ser interno — alcançável apenas de dentro da VPC, com o API Gateway chegando até ele por **VPC Link** — ou permanece `internet-facing`, com a aplicação se defendendo sozinha?

## Decisão

Manter o NLB **`internet-facing`**, tendo como controle compensatório a **validação do mesmo JWT pela própria API .NET**.

Três razões concretas, todas verificáveis no código atual:

**1. O smoke test do deploy deixaria de funcionar.**
A pipeline de CD espera o hostname do Service e faz `curl` em `/health/ready` para provar que a aplicação está *atendendo*, e não apenas que os pods subiram. O runner do GitHub Actions está fora da VPC: com NLB interno, esse passo não teria como alcançar a aplicação e o deploy perderia sua verificação de ponta a ponta.

**2. A integração do gateway aponta para o DNS do NLB.**
O `garageos-lambda-auth` descobre o Load Balancer por tag e monta uma integração `HTTP_PROXY` para `http://<dns>/{proxy}`. Integração `HTTP_PROXY` para um nome privado não resolve a partir do gateway — seria necessário trocar o tipo da integração e introduzir o recurso de VPC Link.

**3. Os nós já estão em subnets públicas.**
O bootstrap dispensou o NAT Gateway (economia de ~US$ 32/mês) e colocou os nós em subnets públicas, protegidos por Security Group. Um NLB interno seria coerente com uma topologia que o projeto deliberadamente não tem.

### O que o controle compensatório cobre — e o que não cobre

A API .NET valida o JWT por conta própria (`AddJwtAuthentication`), com o mesmo `issuer`, `audience` e chave HS256 que a Lambda usa para assinar. Uma requisição direta ao NLB **sem token válido recebe `401` da aplicação**.

| O que se perde ao contornar o gateway | Impacto |
|---|---|
| Rejeição na borda | A requisição inválida consome um pod e abre conexão de log antes de ser recusada |
| Throttling (`100` burst / `50` rps) | Não se aplica a quem vai direto |
| Log de acesso centralizado do gateway | A requisição não aparece no `/aws/apigateway/garageos-producao` |
| Injeção de `X-Cliente-Id` e `X-Cliente-Cpf` | Ausentes; a aplicação não os recebe prontos |

**O que não se perde é a autenticação.** É essa a fronteira que continua de pé, e é por isso que a redundância de validação na API não é excesso de zelo — é o que sustenta esta decisão.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **NLB interno + VPC Link** | É a resposta correta para produção real. Exige um `aws_apigatewayv2_vpc_link` com ENIs nas subnets privadas, troca do tipo de integração, e quebra o smoke test da pipeline como está hoje. Custo em complexidade desproporcional ao ganho num projeto acadêmico cujo cluster sobe e desce a cada sessão |
| **Security Group no NLB restringindo à origem do gateway** | O API Gateway HTTP API não publica faixa de IP estável e própria para *allowlist*. A regra teria de liberar faixas amplas da AWS, o que não fecha nada de fato |
| **Manter público e remover a validação da API** | Deixaria a aplicação sem defesa alguma para quem descobrisse o hostname. É exatamente o cenário que a redundância evita |
| **Ingress/ALB com WAF na frente** | Outro componente para instalar (AWS Load Balancer Controller) e manter, resolvendo um problema que o enunciado não levanta |

## Consequências

**Positivas**

- O deploy continua verificável de ponta a ponta, e a falha aparece na pipeline em vez de na demonstração.
- Nenhum recurso adicional de rede para provisionar, manter ou destruir.
- A aplicação é segura por si mesma, sem depender de o gateway ser a única rota — propriedade que sobrevive a qualquer mudança de topologia.

**Trade-offs conscientes**

- **O gateway não é fronteira obrigatória.** É ponto de controle preferencial, não exclusivo. A distinção precisa ficar explícita para quem ler o diagrama de componentes e assumir o contrário.
- **Throttling e log de acesso valem só para quem passa pelo gateway.** Um cliente direto não é limitado nem registrado ali.
- **Migrar para NLB interno depois toca três lugares:** a anotação do Service, o passo de smoke test da CD e o `data "aws_lb"` do `garageos-lambda-auth`. Registrado aqui para que a mudança não seja descoberta no meio do caminho.

## Referências

- Implementação: `k8s/api-service.yaml` (anotação de scheme), `garageos-lambda-auth/apigateway.tf` (integração `HTTP_PROXY`), `.github/workflows/cd.yml` (smoke test)
- [RFC 0003 — Estratégia de Autenticação](../rfcs/0003-estrategia-de-autenticacao.md)
- [Diagrama de sequência — autenticação](../diagramas/sequencia-autenticacao.md)
