# Script simples para Build da Config Transform Extension

Write-Host "Compilando Config Transform Extension..." -ForegroundColor Cyan

$msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

if (-not (Test-Path $msbuildPath)) {
    $msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
}

if (-not (Test-Path $msbuildPath)) {
    $msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
}

if (-not (Test-Path $msbuildPath)) {
    Write-Host "ERRO: MSBuild não encontrado" -ForegroundColor Red
    Write-Host "Use o Visual Studio Developer Command Prompt ou execute build-and-install.ps1" -ForegroundColor Yellow
    exit 1
}

& $msbuildPath "$PSScriptRoot\ConfigTransformExtension.sln" /t:Restore /p:Configuration=Debug
& $msbuildPath "$PSScriptRoot\ConfigTransformExtension.sln" /p:Configuration=Debug /p:DeployExtension=false

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "Build concluído com sucesso!" -ForegroundColor Green
    Write-Host "VSIX gerado em: ConfigTransformExtension\bin\Debug\ConfigTransformExtension.vsix" -ForegroundColor Cyan
} else {
    Write-Host "Falha no build" -ForegroundColor Red
}
