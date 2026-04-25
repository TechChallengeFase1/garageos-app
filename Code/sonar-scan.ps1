# SonarQube Scanner for GarageOS (PowerShell)
# ==============================================

# Configurar encoding para UTF-8
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

# Cores
$colors = @{
    Red    = "Red"
    Green  = "Green"
    Yellow = "Yellow"
    Blue   = "Cyan"
}

function Write-Header {
    Write-Host ""
    Write-Host "======================================" -ForegroundColor $colors.Blue
    Write-Host "   SonarQube Scanner for GarageOS" -ForegroundColor $colors.Blue
    Write-Host "======================================" -ForegroundColor $colors.Blue
    Write-Host ""
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor $colors.Red
}

function Write-Success {
    param([string]$Message)
    Write-Host "[OK] $Message" -ForegroundColor $colors.Green
}

function Write-Info {
    param([string]$Message)
    Write-Host "$Message" -ForegroundColor $colors.Yellow
}

# ============================================
# VALIDACOES INICIAIS
# ============================================

Write-Header

# Verificar se .env existe na raiz
$envFile = "../.env"

if (-not (Test-Path $envFile)) {
    Write-Error-Custom "Erro: arquivo .env nao encontrado na raiz!"
    Write-Host ""
    Write-Host "Passos para configurar:"
    Write-Host "  1. Na raiz do projeto, copie o arquivo de exemplo:"
    Write-Host "     copy .env.example .env"
    Write-Host ""
    Write-Host "  2. Edite .env e preencha SONAR_TOKEN:"
    Write-Host "     - Acesse: http://localhost:9000"
    Write-Host "     - Clique em seu avatar > My Account > Security"
    Write-Host "     - Gere um novo token"
    Write-Host "     - Cole o token em SONAR_TOKEN no .env"
    Write-Host ""
    exit 1
}

# Carregar variaveis do .env
$envContent = Get-Content $envFile | Where-Object { $_ -notmatch "^#" -and $_ -notmatch "^\s*$" }
foreach ($line in $envContent) {
    if ($line -match "^([^=]+)=(.*)$") {
        $name = $matches[1].Trim()
        $value = $matches[2].Trim()
        [Environment]::SetEnvironmentVariable($name, $value, "Process")
    }
}

# Validar token
if ([string]::IsNullOrEmpty($env:SONAR_TOKEN) -or $env:SONAR_TOKEN -eq "sqp_seu_token_aqui") {
    Write-Error-Custom "Erro: SONAR_TOKEN invalido em .env"
    Write-Host ""
    Write-Host "Para obter seu token:"
    Write-Host "  1. Acesse: http://localhost:9000"
    Write-Host "  2. Clique em seu avatar > My Account > Security"
    Write-Host "  3. Clique em 'Generate Token'"
    Write-Host "  4. Copie o token (comeca com sqp_)"
    Write-Host "  5. Cole em .env na variavel SONAR_TOKEN"
    Write-Host ""
    exit 1
}

# Validar host
if ([string]::IsNullOrEmpty($env:SONAR_HOST_URL)) {
    Write-Error-Custom "Erro: SONAR_HOST_URL nao configurado"
    exit 1
}

Write-Success "Configuracoes carregadas"
Write-Host "  Host: $($env:SONAR_HOST_URL)" -ForegroundColor $colors.Blue
Write-Host "  Projeto: $($env:SONAR_PROJECT_KEY)" -ForegroundColor $colors.Blue
Write-Host ""

# ============================================
# VERIFICAR CONEXAO COM SONARQUBE
# ============================================

Write-Info "Verificando conexao com SonarQube..."
try {
    $response = Invoke-WebRequest -Uri "$($env:SONAR_HOST_URL)/api/system/status" `
                                  -TimeoutSec 5 `
                                  -ErrorAction Stop
    Write-Success "Conectado ao SonarQube"
} catch {
    Write-Error-Custom "Erro: SonarQube nao esta respondendo"
    Write-Host "    Verifique se esta rodando em: $($env:SONAR_HOST_URL)"
    exit 1
}
Write-Host ""

# ============================================
# LIMPEZA
# ============================================

Write-Info "Limpando analise anterior..."
if (Test-Path ".sonarqube") {
    Remove-Item -Recurse -Force ".sonarqube" | Out-Null
}
Write-Success "Limpeza concluida"
Write-Host ""

# ============================================
# INICIAR SONARSCANNER
# ============================================

Write-Info "Iniciando SonarScanner..."
$scannerArgs = @(
    "begin",
    "/k:$($env:SONAR_PROJECT_KEY)",
    "/d:sonar.host.url=$($env:SONAR_HOST_URL)",
    "/d:sonar.token=$($env:SONAR_TOKEN)",
    '/d:sonar.sources="GarageOS.Domain,GarageOS.Application,GarageOS.Infrastructure,GarageOS.Api"',
    '/d:sonar.tests="GarageOS.UnitTests"',
    '/d:sonar.exclusions="**/Migrations/**,**/*Designer.cs,**/GarageOSDbContextModelSnapshot.cs,**/.gitkeep,**/*.md,**/bin/**,**/obj/**,**/.sonarqube/**"',
    '/d:sonar.cs.opencover.reportsPaths="**/TestResults/**/coverage.opencover.xml"'
)

& dotnet sonarscanner @scannerArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Erro ao iniciar SonarScanner"
    exit 1
}
Write-Success "SonarScanner iniciado"
Write-Host ""

# ============================================
# COMPILAR
# ============================================

Write-Info "Compilando projeto..."
& dotnet build GarageOS.slnx
if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Erro na compilacao"
    exit 1
}
Write-Success "Compilacao concluida"
Write-Host ""

# ============================================
# RODAR TESTES
# ============================================

Write-Info "Executando testes com cobertura..."
& dotnet test GarageOS.UnitTests/GarageOS.UnitTests.csproj `
              --collect:"XPlat Code Coverage" `
              --results-directory ./TestResults `
              -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

if ($LASTEXITCODE -ne 0) {
    Write-Error-Custom "Erro ao executar testes"
    exit 1
}
Write-Success "Testes executados"
Write-Host ""

# ============================================
# FINALIZAR
# ============================================

Write-Info "Enviando relatorio para SonarQube..."
& dotnet sonarscanner end /d:sonar.token="$($env:SONAR_TOKEN)"

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "======================================" -ForegroundColor $colors.Green
    Write-Host "  [OK] Analise concluida com sucesso!" -ForegroundColor $colors.Green
    Write-Host "======================================" -ForegroundColor $colors.Green
    Write-Host ""
    Write-Host "Acesse o relatorio em:" -ForegroundColor $colors.Green
    Write-Host "  $($env:SONAR_HOST_URL)/projects" -ForegroundColor $colors.Blue
    Write-Host ""
} else {
    Write-Error-Custom "Erro ao finalizar analise"
    exit 1
}
