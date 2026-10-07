# Celular Remoto — app com painel de configuração (v0.2.0)

Data: 07/10/2026 · Status: aprovado (opção B)

## Objetivo

Transformar o launcher da Ponte num app de verdade:
- nome **Celular Remoto**, ícone próprio, atalho na **Área de Trabalho** e no **menu Iniciar**;
- ao abrir, um **painel** com as configurações e um botão **Iniciar**;
- avisos de bateria/aquecimento quando o usuário escolhe níveis altos.

## Decisões

- **`.exe` em C#** (WinForms, .NET Framework 4.8), compilado com o `csc.exe` que vem no Windows (`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`). Nada a instalar para compilar nem para usar. C# 5 (limite desse compilador: sem interpolação `$""`, sem `nameof`, sem membros `=>`).
- Substitui `app/Celular.ps1` e `app/Ponte.Core.psm1` (e os testes Pester) — a lógica migra para C#.
- O atalho abre **sempre o painel** (sem "abrir direto").
- Configurações em `%APPDATA%\CelularRemoto\config.json` (por usuário, nunca no repo).
- Versão **0.2.0**. O instalador continua Inno Setup, agora instalando `CelularRemoto.exe`.

## Opções do painel

| Grupo | Opção | Valores | Padrão | Aviso ⚠ | Opção do scrcpy |
|---|---|---|---|---|---|
| Imagem | Resolução | Econômica 800 · Equilibrada 1280 · Alta 1920 · Máxima (real) | Equilibrada | Alta, Máxima | `--max-size=N` (Máxima: sem a opção) |
| Imagem | Quadros/s | 30 · 60 · 90 · 120 | 60 | 90, 120 | `--max-fps=N` |
| Imagem | Qualidade | Normal 8M · Alta 16M · Muito alta 24M | Normal | Alta, Muito alta | `--video-bit-rate=NM` |
| Janela | Sempre por cima | sim/não | sim | — | `--always-on-top` |
| Janela | Tela cheia | sim/não | não | — | `--fullscreen` |
| Celular | Apagar a tela enquanto usa | sim/não | sim | — | `--turn-screen-off` |
| Celular | Desligar a tela ao fechar | sim/não | não | — | `--power-off-on-close` |
| Celular | Mostrar toques | sim/não | não | — | `--show-touches` |
| Som | Som do celular no PC | sim/não | sim | — | sem: `--no-audio` |
| Extras | Gravar a tela | sim/não | não | — | `--record=<Vídeos>\Celular Remoto <aaaa-MM-dd HH-mm-ss>.mp4` |

Sempre presentes: `-s <serial>`, `--window-title=Celular Remoto`, `--no-window-aspect-ratio-lock`, `--render-fit=stretched`, `--render-driver=opengl` (corrige o travamento com tela apagada em Samsung + Intel).

Aviso (texto fixo, aparece só quando algum ⚠ está selecionado): "Resolução, quadros ou qualidade altos gastam mais bateria e podem esquentar o celular."

Botão **Restaurar padrão** volta todos os campos aos padrões da tabela.

## Fluxo

1. Abre o painel com a configuração salva (ou padrões; arquivo corrompido → padrões, sem erro).
2. **Iniciar**: salva a configuração, procura o celular (adb devices → mDNS `_adb-tls-connect` → `adb connect`). Não achou → assistente de pareamento (o mesmo da v0.1: código de 6 dígitos, porta achada por mDNS, botão "Tentar conectar").
3. Acorda o celular (`input keyevent KEYCODE_WAKEUP`), esconde o painel e abre o scrcpy.
4. scrcpy fechou → o painel volta. Se fechou com "Could not open audio device", reabre sem som e desmarca "Som" para a próxima vez. Outro erro rápido (< 15 s) → caixa de aviso com a primeira linha `ERROR`.

## Estrutura

```
app/src/Opcoes.cs        Configuracao (modelo + padrões), MontarArgumentos(config, serial, agora, pastaVideos), PrecisaAviso(config)
app/src/SaidaAdb.cs      parse de `adb devices`, `adb mdns services`, sucesso do `adb pair`, erro de áudio, validação do código
app/src/ConfigArquivo.cs ler/salvar JSON (DataContractJsonSerializer, sem dependência externa)
app/src/Adb.cs           executa adb, Find/Wait celular, pareamento
app/src/Painel.cs        janela principal (WinForms)
app/src/Assistente.cs    janela de pareamento (WinForms)
app/src/Program.cs       Main
app/tests/Testes.cs      testes das partes puras (Opcoes, SaidaAdb, ConfigArquivo) — runner próprio, sem framework
app/build.ps1            compila CelularRemoto.exe e Testes.exe com o csc do Windows
app/icone.ico            gerado a partir da logo (fundo transparente, 16–256 px)
```

`instalador/build.ps1` passa a: compilar e rodar `Testes.exe` (falhou → para), baixar o scrcpy (como hoje), gerar o instalador.

## Instalador

- Instala `CelularRemoto.exe` + `scrcpy\` em `{localappdata}\Programs\Ponte` (mesmo AppId, atualiza a v0.1 por cima e remove `Celular.ps1`/`Ponte.Core.psm1` antigos).
- Atalhos **Celular Remoto** no menu Iniciar e na Área de Trabalho (este com tarefa marcada por padrão, desmarcável); remove o atalho antigo "Celular".
- Nome em "Aplicativos instalados": **Celular Remoto**.

## Ícone

A partir da logo enviada pelo Yago: remover fundo branco (transparente), cortar margem, gerar `.ico` multi-tamanho. **Antes de publicar**: confirmar a origem/licença da imagem (repo público). Sem confirmação, não vai pro GitHub.

## Testes

- Unitários (Testes.exe): argumentos para cada opção e combinações; aviso só com ⚠; config ausente/corrompida → padrões; ida e volta do JSON; parsers do adb (casos da v0.1).
- Manual: painel abre com padrões; Iniciar conecta; aviso aparece/some; gravação gera MP4; Restaurar padrão; sem som em PC sem áudio; atalhos na Área de Trabalho e no Iniciar.
