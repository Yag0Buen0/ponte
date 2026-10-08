# Compila o Celular Remoto com o compilador C# que vem no Windows (.NET Framework 4.8).
#   -SoTestes  compila e roda so os testes
# Saida: app\bin\CelularRemoto.exe e app\bin\Testes.exe
param([switch]$SoTestes)
$ErrorActionPreference = 'Stop'

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw '.NET Framework 4.8 nao encontrado (csc.exe)' }

$src = Join-Path $PSScriptRoot 'src'
$bin = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force $bin | Out-Null

$refs = '/reference:System.Windows.Forms.dll', '/reference:System.Drawing.dll', '/reference:System.Runtime.Serialization.dll'
# Partes puras: entram no app e nos testes
$puros = 'Opcoes.cs', 'SaidaAdb.cs', 'ConfigArquivo.cs', 'Mapeador.cs' |
    ForEach-Object { Join-Path $src $_ } | Where-Object { Test-Path $_ }

function Invoke-Csc([string[]]$argumentos) {
    & $csc /nologo /utf8output /codepage:65001 /optimize+ @refs @argumentos
    if ($LASTEXITCODE) { throw 'Falha ao compilar' }
}

Invoke-Csc (@('/target:exe', "/out:$bin\Testes.exe") + $puros + (Join-Path $PSScriptRoot 'tests\Testes.cs'))
& "$bin\Testes.exe"
if ($LASTEXITCODE) { throw "$LASTEXITCODE teste(s) falharam" }
if ($SoTestes) { return }

$icone = Join-Path $PSScriptRoot 'icone.ico'
$opcaoIcone = if (Test-Path $icone) { @("/win32icon:$icone") } else { @() }
Invoke-Csc (@('/target:winexe', "/out:$bin\CelularRemoto.exe") + $opcaoIcone + (Get-ChildItem $src -Filter *.cs).FullName)
Write-Host "Pronto: $bin\CelularRemoto.exe" -ForegroundColor Green
