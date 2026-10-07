# Créditos

## O que é do Celular Remoto

O **Celular Remoto** (projeto *Ponte*) foi criado por **Yag0Buen0**, com desenvolvimento feito com o auxílio do **Claude Code** (Anthropic).

Criado neste projeto:
- o app **Celular Remoto** (`app/`): painel de configuração (resolução, quadros por segundo, qualidade, janela, som, gravação) com avisos de bateria e aquecimento, seletor de celular, espera pelo desbloqueio, opção de desligar a Depuração por Wi-Fi ao fechar;
- o **assistente de pareamento**: o usuário digita só o código de 6 dígitos, e o IP e a porta são achados sozinhos pela rede (mDNS);
- o **instalador** (`instalador/`) que junta tudo, sem precisar de administrador;
- a escolha de configurações que resolveu travamentos em celulares Samsung com a tela apagada (render OpenGL).

O espelhamento e o controle do celular em si são feitos pelo **scrcpy** (abaixo). O Celular Remoto é uma interface e um instalador em volta dele.

## Software de terceiros distribuído no instalador

| Componente | Autor | Licença | Código-fonte |
|---|---|---|---|
| **scrcpy** 4.1 (espelhamento e controle) | Genymobile e Romain Vimont | Apache License 2.0 | https://github.com/Genymobile/scrcpy |
| **adb** (Android Debug Bridge) | The Android Open Source Project | Apache License 2.0 | https://android.googlesource.com/platform/packages/modules/adb |
| **FFmpeg** (`avcodec`, `avformat`, `avutil`, `swresample`) | FFmpeg developers | GNU LGPL 2.1 ou posterior | https://ffmpeg.org |
| **SDL 3** | Sam Lantinga e colaboradores | zlib License | https://github.com/libsdl-org/SDL |
| **libusb** | libusb project | GNU LGPL 2.1 | https://github.com/libusb/libusb |

Esses componentes são distribuídos sem modificação, como vêm no pacote oficial do scrcpy para Windows (`scrcpy-win64-v4.1.zip`, SHA256 conferido no build). O texto da licença do scrcpy vai junto no instalador (`scrcpy\LICENSE.txt`). As bibliotecas LGPL são DLLs separadas e podem ser trocadas por outras versões compatíveis.

## Ferramentas usadas, não distribuídas

- **Inno Setup** (Jordan Russell e Martijn Laan): gera o instalador. https://jrsoftware.org/isinfo.php
- **.NET Framework 4.8** e compilador C# (Microsoft): já vêm no Windows.
- **ws-scrcpy** (NetrisTV, licença MIT): usado só no extra opcional de controle pelo navegador (`fase1/`), baixado à parte e nunca incluído no instalador. https://github.com/NetrisTV/ws-scrcpy

## Marcas

Android é marca da Google LLC. Samsung e Galaxy são marcas da Samsung Electronics. Windows é marca da Microsoft. O Celular Remoto não é afiliado a nenhuma delas.
