# Gera instalador\saida\CelularRemoto-Setup-<versao>.exe
# Precisa do Inno Setup 6:  winget install --id JRSoftware.InnoSetup -e
param([string]$Versao = '0.2.0')
$ErrorActionPreference = 'Stop'

# scrcpy oficial, versao fixa e conferida por hash (traz o adb junto)
$scrcpyVersao = '4.1'
$scrcpySha256 = '5B12172B3264B2889F4583EE64752CE832E29BC8B1089DCA81093459697165DB'
$scrcpyUrl = "https://github.com/Genymobile/scrcpy/releases/download/v$scrcpyVersao/scrcpy-win64-v$scrcpyVersao.zip"

$raiz = Split-Path $PSScriptRoot
$cache = Join-Path $PSScriptRoot '.cache'
$zip = Join-Path $cache "scrcpy-win64-v$scrcpyVersao.zip"
$scrcpyDir = Join-Path $cache "scrcpy-win64-v$scrcpyVersao"

# 1. Compila o app (roda os testes antes; falhou -> para)
& (Join-Path $raiz 'app\build.ps1')

# 2. scrcpy
New-Item -ItemType Directory -Force $cache | Out-Null
if (-not (Test-Path $zip)) {
    Write-Host "Baixando scrcpy $scrcpyVersao..."
    Invoke-WebRequest $scrcpyUrl -OutFile $zip -UseBasicParsing
}
if ((Get-FileHash $zip -Algorithm SHA256).Hash -ne $scrcpySha256) {
    Remove-Item $zip
    throw 'Hash do scrcpy nao confere. Download apagado; rode de novo.'
}
if (-not (Test-Path $scrcpyDir)) { Expand-Archive $zip $cache }

# 3. Inno Setup
$iscc = @(
    (Get-Command iscc -ErrorAction SilentlyContinue).Source,
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe"
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 nao encontrado: winget install --id JRSoftware.InnoSetup -e' }

& $iscc /Q "/DVersao=$Versao" "/DScrcpyDir=$scrcpyDir" (Join-Path $PSScriptRoot 'Ponte.iss')
if ($LASTEXITCODE) { throw 'ISCC falhou' }

$exe = Join-Path $PSScriptRoot "saida\CelularRemoto-Setup-$Versao.exe"
Write-Host "Pronto: $exe" -ForegroundColor Green
Write-Host "SHA256: $((Get-FileHash $exe -Algorithm SHA256).Hash)"
