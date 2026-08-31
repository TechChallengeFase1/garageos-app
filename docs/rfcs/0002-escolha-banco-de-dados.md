# RFC 0002 — Escolha do Banco de Dados

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 2026-08-27 |
| **Autores** | Equipe GarageOS (Tech Challenge — Fase 3) |
| **Repositórios afetados** | `garageos-infra-database`, `garageos-app`, `garageos-lambda-auth` |
| **Relacionadas** | [RFC 0001 — Escolha do Provedor de Nuvem](0001-escolha-provedor-cloud.md) |

## Contexto

A Fase 3 exige que o banco de dados deixe de ser um container gerenciado pelo próprio time (um `StatefulSet` de PostgreSQL dentro do cluster `kind`) e passe a ser um **banco gerenciado pelo provedor de nuvem**, provisionado por Terraform.

Isso obriga a revisitar duas decisões que até então estavam implícitas:

1. **Qual paradigma** — o modelo de dados do GarageOS continua fazendo sentido em um banco relacional?
2. **Qual serviço gerenciado** — dentro da AWS (decidida na RFC 0001), qual oferta atende ao domínio dentro do orçamento de créditos?

O modelo de dados atual já está implementado com Entity Framework Core, com 6 migrations aplicadas e um conjunto de testes de integração que roda contra um PostgreSQL real via Testcontainers.

## Problema

Qual banco de dados adotar para a persistência do GarageOS na nuvem, e como justificá-lo diante da natureza do domínio — clientes, veículos, ordens de serviço, serviços executados, peças consumidas e orçamentos?

## Análise do domínio

O domínio do GarageOS é fortemente relacional. As entidades não existem isoladas: uma OS **só faz sentido** ligada a um cliente e a um veículo, um item de serviço **só existe** dentro de uma OS, e a baixa de estoque **precisa** estar amarrada à peça consumida em uma OS específica.

| Relação | Cardinalidade | Consequência |
|---|---|---|
| Cliente → Veículo | 1:N | Veículo sem dono válido é dado órfão |
| Cliente → OS | 1:N | Restrição de exclusão: não se apaga cliente que tem OS |
| Veículo → OS | 1:N | Mesma restrição |
| OS → Serviço | N:N (via `OrdensDeServicoServicos`) | Cada vínculo carrega estado próprio (`Criada`, `Iniciado`, `Finalizado`) e timestamps |
| OS → Estoque | N:N (via `OrdensDeServicoEstoques`) | Cada vínculo carrega a quantidade consumida |
| OS → Orçamento | 1:1 | Orçamento não existe sem OS (exclusão em cascata) |

Além das relações, três requisitos funcionais empurram na mesma direção:

- **Aging das OS** — cálculo de tempo médio por status, que é agregação sobre `IniciadaEm`/`FinalizadaEm` dos itens de serviço. É `GROUP BY` com `AVG` sobre diferença de timestamps: consulta trivial em SQL.
- **Numeração sequencial de OS** — o formato `OS-2026-00001` exige ler o último sequencial do ano e incrementar. Depende de leitura consistente, não de consistência eventual.
- **Integridade referencial** — o caso de uso de abertura de OS valida cliente, veículo, serviços e peças antes de gravar. As foreign keys são a segunda linha de defesa, aplicada pelo próprio banco.

## Opções consideradas

| Opção | A favor | Contra |
|---|---|---|
| **Amazon RDS PostgreSQL** | Modelo relacional casa com o domínio; a aplicação já roda em PostgreSQL 16 desde a Fase 1 (migrations, mappings e testes de integração prontos); elegível ao free tier em `db.t4g.micro`; backup, patch e failover gerenciados pela AWS | Instância única é ponto de falha com `multi_az = false`; custo passa a existir fora do free tier |
| **Amazon Aurora Serverless v2 (PostgreSQL)** | Escala automática de capacidade; alta disponibilidade nativa | Custo mínimo de 0,5 ACU cobrada continuamente (~US$ 43/mês), sem free tier — consumiria quase metade do crédito só com o banco. Ganho irrelevante na carga do projeto |
| **Amazon DynamoDB** | Free tier generoso; escala horizontal sem gerenciar instância | Exigiria reescrever domínio, repositórios e migrations. As relações N:N com estado próprio (item de serviço com status e timestamps) e o cálculo de aging viram *scans* e desnormalização manual. O ganho de escala não é necessário no volume de uma oficina |
| **Amazon DocumentDB / MongoDB** | Flexibilidade de esquema | Mesmo custo de reescrita, sem vantagem no domínio: o esquema do GarageOS é estável e conhecido, e é justamente a rigidez que protege a integridade dos dados |

## Decisão

Adotar o **Amazon RDS for PostgreSQL 16**, na classe `db.t4g.micro`, Single-AZ, com 20 GB de armazenamento `gp2` criptografado, provisionado pelo Terraform no repositório `garageos-infra-database`.

Três critérios, nessa ordem de peso:

1. **Aderência do modelo relacional ao domínio.** As regras do negócio são relações com restrições. Um banco relacional torna essas restrições declarativas e verificadas pelo próprio SGBD, em vez de dependentes de disciplina no código da aplicação.
2. **Continuidade com o que já existe.** O PostgreSQL já é o banco do projeto desde a Fase 1, por ser open source, sem custo de licenciamento, amplamente consolidado e por já haver experiência prévia da equipe. Trocar o paradigma significaria reescrever `GarageOS.Infrastructure` inteiro e descartar a suíte de testes de integração — risco desnecessário dentro do prazo da fase.
3. **Custo.** `db.t4g.micro` é elegível ao free tier (750 h/mês de uma instância nos primeiros 12 meses da conta). O Aurora custaria cerca de metade do crédito disponível apenas para ficar ligado.

### Configuração escolhida e por quê

| Parâmetro | Valor | Justificativa |
|---|---|---|
| `engine_version` | `16` (só o major) | A AWS aplica os *minors* sozinha, sem gerar diff no Terraform |
| `instance_class` | `db.t4g.micro` | Free tier; ARM (Graviton) tem melhor custo/desempenho que `t3` |
| `storage_type` | `gp2` | O free tier cobre "General Purpose (SSD) gp2"; `gp3` não |
| `storage_encrypted` | `true` | Criptografia em repouso, sem custo adicional |
| `multi_az` | `false` | Dobraria o custo e sairia do free tier — **trade-off consciente**, ver Consequências |
| `publicly_accessible` | `false` | O banco vive nas subnets privadas, sem rota para a internet |
| `backup_retention_period` | 1 dia (produção) / 0 (homolog) | Ambiente descartável, recriado a cada janela de trabalho |
| Performance Insights | desligado | Fora do free tier; o monitoramento do projeto é feito pelo New Relic ([RFC 0004](0004-ferramenta-de-observabilidade.md)) |

### Acesso e credenciais

A senha do master **nunca é escrita por uma pessoa nem versionada**: o Terraform sorteia com `random_password` (32 caracteres, sem `/`, `"`, `@` ou espaço, que o RDS rejeita) e grava no **AWS Secrets Manager** como JSON, incluindo uma `connectionString` já no formato do Npgsql, pronta para a API .NET e para a Lambda.

O acesso de rede usa o padrão de **dois Security Groups**:

```text
garageos-<env>-rds-client   "crachá" vazio, sem regra de entrada
garageos-<env>-rds          SG do banco: aceita 5432 SOMENTE de quem tem o crachá
```

O ID do crachá é publicado no SSM Parameter Store e anexado aos nós do EKS e à Lambda de autenticação. Nenhuma regra por faixa de IP: os nós escalam pelo HPA e trocam de IP, e uma regra por CIDR ficaria larga demais. Ver [ADR 0001](../adrs/0001-comunicacao-entre-repositorios.md).

## Consequências

**Positivas**

- Integridade referencial garantida pelo banco, não apenas pelo código.
- Zero reescrita: migrations, repositórios e testes de integração continuam válidos.
- Consultas de aging e relatórios gerenciais são SQL direto, sem agregação em memória.
- Backup automático, patch de segurança e restauração *point-in-time* passam a ser responsabilidade da AWS.
- O banco deixa de ter estado dentro do cluster: destruir o EKS não destrói dados.

**Trade-offs conscientes**

- **Single-AZ é ponto único de falha.** Uma falha de zona derruba o banco até a AWS restaurá-lo. Mitigação para produção real: `multi_az = true`, com o custo dobrado. Registrado aqui como decisão consciente de projeto acadêmico com orçamento limitado.
- **A senha em texto puro fica no state do Terraform.** Não existe recurso no Terraform que evite isso. A defesa é proteger o state: bucket S3 privado, versionado e criptografado, e `*.tfstate` bloqueado no `.gitignore`.
- **O free tier cobre uma instância por vez.** Rodar `homolog` e `producao` simultaneamente passa de 1400 h/mês e o excedente é cobrado (~US$ 12/mês). Mitigação: destruir o ambiente de homologação quando não estiver em uso.
- **`recovery_window_in_days = 0` no segredo.** Sem janela de recuperação, um segredo apagado por engano some na hora. Foi escolhido porque o padrão de 30 dias reserva o *nome* do segredo e faria `destroy` seguido de `apply` falhar com "already scheduled for deletion" — inviável num ambiente recriado a cada sessão. Em produção real seria 7 ou 30.
- **`skip_final_snapshot = true` e `deletion_protection = false`.** Permitem `terraform destroy` sem intervenção manual. Em produção real seria exatamente o oposto.

## Referências

- Modelo de dados detalhado: [Diagrama ER](../diagramas/modelo-er.md)
- Implementação: `garageos-infra-database/rds.tf`, `secrets.tf`, `security-groups.tf`
- Enunciado do Tech Challenge — Fase 3, requisito "Banco de Dados Gerenciado"
