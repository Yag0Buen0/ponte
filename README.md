# Ponte

Controle do celular Android pelo PC (e, na Fase 2, do PC pelo celular).
Design: `docs/superpowers/specs/2026-10-07-ponte-design.md` · Plano da Fase 1: `docs/superpowers/plans/2026-10-07-fase1-setup.md`

## Fase 1 — uso diário (ferramentas prontas)

Requisitos no PC: scrcpy (traz o adb) — `winget install --id Genymobile.scrcpy -e`.
No celular: Settings → System → Developer options → **Wireless debugging** ligado, mesma rede Wi-Fi do PC.

### Abrir o celular no PC

Tecla **Windows** → digite **Celular** (pasta *Ponte* no menu Iniciar). Para fixar: botão direito → *Fixar em Iniciar*.

| Atalho | Modo | Como fica |
|---|---|---|
| Celular | `normal` | Espelho da tela, tela física apagada |
| Celular PiP | `pip` | Janela pequena, sempre por cima, abre no canto inferior direito |
| Celular Livre | `livre` | Tela virtual separada: abre em pé e se ajusta ao formato da janela (estique para deitar, sem distorcer); o celular físico fica livre |
| Celular PiP Livre | `pip-livre` | Tela virtual pequena, sempre por cima |

Todas as janelas redimensionam para qualquer tamanho e podem ser arrastadas pela barra de título.
Pelo terminal: `fase1\conectar-celular.bat` ou `powershell -File fase1\conectar-celular.ps1 -Modo pip`.
Recriar os atalhos do Iniciar: `powershell -ExecutionPolicy Bypass -File fase1\criar-atalhos.ps1`.

### Atalhos do teclado (com a janela selecionada)

| Ação | Atalho |
|---|---|
| Voltar | botão direito / Alt+B |
| Tela inicial | botão do meio / Alt+H |
| Apps recentes | Alt+S |
| Notificações | Alt+N (Alt+Shift+N fecha) |
| Ligar/acordar | Alt+P |
| Volume | Alt+↑ / Alt+↓ |
| Girar | Alt+R |
| Tela cheia | Alt+F |
| Apagar/acender tela física | Alt+O / Alt+Shift+O |
| Colar texto do PC | Ctrl+V |

### Pelo navegador

1. Primeira vez: `powershell -ExecutionPolicy Bypass -File fase1\instalar-web.ps1` (baixa e compila o ws-scrcpy em `fase1\vendor\`, fora do git).
2. `fase1\iniciar-web.bat` → abre `http://localhost:8000` → clique em **WebCodecs** ao lado do celular.

O servidor escuta só em `127.0.0.1` (não aparece para outros aparelhos da rede).

### Primeira vez num PC novo (pareamento)

Wireless debugging → **Pair device with pairing code**. A porta de pareamento é diferente da porta da tela principal — descubra com:

```
adb mdns services          # linha _adb-tls-pairing._tcp -> IP:porta
adb pair <IP>:<porta> <codigo>
```

## Coisas que é bom saber

- **Tela preta = celular bloqueado.** A Samsung não deixa capturar a tela de bloqueio. Desbloqueie no celular, ou às cegas pelo PC: Alt+P, arrastar de baixo pra cima, digitar o PIN, Enter. Aumentar o *Screen timeout* evita que ele bloqueie durante o uso.
- **Sem áudio**: este PC não tem saída de áudio padrão, então o som fica no celular (`--no-audio`).
- **Apps de banco** e outros protegidos aparecem pretos — é proteção do app.
- **Bateria**: só o Wireless debugging ligado gasta quase nada; transmitindo a tela, algo como 8–20%/hora.
- **Segurança**: só computadores pareados conectam. Desligue o Wireless debugging quando não usar e não ligue em Wi-Fi público. Para revogar: *Paired devices → Forget*.

## Pendências

- **Fora de casa (Tailscale)**: instalado no PC, falta login no PC e no celular — plano, Task 6.
- **PC pelo celular**: RDP pulado (exige senha na conta do Windows). Fica para a Fase 2 (Ponte com login próprio).
- **Fase 2**: app Ponte — pareamento por QR, PC→celular e celular→PC no navegador.
