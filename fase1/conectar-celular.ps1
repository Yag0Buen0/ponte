# Conecta o celular via ADB por Wi-Fi (descoberta mDNS) e abre o scrcpy.
# Modos:
#   (todos: janela redimensionavel para qualquer tamanho)
#   normal       janela comum, tela fisica do celular apagada
#   pip          janela pequena, sempre por cima, abre no canto inferior direito
#   deitado      tela virtual 1920x1080 (apps/videos deitados), celular segue livre
#   pip-deitado  tela virtual 1280x720 em janela pequena sempre por cima (video)
param(
    [ValidateSet('normal', 'pip', 'deitado', 'pip-deitado')]
    [string]$Modo = 'normal'
)
$ErrorActionPreference = 'Stop'

# Mostra o erro no terminal e numa caixa de aviso (atalhos do Iniciar rodam sem janela)
function Stop-ComErro($msg) {
    Write-Host $msg -ForegroundColor Red
    Add-Type -AssemblyName System.Windows.Forms
    [System.Windows.Forms.MessageBox]::Show($msg, 'Celular', 'OK', 'Warning') | Out-Null
    exit 1
}

function Get-AdbDevices {
    adb devices | Select-Object -Skip 1 |
        Where-Object { $_ -match "`tdevice$" } |
        ForEach-Object { ($_ -split "`t")[0] }
}

function Find-CelularNaRede {
    adb mdns services |
        Where-Object { $_ -match '_adb-tls-connect\._tcp' } |
        ForEach-Object { if ($_ -match '(\d{1,3}(\.\d{1,3}){3}:\d+)') { $Matches[1] } } |
        Select-Object -First 1
}

# Opcoes de janela: tamanho fixo encostado no canto inferior direito da tela principal
function Get-JanelaNoCanto($largura, $altura) {
    Add-Type -AssemblyName System.Windows.Forms
    $area = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    $margem = 16
    $barraDeTitulo = 32
    @(
        "--window-width=$largura", "--window-height=$altura",
        "--window-x=$($area.Right - $largura - $margem)",
        "--window-y=$($area.Bottom - $altura - $margem - $barraDeTitulo)"
    )
}

# Janela redimensionavel livremente em todos os modos:
# - espelho (normal/pip): imagem estica para preencher a janela
# - tela virtual (deitado): --flex-display faz a tela virtual acompanhar a janela, sem distorcer
function Get-OpcoesDoModo($modo) {
    $espelho = @('--turn-screen-off', '--no-window-aspect-ratio-lock', '--render-fit=stretched')
    $virtual = @('--flex-display', '--no-window-aspect-ratio-lock')
    switch ($modo) {
        'normal'      { $espelho }
        'pip'         { $espelho + @('--always-on-top') + (Get-JanelaNoCanto 240 520) }
        'deitado'     { $virtual + @('--new-display=1920x1080') }
        'pip-deitado' { $virtual + @('--new-display=1280x720', '--always-on-top') + (Get-JanelaNoCanto 480 270) }
    }
}

adb start-server | Out-Null
Start-Sleep -Seconds 2   # da tempo do adb reconectar sozinho via mDNS
$devices = @(Get-AdbDevices)

if ($devices.Count -eq 0) {
    Write-Host 'Procurando o celular na rede...'
    $alvo = Find-CelularNaRede
    if (-not $alvo) {
        Stop-ComErro 'Celular nao encontrado. Confira: mesma rede Wi-Fi e "Wireless debugging" ligado.'
    }
    adb connect $alvo | Out-Host
    $devices = @(Get-AdbDevices)
}

if ($devices.Count -eq 0) { Stop-ComErro 'Nao consegui conectar ao celular.' }

Write-Host "Conectado: $($devices[0]) (modo: $Modo)"
# Celular dormindo = imagem preta no scrcpy; acorda antes (o desbloqueio fica com o usuario)
adb -s $devices[0] shell input keyevent KEYCODE_WAKEUP
# --no-audio: este PC nao tem saida de audio padrao (scrcpy fecha sem isso)
$opcoes = @('-s', $devices[0], '--no-audio', "--window-title=Celular ($Modo)") + (Get-OpcoesDoModo $Modo)
scrcpy @opcoes
