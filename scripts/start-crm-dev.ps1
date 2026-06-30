param(
    [string]$ProjectRoot = (Resolve-Path "$PSScriptRoot\..").Path,
    [string]$ApiUrl = "http://localhost:5026"
)

$ErrorActionPreference = "Stop"

$apiProject = Join-Path $ProjectRoot "backend\AutoPartsCrm.Api\AutoPartsCrm.Api.csproj"
$desktopProject = Join-Path $ProjectRoot "desktop"

if (-not (Test-Path $apiProject)) {
    throw "Projeto da API nao encontrado em: $apiProject"
}

if (-not (Test-Path $desktopProject)) {
    throw "Projeto Flutter nao encontrado em: $desktopProject"
}

Write-Host "Iniciando API em $ApiUrl..."
$apiProcess = Start-Process `
    -FilePath "dotnet" `
    -ArgumentList @("run", "--project", $apiProject, "--urls", $ApiUrl) `
    -WorkingDirectory $ProjectRoot `
    -WindowStyle Hidden `
    -PassThru

$ready = $false
for ($attempt = 1; $attempt -le 30; $attempt++) {
    try {
        Invoke-RestMethod -Uri "$ApiUrl/health" -TimeoutSec 2 | Out-Null
        $ready = $true
        break
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

if (-not $ready) {
    Stop-Process -Id $apiProcess.Id -ErrorAction SilentlyContinue
    throw "A API nao ficou pronta. Confira se o PostgreSQL esta aberto e se o banco autoparts_crm_dev existe."
}

Write-Host "API pronta. Abrindo app Windows..."
Push-Location $desktopProject
try {
    flutter run -d windows
}
finally {
    Pop-Location
    Stop-Process -Id $apiProcess.Id -ErrorAction SilentlyContinue
}
