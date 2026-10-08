# Mapeador de teclas para jogos (v0.3.0)

Data: 08/10/2026 · Status: aprovado (versão 1)

## Objetivo

Jogar no Celular Remoto com teclado e mouse, com foco em FPS: teclas viram toques, WASD vira analógico, mouse move a câmera, cliques atiram e miram. Também serve para outros jogos (só "tecla → toque").

## Como os toques chegam ao celular

O `adb shell input tap` é lento demais para jogo e não faz vários dedos ao mesmo tempo. O app abre uma **segunda conexão de controle** com o celular, usando o próprio `scrcpy-server` (já instalado no celular pela janela principal), **sem vídeo e sem áudio**:

```
adb -s <serial> forward tcp:0 localabstract:scrcpy_<scid>
adb -s <serial> shell CLASSPATH=/data/local/tmp/scrcpy-server.jar app_process / com.genymobile.scrcpy.Server 4.1 \
    scid=<scid> log_level=info video=false audio=false control=true tunnel_forward=true \
    send_device_meta=false clipboard_autosync=false power_on=false cleanup=false
```

- Conecta em `127.0.0.1:<porta>`, lê 1 byte (dummy) e passa a enviar mensagens de toque.
- Sem vídeo, o servidor usa as **coordenadas reais da tela** (confirmado no código do scrcpy 4.1, `Controller.getEventPointAndDisplayId`).
- Mensagem de toque (32 bytes, big-endian): `type=2` (1) · `action` (1: 0 DOWN, 1 UP, 2 MOVE) · `pointerId` (8) · `x` (4) · `y` (4) · `largura` (2) · `altura` (2) · `pressão` (2, u16 ponto fixo: 1.0 → 0xFFFF) · `actionButton` (4) · `buttons` (4).
- Até 10 dedos ao mesmo tempo (limite do servidor). Cada elemento do mapa usa um `pointerId` próprio.
- Tamanho/rotação atuais: `dumpsys input` → linha `Viewport INTERNAL ... isActive=[1]` → `orientation=N`, `logicalFrame=[0, 0, L, A]`. Lido ao ligar o modo jogo e a cada 2 s enquanto ativo (jogos giram a tela).

## Coordenadas

A janela do scrcpy estica a imagem para preencher a área cliente (`--render-fit=stretched`), então: ponto na janela normalizado (0..1) × tamanho lógico atual = ponto no celular. Os mapas guardam **posições normalizadas** (0..1), valendo para qualquer tamanho de janela.

## Mapa (perfil por jogo)

- Arquivo `%APPDATA%\CelularRemoto\mapas\<pacote>.json` (ex.: `com.dts.freefireth.json`). Pacote do app em foco: `dumpsys window | grep mCurrentFocus`.
- Elementos:
  - **Toque**: tecla (ou `MouseEsquerdo`/`MouseDireito`) + posição. Segurar a tecla segura o dedo.
  - **Analógico**: 4 teclas (padrão W/A/S/D) + centro + raio (fração da largura). Diagonais normalizadas.
  - **Câmera**: centro da área + sensibilidade (padrão 1,0). Só um por mapa.

## Modo jogo

- **F1** (com a janela do celular em foco) liga/desliga. Uma etiqueta "MODO JOGO · F1 sai · F2 edita" aparece no topo da janela.
- Ligado: teclas e cliques **mapeados** viram toques e **não** chegam ao scrcpy; os demais passam normalmente. Movimento do mouse vira câmera: o cursor é preso no centro da janela (`ClipCursor` + `SetCursorPos`), o deslocamento move o "dedo da câmera"; ao passar do raio (0,25 da largura) ou ficar 150 ms parado, o dedo levanta e o próximo movimento recomeça do centro.
- Sair (F1, Alt+Tab, janela perde o foco): solta todos os dedos, libera o cursor.
- Captura por hooks globais de baixo nível (`WH_KEYBOARD_LL`, `WH_MOUSE_LL`), ativos só com o modo jogo ligado e a janela do celular em primeiro plano; eventos injetados (`SetCursorPos`) são ignorados.

## Editor (F2)

Camada transparente por cima da janela do celular, com barra: **+ Toque · + Analógico · + Câmera · Salvar · Cancelar**.
- Clique num marcador seleciona; arrastar move; com um marcador selecionado, apertar uma tecla (ou clicar com o botão esquerdo/direito segurando Ctrl) define a tecla; **Delete** remove; rodinha do mouse muda o raio do analógico e a sensibilidade da câmera.
- Salvar grava o mapa do jogo em foco.

## Painel

Grupo **Jogos**: ☐ Mapeador de teclas (F1 modo jogo · F2 editar). Desligado por padrão.

## Fora do escopo (versão 2)

Toque repetido (turbo), rodinha trocando arma, macros, exportar/importar.

## Testes

- Unitários: bytes da mensagem de toque; parse de `dumpsys input` (rotação/tamanho) e do pacote em foco; normalizado → coordenadas do celular; vetor do analógico (diagonais, soltar); lógica da câmera (acúmulo, sensibilidade, recentralizar no raio); JSON do mapa (ida e volta, ausente/corrompido).
- Manual: tecla → toque num app de desenho; analógico e câmera num jogo; vários dedos juntos (andar + mirar + atirar); F1/F2; perder foco solta tudo.
