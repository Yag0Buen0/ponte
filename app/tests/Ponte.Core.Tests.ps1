# Testes das funcoes puras do Ponte (Pester 3.4, que vem no Windows).
# Rodar: powershell -NoProfile -Command "Invoke-Pester app\tests"
Import-Module (Join-Path $PSScriptRoot '..\Ponte.Core.psm1') -Force

Describe 'ConvertFrom-MdnsServices' {
    $saida = @(
        'List of discovered mdns services',
        "adb-ABC123-xyz`t_adb-tls-pairing._tcp`t192.168.0.10:33329",
        "adb-ABC123-xyz`t_adb-tls-connect._tcp`t192.168.0.10:39111",
        "outro`t_googlecast._tcp`t192.168.0.20:8009"
    )

    It 'separa servicos de pareamento e conexao' {
        $s = @(ConvertFrom-MdnsServices $saida)
        $s.Count | Should Be 2
        ($s | Where-Object Tipo -eq 'pareamento').Endereco | Should Be '192.168.0.10:33329'
        ($s | Where-Object Tipo -eq 'conexao').Endereco | Should Be '192.168.0.10:39111'
    }

    It 'retorna vazio quando nao ha celular' {
        @(ConvertFrom-MdnsServices @('List of discovered mdns services')).Count | Should Be 0
    }
}

Describe 'Get-SerialsConectados' {
    It 'devolve so os aparelhos prontos' {
        $saida = @(
            'List of devices attached',
            "adb-ABC123-xyz._adb-tls-connect._tcp`tdevice",
            "192.168.0.11:5555`toffline",
            "XYZ`tunauthorized",
            ''
        )
        $r = @(Get-SerialsConectados $saida)
        $r.Count | Should Be 1
        $r[0] | Should Be 'adb-ABC123-xyz._adb-tls-connect._tcp'
    }
}

Describe 'Test-CodigoPareamento' {
    It 'aceita 6 digitos (com espacos em volta)' { Test-CodigoPareamento ' 123456 ' | Should Be $true }
    It 'recusa menos digitos' { Test-CodigoPareamento '12345' | Should Be $false }
    It 'recusa letras' { Test-CodigoPareamento '12a456' | Should Be $false }
}

Describe 'Test-PareamentoOk' {
    It 'reconhece sucesso do adb pair' {
        Test-PareamentoOk 'Successfully paired to 192.168.0.10:33329 [guid=adb-ABC123-xyz]' | Should Be $true
    }
    It 'reconhece falha' {
        Test-PareamentoOk 'error: protocol fault (couldn''t read status message): No error' | Should Be $false
    }
}

Describe 'Test-ErroDeAudio' {
    It 'detecta PC sem saida de audio' {
        Test-ErroDeAudio "INFO: Texture: 1080x2340`nERROR: Could not open audio device: No default audio device available" | Should Be $true
    }
    It 'ignora outros erros' {
        Test-ErroDeAudio 'ERROR: Could not find any ADB device' | Should Be $false
    }
}

Describe 'Get-OpcoesScrcpy' {
    It 'abre redimensionavel, esticando, com a tela fisica apagada' {
        $o = Get-OpcoesScrcpy -Serial 'S1'
        ($o -join ' ') | Should Be '-s S1 --window-title=Celular --turn-screen-off --no-window-aspect-ratio-lock --render-fit=stretched'
    }
    It 'adiciona --no-audio quando pedido' {
        (Get-OpcoesScrcpy -Serial 'S1' -SemAudio) -contains '--no-audio' | Should Be $true
    }
}
