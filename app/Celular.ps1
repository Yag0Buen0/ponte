# Abre a tela do celular Android no PC (scrcpy por Wi-Fi).
# Se o celular nao for encontrado, mostra o assistente de pareamento.
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Ponte.Core.psm1') -Force
Add-Type -AssemblyName System.Windows.Forms, System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

function Show-Aviso([string]$Mensagem) {
    [System.Windows.Forms.MessageBox]::Show($Mensagem, 'Celular', 'OK', 'Warning') | Out-Null
}

# scrcpy e adb: pasta embutida pelo instalador, senao o PATH (desenvolvimento)
$embutido = Join-Path $PSScriptRoot 'scrcpy'
if (Test-Path (Join-Path $embutido 'scrcpy.exe')) { $env:Path = "$embutido;$env:Path" }
foreach ($cmd in 'adb', 'scrcpy') {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Show-Aviso "Nao encontrei o $cmd. Reinstale o Ponte."
        exit 1
    }
}

# Roda o adb e devolve stdout+stderr como linhas (sem virar erro no PowerShell 5.1)
function Invoke-Adb {
    $ErrorActionPreference = 'Continue'
    @(& adb @args 2>&1 | ForEach-Object { "$_" })
}

function Find-Celular {
    $serial = @(Get-SerialsConectados (Invoke-Adb devices)) | Select-Object -First 1
    if ($serial) { return $serial }
    $conexao = @(ConvertFrom-MdnsServices (Invoke-Adb mdns services)) |
        Where-Object Tipo -eq 'conexao' | Select-Object -First 1
    if ($conexao) {
        Invoke-Adb connect $conexao.Endereco | Out-Null
        return @(Get-SerialsConectados (Invoke-Adb devices)) | Select-Object -First 1
    }
    $null
}

# Repete Find-Celular por alguns segundos sem travar a janela
function Wait-Celular([int]$Segundos) {
    $fim = (Get-Date).AddSeconds($Segundos)
    do {
        $serial = Find-Celular
        if ($serial) { return $serial }
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 700
    } while ((Get-Date) -lt $fim)
    $null
}

function Invoke-Pareamento([string]$Codigo, [System.Windows.Forms.Label]$Status) {
    $Status.Text = 'Procurando o celular na rede...'
    $fim = (Get-Date).AddSeconds(15)
    do {
        $pareamento = @(ConvertFrom-MdnsServices (Invoke-Adb mdns services)) |
            Where-Object Tipo -eq 'pareamento' | Select-Object -First 1
        if ($pareamento) { break }
        [System.Windows.Forms.Application]::DoEvents()
        Start-Sleep -Milliseconds 700
    } while ((Get-Date) -lt $fim)

    if (-not $pareamento) {
        $Status.Text = 'Nao achei o celular. Deixe aberta a tela "Pair device with pairing code" e confira o Wi-Fi.'
        return $null
    }

    $Status.Text = 'Pareando...'
    [System.Windows.Forms.Application]::DoEvents()
    $saida = (Invoke-Adb pair $pareamento.Endereco $Codigo.Trim()) -join "`n"
    if (-not (Test-PareamentoOk $saida)) {
        $Status.Text = 'O pareamento falhou. Gere um codigo novo no celular e tente de novo.'
        return $null
    }

    $Status.Text = 'Pareado! Conectando...'
    $serial = Wait-Celular 15
    if (-not $serial) { $Status.Text = 'Pareado, mas nao conectou. Clique em "Tentar conectar".' }
    $serial
}

function Show-Assistente {
    $form = New-Object System.Windows.Forms.Form -Property @{
        Text = 'Conectar celular'; ClientSize = New-Object System.Drawing.Size(460, 400)
        StartPosition = 'CenterScreen'; FormBorderStyle = 'FixedDialog'; MaximizeBox = $false
        Font = New-Object System.Drawing.Font('Segoe UI', 10)
    }
    $passos = New-Object System.Windows.Forms.Label -Property @{
        Location = '16,12'; Size = '430,220'
        Text = "Celular e PC precisam estar no mesmo Wi-Fi.`n`n" +
            "No celular (Android 11 ou mais novo):`n" +
            "1. Configuracoes > Sobre o telefone > toque 7 vezes em `"Numero da versao`"`n" +
            "    (Settings > About phone > Build number)`n" +
            "2. Configuracoes > Opcoes do desenvolvedor > Depuracao por Wi-Fi: ligue`n" +
            "    (Developer options > Wireless debugging)`n" +
            "3. Toque em `"Depuracao por Wi-Fi`" > `"Parear dispositivo com codigo`"`n" +
            "    (Pair device with pairing code)`n" +
            "4. Digite aqui o codigo de 6 digitos:"
    }
    $codigo = New-Object System.Windows.Forms.TextBox -Property @{
        Location = '16,236'; Size = '140,30'; MaxLength = 6
        Font = New-Object System.Drawing.Font('Segoe UI', 14)
    }
    $parear = New-Object System.Windows.Forms.Button -Property @{ Location = '170,236'; Size = '120,34'; Text = 'Parear' }
    $tentar = New-Object System.Windows.Forms.Button -Property @{ Location = '300,236'; Size = '146,34'; Text = 'Tentar conectar' }
    $status = New-Object System.Windows.Forms.Label -Property @{
        Location = '16,286'; Size = '430,100'; ForeColor = [System.Drawing.Color]::DarkSlateBlue
        Text = 'Ja pareou antes? Ligue a Depuracao por Wi-Fi e clique em "Tentar conectar".'
    }
    $form.Controls.AddRange(@($passos, $codigo, $parear, $tentar, $status))
    $form.AcceptButton = $parear

    $concluir = {
        param($serial)
        if ($serial) { $form.Tag = $serial; $form.Close() }
    }
    $parear.Add_Click({
        if (-not (Test-CodigoPareamento $codigo.Text)) { $status.Text = 'O codigo tem 6 digitos.'; return }
        $parear.Enabled = $tentar.Enabled = $false
        try { & $concluir (Invoke-Pareamento $codigo.Text $status) }
        finally { $parear.Enabled = $tentar.Enabled = $true }
    })
    $tentar.Add_Click({
        $parear.Enabled = $tentar.Enabled = $false
        $status.Text = 'Procurando o celular...'
        try {
            $serial = Wait-Celular 8
            if (-not $serial) { $status.Text = 'Nao encontrado. A Depuracao por Wi-Fi esta ligada e no mesmo Wi-Fi?' }
            & $concluir $serial
        } finally { $parear.Enabled = $tentar.Enabled = $true }
    })

    $form.ShowDialog() | Out-Null
    $form.Tag
}

# Abre o scrcpy; se o PC nao tiver saida de audio, reabre sem audio
function Start-Celular([string]$Serial) {
    $log = Join-Path $env:TEMP 'ponte-scrcpy.log'
    foreach ($semAudio in $false, $true) {
        $inicio = Get-Date
        $p = Start-Process scrcpy -ArgumentList (Get-OpcoesScrcpy -Serial $Serial -SemAudio:$semAudio) `
            -RedirectStandardError $log -WindowStyle Hidden -PassThru
        $p.WaitForExit()
        $texto = Get-Content -Raw $log -ErrorAction SilentlyContinue
        $rapido = ((Get-Date) - $inicio).TotalSeconds -lt 15
        # o erro de audio pode vir so quando o celular toca algo, entao nao depende do tempo
        if (-not $semAudio -and (Test-ErroDeAudio $texto)) { continue }
        if ($rapido -and $p.ExitCode -ne 0) {
            $erro = ($texto -split "`n" | Where-Object { $_ -match 'ERROR' } | Select-Object -First 1)
            Show-Aviso "O scrcpy fechou com erro:`n$erro"
        }
        return
    }
}

Invoke-Adb start-server | Out-Null
$serial = Wait-Celular 3
if (-not $serial) { $serial = Show-Assistente }
if (-not $serial) { exit 0 }

# Celular dormindo = imagem preta; acorda antes (o desbloqueio fica com o usuario)
Invoke-Adb -s $serial shell input keyevent KEYCODE_WAKEUP | Out-Null
Start-Celular $serial
