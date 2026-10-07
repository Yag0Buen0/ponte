# Celular Remoto

Veja e controle a tela do seu celular Android no PC com Windows, **sem cabo**, pelo Wi-Fi.
Por baixo usa o [scrcpy](https://github.com/Genymobile/scrcpy), que já vem dentro do instalador. Veja os [Créditos](CREDITOS.md).

## Instalar

1. Baixe o `CelularRemoto-Setup-x.y.z.exe` em [**Releases**](https://github.com/Yag0Buen0/ponte/releases).
2. Abra. Se aparecer *"O Windows protegeu o computador"*: **Mais informações → Executar assim mesmo** (o instalador não tem assinatura digital paga).
3. Avance e conclua. Não precisa de administrador; instala só para o seu usuário e cria o atalho **Celular Remoto** no menu Iniciar e (se marcado) na Área de Trabalho.

Já tinha a versão 0.1 ("Ponte")? É só instalar por cima, ela é atualizada.

## Primeira conexão

Precisa de **Android 11 ou mais novo**, com celular e PC **no mesmo Wi-Fi**.

1. No celular, ative as **Opções do desenvolvedor**: Configurações → Sobre o telefone → toque 7 vezes em **Número da versão**.
2. Configurações → Opções do desenvolvedor → **Depuração por Wi-Fi** → ligue.
3. Abra o **Celular Remoto** e clique em **Iniciar**. Como o celular ainda não foi pareado, aparece o assistente.
4. No celular: **Depuração por Wi-Fi → Parear dispositivo com código**. Digite o código de 6 dígitos no assistente e clique em **Parear**.

Das próximas vezes, com a Depuração por Wi-Fi ligada, é só abrir e clicar em **Iniciar**.

| Inglês | Português |
|---|---|
| Settings → About phone → Build number | Configurações → Sobre o telefone → Número da versão |
| Developer options → Wireless debugging | Opções do desenvolvedor → Depuração por Wi-Fi |
| Pair device with pairing code | Parear dispositivo com código de pareamento |
| Quick settings developer tiles | Blocos de desenvolvedor das configurações rápidas |

**Dica:** em *Blocos de desenvolvedor das configurações rápidas*, ligue **Depuração por Wi-Fi**. Aí ela vira um botão no painel rápido do celular (onde ficam Wi-Fi e lanterna).

## O painel

| Grupo | Opções |
|---|---|
| Celular | Escolha qual celular usar quando houver mais de um; **Atualizar** procura de novo |
| Imagem e desempenho | **Resolução** (Econômica 800p · Equilibrada 1280p · Alta 1920p · Máxima), **Quadros** (30 · 60 · 90 · 120 por segundo), **Qualidade** (Normal · Alta · Muito alta). Os níveis marcados com ⚠ gastam mais bateria e podem esquentar o celular; o painel avisa. |
| Janela | Sempre por cima dos outros apps · Abrir em tela cheia |
| Celular | Apagar a tela do celular enquanto usa · Manter o celular acordado enquanto a janela estiver aberta (senão ele dorme no tempo de tela dele enquanto você só assiste, e a janela fica preta) · Desligar a tela ao fechar · Mostrar os toques · **Desligar a Depuração por Wi-Fi ao fechar** (mais seguro, mas na próxima vez você religa a depuração no celular antes de Iniciar) |
| Som | Som do celular no PC (se o PC não tiver saída de áudio, ele desliga sozinho) |
| Teclado | Digitar com acentos (ligado; desligue em jogos que usam W A S D) |
| Extras | Gravar a tela: salva um MP4 na pasta Vídeos |

As escolhas ficam salvas para a próxima vez. **Restaurar padrão** volta tudo ao normal. Quando você fecha a janela do celular, o painel volta.

A janela do celular pode ser **redimensionada para qualquer tamanho** (a imagem estica) e arrastada pela barra de título.

| Ação | Atalho (com a janela do celular selecionada) |
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

## Celular bloqueado

O Android **não deixa capturar a tela de bloqueio**, então ela ficaria preta no PC. Por isso, se o celular estiver bloqueado ao clicar em Iniciar, o painel avisa e **espera você desbloquear no celular**: a tela abre sozinha logo depois.

Usa PIN e prefere desbloquear pelo PC? Clique em **Abrir assim mesmo** e, com a janela selecionada: Alt+P, arraste de baixo para cima, digite o PIN no teclado, Enter.

Para não bloquear toda hora: aumente o **tempo de tela** do celular, ou use o desbloqueio em **lugares confiáveis** (Samsung: *Tela de bloqueio → Extend Unlock*; outros: *Smart Lock*). Esse último deixa o celular desbloqueado em casa para qualquer pessoa, então a decisão é sua.

## Coisas que é bom saber

- **Apps de banco** e outros protegidos aparecem pretos: é proteção do próprio app.
- **Bateria**: só a Depuração por Wi-Fi ligada gasta quase nada; transmitindo a tela, algo como 8–20% por hora (mais nos níveis ⚠).
- **Segurança**: só computadores pareados conectam (conexão criptografada). Desligue a Depuração por Wi-Fi quando não usar (ou marque a opção no painel) e não ligue em Wi-Fi público. Para revogar: Depuração por Wi-Fi → Dispositivos pareados → Esquecer.

## Desinstalar

Configurações do Windows → Aplicativos → Aplicativos instalados → **Celular Remoto** → Desinstalar.

## Para desenvolvedores

```
app/src/        CelularRemoto.exe em C# (WinForms, .NET Framework 4.8)
app/tests/      testes das partes puras (runner próprio, sem framework)
app/build.ps1   compila com o csc.exe que vem no Windows e roda os testes
instalador/     Ponte.iss (Inno Setup 6) e build.ps1
fase1/          extra: controle pelo navegador com ws-scrcpy
docs/           design e planos
```

Compilar e testar (nada a instalar): `powershell -ExecutionPolicy Bypass -File app\build.ps1`

Gerar o instalador (precisa do Inno Setup 6: `winget install --id JRSoftware.InnoSetup -e`):

```
powershell -ExecutionPolicy Bypass -File instalador\build.ps1 -Versao 0.2.0
```

O build compila o app, roda os testes, baixa o scrcpy oficial numa versão fixa, confere o SHA256 e gera `instalador\saida\CelularRemoto-Setup-<versão>.exe`.

## Créditos

Criado por **Yag0Buen0**, com o auxílio do Claude Code. O espelhamento e o controle são do **scrcpy** (Genymobile e Romain Vimont, Apache 2.0), que inclui adb, FFmpeg, SDL e libusb. A lista completa com as licenças está em [CREDITOS.md](CREDITOS.md) e no próprio app (link **Créditos e licenças** no painel).
