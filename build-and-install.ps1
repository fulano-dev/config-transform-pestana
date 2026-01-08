# Script para Build e Instalação da Config Transform Extension
# Execute este script no PowerShell

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Config Transform Extension - Build & Install" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Caminho da solução
$solutionPath = "$PSScriptRoot\ConfigTransformExtension.sln"
$vsixPath = "$PSScriptRoot\ConfigTransformExtension\bin\Debug\ConfigTransformExtension.vsix"

# Verifica se a solução existe
if (-not (Test-Path $solutionPath)) {
    Write-Host "ERRO: Solução não encontrada em $solutionPath" -ForegroundColor Red
    exit 1
}

Write-Host "1. Localizando MSBuild..." -ForegroundColor Yellow

# Tenta encontrar o MSBuild
$msbuildPath = $null
$vswherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"

if (Test-Path $vswherePath) {
    $vsPath = & $vswherePath -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($vsPath) {
        $msbuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
        if (-not (Test-Path $msbuildPath)) {
            $msbuildPath = Join-Path $vsPath "MSBuild\15.0\Bin\MSBuild.exe"
        }
    }
}

if (-not $msbuildPath -or -not (Test-Path $msbuildPath)) {
    Write-Host "ERRO: MSBuild não encontrado. Certifique-se de que o Visual Studio está instalado." -ForegroundColor Red
    Write-Host ""
    Write-Host "Alternativa: Abra a solução no Visual Studio e compile manualmente (Ctrl+Shift+B)" -ForegroundColor Yellow
    exit 1
}

Write-Host "   MSBuild encontrado: $msbuildPath" -ForegroundColor Green
Write-Host ""

Write-Host "2. Restaurando pacotes NuGet..." -ForegroundColor Yellow
& $msbuildPath $solutionPath /t:Restore /p:Configuration=Debug /v:minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Falha ao restaurar pacotes NuGet" -ForegroundColor Red
    exit 1
}
Write-Host "   Pacotes restaurados com sucesso" -ForegroundColor Green
Write-Host ""

Write-Host "3. Compilando solução..." -ForegroundColor Yellow
& $msbuildPath $solutionPath /p:Configuration=Debug /p:DeployExtension=false /v:minimal

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERRO: Falha na compilação" -ForegroundColor Red
    exit 1
}
Write-Host "   Compilação concluída com sucesso" -ForegroundColor Green
Write-Host ""

# Verifica se o VSIX foi gerado
if (-not (Test-Path $vsixPath)) {
    Write-Host "ERRO: Arquivo VSIX não foi gerado em $vsixPath" -ForegroundColor Red
    exit 1
}

Write-Host "4. Arquivo VSIX gerado com sucesso!" -ForegroundColor Green
Write-Host "   Localização: $vsixPath" -ForegroundColor Cyan
Write-Host ""

Write-Host "5. Instalando extensão..." -ForegroundColor Yellow
Write-Host "   IMPORTANTE: Feche todas as instâncias do Visual Studio antes de continuar!" -ForegroundColor Yellow
Write-Host ""

$response = Read-Host "Deseja instalar a extensão agora? (S/N)"
if ($response -eq 'S' -or $response -eq 's') {
    
    # Verifica se há instâncias do VS abertas
    $vsProcesses = Get-Process devenv -ErrorAction SilentlyContinue
    if ($vsProcesses) {
        Write-Host ""
        Write-Host "   AVISO: Detectadas instâncias do Visual Studio em execução!" -ForegroundColor Red
        Write-Host "   Por favor, feche todas as instâncias antes de continuar." -ForegroundColor Yellow
        Write-Host ""
        $forceInstall = Read-Host "Deseja continuar mesmo assim? (S/N)"
        if ($forceInstall -ne 'S' -and $forceInstall -ne 's') {
            Write-Host "Instalação cancelada." -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Para instalar manualmente:" -ForegroundColor Cyan
            Write-Host "   1. Feche o Visual Studio" -ForegroundColor White
            Write-Host "   2. Execute: $vsixPath" -ForegroundColor White
            exit 0
        }
    }
    
    Write-Host "   Iniciando instalação..." -ForegroundColor Yellow
    Start-Process -FilePath $vsixPath -Wait
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Instalação concluída!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Próximos passos:" -ForegroundColor Cyan
    Write-Host "1. Abra o Visual Studio" -ForegroundColor White
    Write-Host "2. Abra um projeto com arquivos .config" -ForegroundColor White
    Write-Host "3. Clique com botão direito em um arquivo de transformação" -ForegroundColor White
    Write-Host "   (ex: web.pestana-hlg2.config)" -ForegroundColor White
    Write-Host "4. Selecione 'Aplicar Transformação'" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "Build concluído com sucesso!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Para instalar manualmente:" -ForegroundColor Cyan
    Write-Host "   1. Feche o Visual Studio" -ForegroundColor White
    Write-Host "   2. Execute: $vsixPath" -ForegroundColor White
    Write-Host ""
}

Write-Host "Pressione qualquer tecla para sair..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
