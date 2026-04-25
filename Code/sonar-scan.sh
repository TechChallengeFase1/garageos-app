#!/bin/bash

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}"
echo "╔════════════════════════════════════════╗"
echo "║   SonarQube Scanner for GarageOS       ║"
echo "╚════════════════════════════════════════╝"
echo -e "${NC}"

# Verificar se .env existe na raiz
ENV_FILE="../.env"
if [ ! -f "$ENV_FILE" ]; then
    echo -e "${RED}❌ Erro: arquivo .env não encontrado na raiz!${NC}"
    echo ""
    echo "Passos para configurar:"
    echo "  1. Na raiz do projeto, copie o arquivo de exemplo:"
    echo "     cp .env.example .env"
    echo ""
    echo "  2. Edite .env e preencha SONAR_TOKEN:"
    echo "     - Acesse: http://localhost:9000"
    echo "     - Clique em seu avatar → My Account → Security"
    echo "     - Gere um novo token"
    echo "     - Cole o token em SONAR_TOKEN no .env"
    echo ""
    exit 1
fi

# Carregar variáveis do .env da raiz
export $(cat "$ENV_FILE" | grep -v '^#' | grep -v '^$' | xargs)

# Validar token
if [ -z "$SONAR_TOKEN" ] || [ "$SONAR_TOKEN" = "sqp_seu_token_aqui" ]; then
    echo -e "${RED}❌ Erro: SONAR_TOKEN inválido em .env${NC}"
    echo ""
    echo "Para obter seu token:"
    echo "  1. Acesse: http://localhost:9000"
    echo "  2. Clique em seu avatar → My Account → Security"
    echo "  3. Clique em 'Generate Token'"
    echo "  4. Copie o token (começa com sqp_)"
    echo "  5. Cole em .env na variável SONAR_TOKEN"
    echo ""
    exit 1
fi

# Validar host
if [ -z "$SONAR_HOST_URL" ]; then
    echo -e "${RED}❌ Erro: SONAR_HOST_URL não configurado${NC}"
    exit 1
fi

echo -e "${GREEN}✓${NC} Configurações carregadas"
echo -e "  ${BLUE}Host:${NC} $SONAR_HOST_URL"
echo -e "  ${BLUE}Projeto:${NC} $SONAR_PROJECT_KEY"
echo ""

# Verificar conexão com SonarQube
echo -e "${YELLOW}🔗 Verificando conexão com SonarQube...${NC}"
if ! curl -s -f "$SONAR_HOST_URL/api/system/status" > /dev/null 2>&1; then
    echo -e "${RED}❌ Erro: SonarQube não está respondendo${NC}"
    echo "    Verifique se está rodando em: $SONAR_HOST_URL"
    exit 1
fi
echo -e "${GREEN}✓${NC} Conectado ao SonarQube"
echo ""

# Limpar análise anterior
echo -e "${YELLOW}🧹 Limpando análise anterior...${NC}"
rm -rf .sonarqube
echo -e "${GREEN}✓${NC} Limpeza concluída"
echo ""

# Iniciar SonarScanner
echo -e "${YELLOW}📤 Iniciando SonarScanner...${NC}"
dotnet sonarscanner begin \
  /k:"$SONAR_PROJECT_KEY" \
  /d:sonar.host.url="$SONAR_HOST_URL" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.sources="GarageOS.Domain,GarageOS.Application,GarageOS.Infrastructure,GarageOS.Api" \
  /d:sonar.tests="GarageOS.UnitTests" \
  /d:sonar.exclusions="**/Migrations/**,**/*Designer.cs,**/GarageOSDbContextModelSnapshot.cs,**/.gitkeep,**/*.md,**/bin/**,**/obj/**,**/.sonarqube/**" \
  /d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml"

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Erro ao iniciar SonarScanner${NC}"
    exit 1
fi
echo -e "${GREEN}✓${NC} SonarScanner iniciado"
echo ""

# Compilar
echo -e "${YELLOW}🔨 Compilando projeto...${NC}"
dotnet build GarageOS.slnx
if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Erro na compilação${NC}"
    exit 1
fi
echo -e "${GREEN}✓${NC} Compilação concluída"
echo ""

# Rodar testes
echo -e "${YELLOW}🧪 Executando testes com cobertura...${NC}"
dotnet test GarageOS.UnitTests/GarageOS.UnitTests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Erro ao executar testes${NC}"
    exit 1
fi
echo -e "${GREEN}✓${NC} Testes executados"
echo ""

# Finalizar
echo -e "${YELLOW}📊 Enviando relatório para SonarQube...${NC}"
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"

if [ $? -eq 0 ]; then
    echo ""
    echo -e "${GREEN}╔════════════════════════════════════════╗${NC}"
    echo -e "${GREEN}║  ✅ Análise concluída com sucesso!    ║${NC}"
    echo -e "${GREEN}╚════════════════════════════════════════╝${NC}"
    echo ""
    echo -e "Acesse o relatório em:"
    echo -e "  ${BLUE}$SONAR_HOST_URL/projects${NC}"
    echo ""
else
    echo -e "${RED}❌ Erro ao finalizar análise${NC}"
    exit 1
fi
