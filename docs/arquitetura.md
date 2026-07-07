# GarageOS — Desenho da Arquitetura Proposta

Componentes da aplicação (Clean Architecture) e infraestrutura provisionada (Kubernetes) para a API de gestão de oficina automotiva.

> Branch `main` · Runtime .NET 10 / ASP.NET Core · Banco PostgreSQL 16 · Orquestração Kubernetes

![Desenho da arquitetura do GarageOS](./arquitetura.png)

---

## 1. Componentes da Aplicação

Estrutura em Clean Architecture: as camadas externas dependem das internas, nunca o contrário
(`Api → Application / Infrastructure → Domain`). O `Domain` não conhece nenhuma das demais camadas.

```mermaid
flowchart TB
    subgraph API["GarageOS.Api — camada de entrada"]
        A1["AuthController"]
        A2["ClientesController"]
        A3["VeiculosController"]
        A4["ServicosController"]
        A5["EstoquesController"]
        A6["OrdensDeServicoController"]
        A7["Swagger UI"]
    end

    subgraph APP["GarageOS.Application — casos de uso"]
        P1["Use Cases"]
        P2["DTOs"]
        P3["FluentValidation"]
    end

    subgraph INFRA["GarageOS.Infrastructure — implementações técnicas"]
        I1["GarageOSDbContext (EF Core)"]
        I2["Repositórios"]
        I3["Migrations"]
    end

    subgraph DOMAIN["GarageOS.Domain — núcleo (sem dependências externas)"]
        D1["Entidades"]
        D2["Value Objects"]
        D3["Interfaces de Repositório"]
        D4["Exceções de Domínio"]
    end

    API -->|depende de| APP
    API -->|depende de| INFRA
    APP -->|implementa| DOMAIN
    INFRA -.->|inversão de dependência| DOMAIN
```

**Cobertura de testes**

| Projeto | Arquivos | Ferramentas | Cobre |
|---|---|---|---|
| `GarageOS.UnitTests` | 49 | xUnit + Moq | Use Cases, Validators, Domain |
| `GarageOS.IntegrationTests` | 9 | Testcontainers | Controllers e Repositórios contra Postgres real |

---

## 2. Infraestrutura Provisionada

Produção roda em um cluster Kubernetes: a imagem publicada no Docker Hub é implantada como Deployment com
autoscaling, exposta via Service NodePort, e persiste dados em um StatefulSet PostgreSQL com volume dedicado.

```mermaid
flowchart TB
    Cliente["Cliente / Navegador"]
    DockerHub["Docker Hub\ngarageosfiap/garageos-api:latest"]

    subgraph NS["Namespace: garageos"]
        Svc1["Service — garageos-api-service\n(NodePort 30080)"]

        subgraph Dep["Deployment — garageos-api (2 réplicas base)"]
            Pod1["Pod 1/2"]
            Pod2["Pod 2/2"]
            HPA["HPA\nescala 2 → 10 réplicas @ 70% CPU"]
            CM["ConfigMap\nvariáveis não sensíveis"]
            Sec["Secret\ncredenciais banco/JWT/admin (base64)"]
        end

        Svc2["Service — postgres-service\n(ClusterIP)"]

        subgraph STS["StatefulSet — postgres (PostgreSQL 16)"]
            PVC["PVC\nvolume persistente"]
        end
    end

    Cliente -->|HTTP :30080| Svc1
    Svc1 -->|roteia para| Dep
    Dep -->|Npgsql :5432| Svc2
    Svc2 -->|roteia para| STS
    STS --- PVC
    DockerHub -.->|pull de imagem| Dep
    HPA -.->|monitora CPU| Dep
    CM -.->|injeta env| Dep
    Sec -.->|injeta env| Dep
```

**Legenda**: seta sólida = fluxo de requisição/dados · seta tracejada = configuração, montagem ou pull externo.

---

## Observações

- Ambiente local de desenvolvimento usa `docker-compose.yml` (postgres, pgadmin, api, sonarqube-db, sonarqube) em vez do cluster K8s — não representado aqui por ser um ambiente à parte.
- As probes do Deployment são TCP puras porque a API ainda não expõe um endpoint `/health`; um readiness/liveness HTTP traria diagnóstico mais preciso.
- O Secret do K8s guarda os valores em base64 (não criptografado) — considerar um cofre externo (ex. Sealed Secrets, Vault) antes de expor o repositório publicamente.
