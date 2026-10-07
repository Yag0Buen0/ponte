# Ponte — controle celular ↔ PC (design)

Data: 07/10/2026 · Autor: Yago · Status: aprovado

## Objetivo

Controlar o celular Android pelo PC (Windows 11 Pro) e o PC pelo celular, com:
- conexão principal por **Wi-Fi na mesma rede**, e também **de fora de casa** (via Tailscale);
- pareamento do celular por **QR code**;
- interface **web** (navegador), além do uso diário com ferramentas prontas.

Dois propósitos: ter algo funcionando já (Fase 1) e construir um projeto próprio para aprender/portfólio (Fase 2).

Celular: Android atual (≥ 11, suporta "Depuração por Wi-Fi" com pareamento por QR).

## Fase 1 — setup com ferramentas prontas (sem código)

| Item | Função |
|---|---|
| scrcpy + ADB por Wi-Fi | Controlar o celular pelo PC com mouse/teclado. Pareamento único por código de 6 dígitos (`adb pair`). |
| `conectar-celular.bat` | Atalho que conecta (`adb connect` / descoberta mDNS) e abre o scrcpy. |
| ws-scrcpy | Mesmo controle pelo navegador. |
| RDP do Windows + app "Windows App" | Controlar o PC pelo celular. |
| Tailscale (PC + celular) | Acesso de fora de casa sem expor portas na internet. |

Pronto quando: celular controlado pelo PC via Wi-Fi com um clique; PC controlado pelo celular via RDP; ambos funcionando também pelo Tailscale.

## Fase 2 — app "Ponte"

### Stack
- Backend: Node.js 24 + TypeScript, Fastify, WebSocket (`ws`).
- Frontend: Vite + TypeScript, sem framework.
- Testes: Vitest.
- Dependências externas: `adb` (platform-tools), `scrcpy-server` (jar do projeto scrcpy), `ffmpeg`, `nut.js`, `bonjour-service` (mDNS), gerador de QR.

### Arquitetura

```
 Navegador (PC ou celular)               PC (backend Ponte)                 Destino
 ┌──────────────────────┐    WebSocket   ┌───────────────────────┐
 │ canvas + WebCodecs   │◄── vídeo ──────│ android-session ──────┼── ADB ──► celular (scrcpy-server)
 │ captura de toque/    │─── comandos ──►│ desktop-session ──────┼── ffmpeg (tela) + nut.js (input)
 │ mouse/teclado        │                │ pairing · auth · adb  │
 └──────────────────────┘                └───────────────────────┘
```

### Módulos (backend)

| Módulo | Responsabilidade | Depende de |
|---|---|---|
| `adb` | Wrapper do binário adb: `devices`, `pair`, `connect`, `push`, `forward`, `shell`. | adb |
| `pairing` | Gera QR `WIFI:T:ADB;S:<nome>;P:<senha>;;`, descobre o celular via mDNS (`_adb-tls-pairing._tcp`), roda `adb pair`, depois conecta via `_adb-tls-connect._tcp`. Reconexão automática por mDNS em usos futuros. | adb, bonjour-service |
| `android-session` | Envia e inicia o `scrcpy-server` no celular, lê o stream H.264, envia eventos pelo protocolo de controle do scrcpy. | adb |
| `desktop-session` | Captura a tela do PC em H.264 (ffmpeg `gdigrab`, baixa latência) e aplica mouse/teclado com nut.js. | ffmpeg, nut.js |
| `auth` | Senha (hash no `.env`), sessão por cookie; todas as rotas e WebSockets exigem sessão. | — |
| `gateway` | Rotas HTTP e WebSockets `/ws/android` e `/ws/desktop`; liga navegador ↔ sessão. | todos acima |

### Frontend
- Login.
- Tela de pareamento: mostra o QR e o status ("aguardando", "pareando", "conectado").
- Visualizador único: decodifica H.264 com WebCodecs (`VideoDecoder`) e desenha em canvas — serve para celular e PC, pois os dois mandam H.264.
- Captura de entrada: pointer/touch/teclado → mensagens com coordenadas normalizadas → backend converte para coordenadas do aparelho.
- QR com o endereço da Ponte, para abrir o controle do PC no celular com a câmera.

### Fluxo de dados
1. Navegador abre WebSocket autenticado.
2. Backend inicia a sessão (android ou desktop) e repassa frames H.264 (com marcação de config/keyframe) ao navegador.
3. Navegador envia eventos de entrada; backend traduz (protocolo scrcpy ou nut.js) e aplica.

### HTTPS e acesso remoto
WebCodecs exige contexto seguro. Acesso fora do `localhost` será via `tailscale serve` (HTTPS automático). O servidor escuta só em `127.0.0.1`; nada exposto direto na internet.

### Erros
- Celular desconecta → tela mostra "reconectando", backend tenta via mDNS/`adb connect` com backoff.
- WebSocket cai → navegador reconecta sozinho e pede novo keyframe.
- Pareamento por QR expira (timeout) → gera QR novo.
- Dependência ausente (adb/ffmpeg) → erro claro na inicialização dizendo o que instalar.

### Testes
- Unitários (Vitest): codificação das mensagens de controle do scrcpy, conversão de coordenadas, montagem do QR, parse da saída do adb/mDNS.
- Manual ponta a ponta com o celular real e com o PC.

### Limitações conhecidas
- QR só funciona com celular e PC na mesma rede (depende de mDNS). Fora de casa, o celular precisa estar pareado antes.
- Somente Android; iPhone fora do escopo.

## Ordem de execução
1. Fase 1 (setup).
2. Ponte: pareamento por QR.
3. Ponte: PC → celular (vídeo + controle).
4. Ponte: celular → PC (captura de tela, input, QR do endereço).

Cada etapa da Fase 2 tem plano de implementação próprio.
