# Documentação do GarageOS

Índice central da documentação de arquitetura da solução. Os quatro repositórios do projeto apontam para cá.

---

## RFCs — decisões de tecnologia

Uma RFC responde **"o quê e por quê"**: registra o problema, as opções avaliadas, o critério de escolha e as consequências aceitas.

| # | Título | Status | Decisão |
|---|---|---|---|
| [0001](rfcs/0001-escolha-provedor-cloud.md) | Escolha do provedor de nuvem | Aceito | AWS |
| [0002](rfcs/0002-escolha-banco-de-dados.md) | Escolha do banco de dados | Aceito | Amazon RDS PostgreSQL 16 |
| [0003](rfcs/0003-estrategia-de-autenticacao.md) | Estratégia de autenticação | Aceito | CPF em Lambda, JWT HS256, Lambda Authorizer |
| [0004](rfcs/0004-ferramenta-de-observabilidade.md) | Ferramenta de observabilidade | Aceito | New Relic |

## ADRs — decisões de arquitetura

Um ADR responde **"como"**: registra uma decisão estrutural interna, com as alternativas descartadas e o custo de manter a escolha.

| # | Título | Status | Decisão |
|---|---|---|---|
| [0001](adrs/0001-comunicacao-entre-repositorios.md) | Padrão de comunicação entre os repositórios | Aceito | SSM Parameter Store como contrato, Security Group como crachá |
| [0002](adrs/0002-uso-do-hpa.md) | Uso do Horizontal Pod Autoscaler | Aceito | HPA v2 por CPU, 2–10 réplicas, com 4 pré-condições |
| [0003](adrs/0003-exposicao-do-load-balancer.md) | Exposição do Load Balancer da aplicação | Aceito | NLB `internet-facing`, com validação do JWT na própria API |

## Diagramas

| Diagrama | Mostra |
|---|---|
| [Componentes](diagramas/componentes.md) | Infraestrutura AWS, repositórios, camadas da aplicação e fluxo de CI/CD |
| [Sequência — autenticação](diagramas/sequencia-autenticacao.md) | CPF → JWT, validação no gateway e a chave compartilhada |
| [Sequência — abertura de OS](diagramas/sequencia-abertura-os.md) | Abertura, orçamento, aprovação, ciclo de status e acompanhamento |
| [Modelo ER](diagramas/modelo-er.md) | Tabelas, relações, decisões de modelagem e justificativa do banco |
| [Arquitetura (Fase 2)](arquitetura.md) | Desenho anterior, mantido como histórico |

---

## Mapa dos requisitos da Fase 3

| Requisito do enunciado | Onde está | Documento |
|---|---|---|
| Terraform para provisionamento | `bootstrap/`, `garageos-infra-database`, `garageos-infra-k8s`, `garageos-lambda-auth` | [RFC 0001](rfcs/0001-escolha-provedor-cloud.md) |
| Deploy automático para a nuvem | GitHub Actions com OIDC, sem credencial estática | [ADR 0001](adrs/0001-comunicacao-entre-repositorios.md) |
| Banco de dados gerenciado | Amazon RDS PostgreSQL | [RFC 0002](rfcs/0002-escolha-banco-de-dados.md) |
| Infraestrutura do banco com Terraform | `garageos-infra-database` | [RFC 0002](rfcs/0002-escolha-banco-de-dados.md) |
| Cluster Kubernetes com escalabilidade | Amazon EKS + HPA + `metrics-server` | [ADR 0002](adrs/0002-uso-do-hpa.md) |
| API Gateway | Amazon API Gateway HTTP API | [RFC 0003](rfcs/0003-estrategia-de-autenticacao.md) |
| Function Serverless | AWS Lambda (auth + authorizer) | [RFC 0003](rfcs/0003-estrategia-de-autenticacao.md) |
| Autenticação por CPF em rotas sensíveis | Lambda + Lambda Authorizer | [Sequência de autenticação](diagramas/sequencia-autenticacao.md) |
| Health checks | `/health/live` e `/health/ready` | [ADR 0002](adrs/0002-uso-do-hpa.md) |
| Logs estruturados | Serilog em JSON + correlation ID | [RFC 0004](rfcs/0004-ferramenta-de-observabilidade.md) |
| Monitoramento de CPU e memória do cluster | New Relic `nri-bundle` (`garageos-infra-k8s/newrelic.tf`) | [RFC 0004](rfcs/0004-ferramenta-de-observabilidade.md) |
| Dashboards de negócio e alerta | Custom events via `IMetricasDeNegocio`, NRQL registradas na RFC | [RFC 0004](rfcs/0004-ferramenta-de-observabilidade.md) |

---

## Repositórios

| Repositório | Responsabilidade |
|---|---|
| [`garageos-app`](https://github.com/TechChallengeFase1/garageos-app) | API .NET, manifestos Kubernetes, CI/CD da aplicação e documentação central |
| [`garageos-infra-database`](https://github.com/TechChallengeFase1/garageos-infra-database) | RDS PostgreSQL e o `bootstrap/` (state, OIDC, VPC, segredos compartilhados) |
| [`garageos-infra-k8s`](https://github.com/TechChallengeFase1/garageos-infra-k8s) | Cluster EKS, node group, Access Entries, namespace, `metrics-server` e `nri-bundle` |
| [`garageos-lambda-auth`](https://github.com/TechChallengeFase1/garageos-lambda-auth) | Lambda de autenticação por CPF e API Gateway |

## Convenções

- **RFC** para escolha de tecnologia; **ADR** para decisão estrutural interna.
- Numeração sequencial, sem reaproveitar número.
- Status possíveis: `Proposto`, `Aceito`, `Substituído por #NNNN`, `Descontinuado`.
- Uma decisão aceita **não é editada** para mudar de rumo: escreve-se uma nova que a substitua, preservando o histórico.
- Diagramas em **Mermaid**, dentro do Markdown, para que sejam revisáveis em diff e renderizem direto no GitHub.
