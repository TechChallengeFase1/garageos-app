# ADR 0001 — Padrão de Comunicação entre os Repositórios

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-08-27 |
| **Contexto** | Tech Challenge — Fase 3 |
| **Repositórios afetados** | os 4 |
| **Relacionadas** | [RFC 0001](../rfcs/0001-escolha-provedor-cloud.md), [RFC 0002](../rfcs/0002-escolha-banco-de-dados.md), [RFC 0003](../rfcs/0003-estrategia-de-autenticacao.md) |

## Contexto

O enunciado exige que a solução seja dividida em repositórios separados — infraestrutura de banco, infraestrutura de Kubernetes, function serverless e aplicação. Cada um tem seu próprio state do Terraform e sua própria pipeline.

Isso cria um problema concreto: **os repositórios precisam uns dos outros**, mas não enxergam as variáveis nem os recursos uns dos outros.

| Quem precisa | Do quê | De quem |
|---|---|---|
| `garageos-infra-database` | VPC e subnets privadas | bootstrap |
| `garageos-infra-k8s` | VPC, subnets, role do CI, crachá de acesso ao RDS | bootstrap e database |
| `garageos-lambda-auth` | VPC, subnets, crachá, endpoint/credenciais do RDS, segredo da app, Load Balancer | bootstrap, database, k8s e app |
| `garageos-app` | nome do cluster, namespace, ARNs dos segredos | bootstrap e k8s |

Sem um mecanismo, esses valores acabariam escritos à mão nos workflows — e recriar a infraestrutura do zero exigiria editar arquivos em quatro repositórios.

## Decisão

Adotar o **AWS Systems Manager Parameter Store como "quadro de avisos"** entre os repositórios: cada raiz Terraform **publica** o que os outros podem consumir, sob um caminho nomeado, e **lê** o que precisa com o data source `aws_ssm_parameter`.

### O contrato

```text
/garageos/vpc/id                                 bootstrap  →  database, k8s, lambda
/garageos/vpc/cidr                               bootstrap
/garageos/vpc/public-subnet-ids                  bootstrap  →  k8s
/garageos/vpc/private-subnet-ids                 bootstrap  →  database, k8s, lambda
/garageos/iam/github-actions-role-arn            bootstrap  →  k8s
/garageos/terraform/state-bucket                 bootstrap
/garageos/app/secret-arn                         bootstrap  →  app, lambda

/garageos/<env>/rds/endpoint                     database   →  lambda
/garageos/<env>/rds/port                         database   →  lambda
/garageos/<env>/rds/dbname                       database   →  lambda
/garageos/<env>/rds/client-security-group-id     database   →  k8s, lambda
/garageos/<env>/rds/secret-arn                   database   →  app, lambda

/garageos/<env>/eks/cluster-name                 k8s        →  app
/garageos/<env>/eks/endpoint                     k8s
/garageos/<env>/eks/namespace                    k8s        →  app
/garageos/<env>/eks/oidc-provider-arn            k8s        →  app (IRSA)
```

Os caminhos de VPC e IAM **não levam ambiente** porque a rede é compartilhada. Os de RDS e EKS levam, porque `homolog` e `producao` têm banco e cluster distintos.

### Três regras que sustentam o padrão

**1. Nunca publicar segredo, só o ponteiro.**
Senha e chave JWT ficam no Secrets Manager. O que vai para o Parameter Store é o **ARN de onde buscá-los**. O tier padrão do Parameter Store é gratuito; o Secrets Manager cobra por segredo — usar cada um para o que ele serve também é decisão de custo.

**2. A dependência aponta numa direção só.**
`bootstrap → database → k8s → app → lambda`. Se um repositório anterior não tiver sido aplicado, o `plan` do seguinte falha com `ParameterNotFound` — antes de criar qualquer coisa. É melhor falhar no plano do que provisionar um cluster que não consegue falar com nada.

**3. Acesso entre componentes por Security Group, não por IP.**
O repositório do banco cria um SG vazio — o **crachá** (`garageos-<env>-rds-client`) — e o SG do RDS aceita a porta 5432 apenas de quem o carrega. O ID do crachá é publicado no SSM; o `infra-k8s` o anexa aos nós via Launch Template e o `lambda-auth` o anexa a si mesma. Ninguém precisa saber IP de ninguém, e a regra continua correta quando o HPA escala os nós e eles trocam de IP.

### O caso do Load Balancer

Há uma exceção deliberada: o NLB da aplicação é criado pelo **Kubernetes**, não pelo Terraform, então não há como publicá-lo no SSM a partir de uma raiz Terraform. O `garageos-lambda-auth` o descobre por **tag**:

```hcl
data "aws_lb" "api" {
  tags = { "kubernetes.io/service-name" = "garageos/garageos-api" }
}
```

É o mesmo princípio — descoberta em tempo de execução, sem valor escrito à mão —, apenas com outro mecanismo.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **`terraform_remote_state`** lendo o state alheio no S3 | Exigiria permissão de leitura no state inteiro do outro repositório, que traz junto **tudo** que existe lá, incluindo valores sensíveis. Além disso, o contrato ficaria implícito: qualquer output vira dependência acidental. Com o SSM, o que é publicado é exatamente o que outros podem consumir |
| **Um único repositório de infraestrutura** | Contraria o enunciado e cria um state monolítico: um `destroy` do cluster passaria a enxergar o banco |
| **Valores escritos à mão em cada workflow** | Recriar a infraestrutura do zero exigiria editar quatro repositórios. Um rename quebraria o deploy sem aviso |
| **Terragrunt / módulos compartilhados** | Ferramenta a mais para aprender e manter, para resolver um problema que quatro parâmetros do SSM já resolvem |
| **Outputs por arquivo em bucket S3** | Reinventaria o Parameter Store, sem versionamento nem controle de acesso por caminho |

## Consequências

**Positivas**

- Recriar a infraestrutura do zero não exige editar nenhum workflow: tudo é descoberto em tempo de execução.
- O contrato entre repositórios é explícito e legível — dá para listar com `aws ssm get-parameters-by-path --path /garageos --recursive`.
- Falha cedo e com mensagem clara quando a ordem de aplicação é desrespeitada.
- Nenhum valor sensível trafega pelo Parameter Store.
- O acesso ao banco continua correto sob escala automática dos nós.

**Trade-offs conscientes**

- **Acoplamento por convenção de nome.** Renomear um caminho quebra os consumidores, e o Terraform não avisa em tempo de compilação. Mitigação: os caminhos estão documentados aqui e comentados no código que os cria.
- **Ordem de aplicação obrigatória.** Não é possível aplicar os quatro repositórios em paralelo no primeiro provisionamento.
- **`nonsensitive()` espalhado no código.** O provider da AWS marca o valor de *qualquer* parâmetro do SSM como sensível, inclusive `String`. Sem a conversão, qualquer output que os referencie falha com "Output refers to sensitive values" e os IDs apareceriam mascarados no plano, deixando a revisão cega. A precaução faz sentido para `SecureString`; aqui os valores são identificadores públicos.
- **A descoberta do NLB por tag depende de a aplicação já estar implantada.** O `terraform apply` do `garageos-lambda-auth` falha se o Service ainda não criou o Load Balancer — daí a ordem `app` antes de `lambda`.
