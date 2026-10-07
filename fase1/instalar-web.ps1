# Baixa e compila o ws-scrcpy em fase1\vendor\ws-scrcpy (pasta fora do git).
# - Sem terminal adb (node-pty exige compilador) e sem suporte a iPhone (Appium).
# - Servidor escuta so em 127.0.0.1; acesso remoto e via `tailscale serve`.
$ErrorActionPreference = 'Stop'
$dir = Join-Path $PSScriptRoot 'vendor\ws-scrcpy'

if (-not (Test-Path $dir)) {
    git clone --depth 1 https://github.com/NetrisTV/ws-scrcpy.git $dir
    if ($LASTEXITCODE) { exit 1 }
}

Set-Content -Encoding ascii (Join-Path $dir 'build.config.override.json') @'
{
  "INCLUDE_ADB_SHELL": false,
  "INCLUDE_APPL": false
}
'@

$httpServer = Join-Path $dir 'src\server\services\HttpServer.ts'
$src = Get-Content -Raw $httpServer
$src = $src.Replace('server.listen(port, () =>', "server.listen(port, '127.0.0.1', () =>")
Set-Content -NoNewline -Encoding utf8 $httpServer $src

Push-Location $dir
try {
    npm install --ignore-scripts --omit=optional --no-audit --no-fund
    npm run dist
    Set-Location dist
    npm install --ignore-scripts --omit=optional --no-audit --no-fund
} finally {
    Pop-Location
}
Write-Host 'ws-scrcpy pronto. Rode fase1\iniciar-web.bat' -ForegroundColor Green
