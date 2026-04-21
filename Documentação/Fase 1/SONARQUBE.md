# SonarQube - Guia de Configuração

Passo a passo completo para um novo desenvolvedor:

## 0. Instalar o SonarScanner (uma única vez)

Execute este comando para instalar o `dotnet-sonarscanner` como ferramenta global:

```bash
dotnet tool install --global dotnet-sonarscanner
```

Se já tiver instalado e quiser atualizar:

```bash
dotnet tool update --global dotnet-sonarscanner
```

## 1. Acessar o SonarQube

- Abrir http://localhost:9000
- Login com as credenciais de admin

## 2. Criar o projeto

- Projects → Create Project → Create a local project
- **Project key:** `GarageOS` (tem que ser exatamente esse nome)
- **Display name:** qualquer um

## 3. Gerar o token

- Selecionar "Locally" → "Generate a token"
- Copiar o token gerado (começa com `sqp_`)

## 4. Rodar a análise (dentro da pasta `Code/`)

### Passo 1: Iniciar o SonarScanner

```bash
dotnet sonarscanner begin /k:"GarageOS" /d:sonar.host.url="http://localhost:9000" /d:sonar.token="SEU_TOKEN_AQUI" /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml"
```

### Passo 2: Compilar e rodar os testes

```bash
dotnet build GarageOS.slnx && dotnet test GarageOS.UnitTests/GarageOS.UnitTests.csproj --collect:"XPlat Code Coverage" --results-directory ./TestResults -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

### Passo 3: Finalizar o SonarScanner

```bash
dotnet sonarscanner end /d:sonar.token="SEU_TOKEN_AQUI"
```

## ⚠️ Observações importantes

- Cada desenvolvedor gera o próprio token
- A **project key** deve ser a mesma (`GarageOS`) para todos apontarem para o mesmo projeto              