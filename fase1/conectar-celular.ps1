# Conecta o celular via ADB por Wi-Fi (descoberta mDNS) e abre o scrcpy
# com a tela fisica do celular apagada.
$ErrorActionPreference = 'Stop'

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

adb start-server | Out-Null
Start-Sleep -Seconds 2   # da tempo do adb reconectar sozinho via mDNS
$devices = @(Get-AdbDevices)

if ($devices.Count -eq 0) {
    Write-Host 'Procurando o celular na rede...'
    $alvo = Find-CelularNaRede
    if (-not $alvo) {
        throw 'Celular nao encontrado. Confira: mesma rede Wi-Fi e "Wireless debugging" ligado.'
    }
    adb connect $alvo | Out-Host
    $devices = @(Get-AdbDevices)
}

if ($devices.Count -eq 0) { throw 'Nao consegui conectar ao celular.' }

Write-Host "Conectado: $($devices[0])"
# --no-audio: este PC nao tem saida de audio padrao (scrcpy fecha sem isso)
scrcpy -s $devices[0] --no-audio --turn-screen-off
