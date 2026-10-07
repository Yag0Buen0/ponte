# Funcoes puras do Ponte: interpretam a saida do adb/scrcpy e montam opcoes.
# Sem efeitos colaterais, para serem testadas (app\tests).

# Linhas de `adb mdns services` -> objetos { Nome, Tipo (pareamento|conexao), Endereco }
function ConvertFrom-MdnsServices([string[]]$Linhas) {
    foreach ($linha in $Linhas) {
        if ($linha -match '^(\S+)\s+_adb-tls-(pairing|connect)\._tcp\s+(\S+:\d+)') {
            [pscustomobject]@{
                Nome     = $Matches[1]
                Tipo     = if ($Matches[2] -eq 'pairing') { 'pareamento' } else { 'conexao' }
                Endereco = $Matches[3]
            }
        }
    }
}

# Linhas de `adb devices` -> seriais prontos para uso (estado "device")
function Get-SerialsConectados([string[]]$Linhas) {
    foreach ($linha in $Linhas) {
        if ($linha -match '^(\S+)\s+device$') { $Matches[1] }
    }
}

function Test-CodigoPareamento([string]$Codigo) {
    $Codigo.Trim() -match '^\d{6}$'
}

function Test-PareamentoOk([string]$Saida) {
    $Saida -match 'Successfully paired'
}

# PC sem saida de audio padrao: o scrcpy fecha logo no inicio com este erro
function Test-ErroDeAudio([string]$Texto) {
    $Texto -match 'Could not open audio device'
}

# Janela redimensionavel para qualquer tamanho (imagem estica), sempre por cima,
# tela fisica apagada. OpenGL: com a tela apagada (Samsung) o Direct3D descartava
# quadros em rajadas e a imagem travava; com OpenGL fica fluido.
function Get-OpcoesScrcpy([string]$Serial, [switch]$SemAudio) {
    $opcoes = @('-s', $Serial, '--window-title=Celular', '--turn-screen-off',
        '--no-window-aspect-ratio-lock', '--render-fit=stretched',
        '--always-on-top', '--render-driver=opengl')
    if ($SemAudio) { $opcoes += '--no-audio' }
    $opcoes
}

Export-ModuleMember -Function *
