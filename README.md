# Ponte

Veja e controle a tela do seu celular Android no PC com Windows, **sem cabo**, pelo Wi-Fi.
Por baixo usa o [scrcpy](https://github.com/Genymobile/scrcpy) (que já vem dentro do instalador).

## Instalar

1. Baixe o `Ponte-Setup-x.y.z.exe` em [**Releases**](https://github.com/Yag0Buen0/ponte/releases).
2. Abra. Se aparecer *"O Windows protegeu o computador"*: **Mais informações → Executar assim mesmo** (o instalador não tem assinatura digital paga).
3. Avance e conclua. Não precisa de administrador; instala só para o seu usuário.

## Primeira conexão

Precisa de **Android 11 ou mais novo**, com celular e PC **no mesmo Wi-Fi**.

1. No celular, ative as **Opções do desenvolvedor**: Configurações → Sobre o telefone → toque 7 vezes em **Número da versão**.
2. Configurações → Opções do desenvolvedor → **Depuração por Wi-Fi** → ligue.
3. Abra o **Celular** no menu Iniciar do PC. Como o celular ainda não foi pareado, aparece o assistente.
4. No celular: **Depuração por Wi-Fi → Parear dispositivo com código**. Digite o código de 6 dígitos no assistente e clique em **Parear**.

Pronto. Das próximas vezes, com a Depuração por Wi-Fi ligada, é só abrir **Celular** no menu Iniciar.

| Inglês | Português |
|---|---|
| Settings → About phone → Build number | Configurações → Sobre o telefone → Número da versão |
| Developer options → Wireless debugging | Opções do desenvolvedor → Depuração por Wi-Fi |
| Pair device with pairing code | Parear dispositivo com código de pareamento |

## Usando

A janela fica **sempre por cima dos outros apps**, pode ser **redimensionada para qualquer tamanho** (a imagem estica) e arrastada pela barra de título. A tela física do celular fica apagada enquanto você usa pelo PC.

| Ação | Atalho (com a janela selecionada) |
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

## Coisas que é bom saber

- **Tela preta = celular bloqueado.** O Android não deixa capturar a tela de bloqueio. Desbloqueie no celular, ou às cegas pelo PC: Alt+P, arraste de baixo para cima, digite o PIN, Enter. Aumentar o *tempo de tela* evita que ele bloqueie durante o uso.
- **Som**: vai para o PC. Se o PC não tiver saída de áudio, a Ponte abre sem som automaticamente.
- **Apps de banco** e outros protegidos aparecem pretos — é proteção do próprio app.
- **Bateria**: só a Depuração por Wi-Fi ligada gasta quase nada; transmitindo a tela, algo como 8–20% por hora.
- **Segurança**: só computadores pareados conectam (conexão criptografada). Desligue a Depuração por Wi-Fi quando não usar e não ligue em Wi-Fi público. Para revogar: Depuração por Wi-Fi → Dispositivos pareados → Esquecer.

## Desinstalar

Configurações do Windows → Aplicativos → Aplicativos instalados → **Ponte (Celular no PC)** → Desinstalar.

## Para desenvolvedores

```
app/            Celular.ps1 (abre o celular / assistente de pareamento) e Ponte.Core.psm1 (funções puras)
app/tests/      testes (Pester 3.4, já vem no Windows): powershell -Command "Invoke-Pester app\tests"
instalador/     Ponte.iss (Inno Setup 6) e build.ps1
fase1/          extra: controle pelo navegador com ws-scrcpy (instalar-web.ps1, iniciar-web.bat)
docs/           design e planos
```

Gerar o instalador (precisa do Inno Setup 6: `winget install --id JRSoftware.InnoSetup -e`):

```
powershell -ExecutionPolicy Bypass -File instalador\build.ps1 -Versao 0.1.0
```

O build roda os testes, baixa o scrcpy oficial numa versão fixa, confere o SHA256 e gera `instalador\saida\Ponte-Setup-<versão>.exe`.

## Licenças

O scrcpy (inclui o adb) é distribuído sob a Apache License 2.0 — o texto vai junto no instalador (`scrcpy\LICENSE.txt`).
