# RFC 0001 — Escolha do Provedor de Nuvem

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-08-17 |
| **Autores** | Equipe GarageOS (Tech Challenge — Fase 3) |
| **Repositórios afetados** | `garageos-infra-database`, `garageos-infra-kubernetes`, `garageos-lambda-auth`, `garageos-app` |

## Contexto

O enunciado da Fase 3 exige elevar o GarageOS a um nível de operação corporativa, com infraestrutura provisionada em nuvem, API Gateway, autenticação serverless, banco de dados gerenciado e cluster Kubernetes escalável, tudo via Terraform. A escolha do provedor é livre, mas condiciona diretamente todas as demais decisões técnicas do projeto: o serviço de function serverless a usar, o tipo de API Gateway, a oferta de banco gerenciado e o serviço de Kubernetes gerenciado.

Até este ponto, a infraestrutura do projeto era inteiramente local (cluster `kind` e PostgreSQL rodando dentro do próprio cluster, provisionados por Terraform apenas para o ambiente do runner de CI). Nenhuma decisão de nuvem havia sido tomada.

## Problema

Qual provedor de nuvem (AWS, Azure ou GCP) deve ser adotado como base para toda a infraestrutura do projeto, considerando o prazo da entrega, o nível de familiaridade da equipe e o custo de operação durante o desenvolvimento e a demonstração?

## Opções consideradas

| Opção | A favor | Contra |
|---|---|---|
| **AWS** | Maior familiaridade da equipe com o console, CLI e nomenclatura (EKS, RDS, Lambda, API Gateway); créditos já disponibilizados pela faculdade, eliminando o risco de custo direto do grupo | Custo do control plane do EKS (~US$0,10/h) mesmo com créditos, caso não seja destruído entre sessões de trabalho |
| **Azure** | Também oferece crédito educacional em alguns casos | Nenhum integrante do grupo tem experiência prévia relevante — aumentaria o tempo gasto aprendendo a plataforma em vez de implementando os requisitos, dentro de um prazo já apertado |
| **GCP** | Camada gratuita permanente em alguns serviços | Mesma limitação de familiaridade da equipe; menor exposição prévia ao ecossistema em comparação com AWS |

## Decisão

Adotar a **AWS** como provedor de nuvem para todo o projeto.

Os dois critérios decisivos, nessa ordem de peso, foram:

1. **Maior entendimento/familiaridade dos integrantes** — reduz risco de execução dentro do prazo da fase. Com quatro repositórios para provisionar (banco, Kubernetes, Lambda e aplicação) e uma única API Gateway integrando tudo, gastar tempo aprendendo uma plataforma nova aumentaria significativamente o risco de não concluir os requisitos obrigatórios a tempo.
2. **Créditos provisionados pela faculdade** — remove a barreira de custo para provisionar recursos gerenciados (EKS, RDS, Lambda, API Gateway) durante o desenvolvimento e a gravação do vídeo de demonstração, sem exigir gasto do próprio grupo.

Mapeamento dos requisitos obrigatórios para serviços AWS:

| Requisito do desafio | Serviço AWS |
|---|---|
| API Gateway | Amazon API Gateway (HTTP API) |
| Function Serverless de autenticação | AWS Lambda |
| Banco de Dados Gerenciado | Amazon RDS (PostgreSQL) |
| Cluster Kubernetes com escalabilidade | Amazon EKS + node group gerenciado |
| Terraform para provisionamento | Terraform com provider `aws`, um state por repositório |
| Monitoramento/Observabilidade | Datadog ou New Relic, integrados via Helm (EKS) e via layer/exporter (Lambda) — decisão de ferramenta específica em RFC futura |

## Consequências

**Positivas**
- Curva de aprendizado menor, já que a equipe já navega no console/CLI da AWS.
- Custo de infraestrutura durante o desenvolvimento coberto pelos créditos da faculdade.
- A própria redação do desafio já usa terminologia AWS (“Lambda” é citado literalmente como nome do primeiro repositório exigido), o que facilita o alinhamento entre o enunciado e a implementação.

**Negativas / riscos**
- **Custo residual do EKS**: o control plane cobra por hora independentemente do uso. Mitigação: `terraform destroy` da infraestrutura de Kubernetes fora das janelas de desenvolvimento/demonstração, reprovisionando via pipeline de CD quando necessário.
- **Consumo dos créditos**: créditos educacionais costumam ter validade e teto de consumo. Mitigação: acompanhar o Billing/Cost Explorer periodicamente e preferir instâncias/node groups de menor custo (ex: `t3.medium`) para o cluster.
- **Lock-in em nomenclatura e serviços AWS-específicos** (ex: uso de IAM, VPC, Security Groups) na documentação e no código de infraestrutura. Aceitável para o escopo acadêmico do projeto; não há requisito de portabilidade entre nuvens.

## Decisões desdobradas (fora do escopo desta RFC)

Esta RFC resolve apenas *qual nuvem*. As decisões de *como* implementar cada peça dentro da AWS ficam para RFCs/ADRs específicas, entre elas:

- RFC — Escolha e justificativa do banco gerenciado (RDS PostgreSQL) e ajustes no modelo relacional.
- RFC — Estratégia de autenticação do cliente (CPF via Lambda, JWT).
- ADR — Uso de Lambda Authorizer no API Gateway para validação do JWT (em vez do autorizador JWT nativo, que exige emissor OIDC/JWKS).
- ADR — Lambda dentro da VPC do RDS sem NAT Gateway (Lambda só precisa alcançar recursos internos à VPC).
- ADR — Compartilhamento de outputs entre os states Terraform dos três repositórios de infraestrutura via SSM Parameter Store.

## Referências

- Enunciado do Tech Challenge — Fase 3 (requisitos de infraestrutura obrigatória).
- Discussão interna da equipe sobre autenticação via CPF (esclarecimento do professor).
