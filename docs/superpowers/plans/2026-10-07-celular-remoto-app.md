# Celular Remoto v0.2.0 · Plano de implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Trocar o launcher PowerShell por um `CelularRemoto.exe` (WinForms) com painel de configuração, seletor de celular e botão Iniciar.

**Architecture:** Partes puras (montar argumentos do scrcpy, avisos, parsers da saída do adb, config JSON) em classes estáticas testadas por um `Testes.exe` próprio. Partes com efeito (executar adb/scrcpy, janelas) usam essas classes. Tudo compilado pelo `csc.exe` do .NET Framework 4.8 que vem no Windows.

**Tech Stack:** C# 5, WinForms, .NET Framework 4.8, `DataContractJsonSerializer`, Inno Setup 6, scrcpy 4.1.

## Global Constraints

- Compilador: `%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe` (C# 5 — sem `$""`, sem `nameof`, sem membros `=>`, sem `?.`). `async/await` e `Task.Run` OK.
- Sem dependência externa (nem NuGet). Referências: `System.Windows.Forms.dll`, `System.Drawing.dll`, `System.Runtime.Serialization.dll`.
- Config em `%APPDATA%\CelularRemoto\config.json`; nunca no repo.
- Nada pessoal no repo (e-mail, `C:\Users\<nome>`, IP, serial de celular). Seriais nos testes são fictícios.
- Textos da interface em português, com acentos (arquivo `.cs` salvo em UTF-8; o csc lê UTF-8 sem BOM? → salvar com BOM para garantir).
- Ícone só vai para o GitHub depois de confirmada a origem/licença da logo.
- Versão `0.2.0`; mesmo `AppId` do instalador da v0.1.

---

### Task 1: Esqueleto, runner de testes e `Opcoes` (argumentos + avisos)

**Files:** Create `app/src/Opcoes.cs`, `app/tests/Testes.cs`, `app/build.ps1`

**Interfaces — Produces:**
- `class Configuracao` (`[DataContract]`, campos públicos `[DataMember]`): `int Resolucao` (800/1280/1920/0=máxima), `int Fps` (30/60/90/120), `int BitrateMbps` (8/16/24), `bool SempreNoTopo, TelaCheia, ApagarTela, DesligarTelaAoFechar, MostrarToques, Som, Gravar, DesligarDepuracaoAoFechar`, `string UltimoSerial`; `static Configuracao Padrao()`; `void Normalizar()` (valores fora das listas → padrão); `[OnDeserializing]` aplica padrões antes de ler.
- `static class Opcoes`: `List<string> MontarArgumentos(Configuracao c, string serial, DateTime agora, string pastaVideos)`, `string CaminhoGravacao(string pastaVideos, DateTime agora)`, `bool AvisoDesempenho(Configuracao c)`, `string JuntarArgumentos(IEnumerable<string> args)` (aspas em argumento com espaço/aspas).

- [ ] Step 1: testes (padrões; argumentos padrão exatos; cada opção liga/desliga sua flag; Máxima sem `--max-size`; gravação com caminho entre aspas; aviso só com Alta/Máxima/90/120/16M/24M; Normalizar).
- [ ] Step 2: `build.ps1 -SoTestes` → falha de compilação (classe não existe).
- [ ] Step 3: implementar `Opcoes.cs`.
- [ ] Step 4: testes passam.
- [ ] Step 5: commit.

### Task 2: `SaidaAdb` (parsers portados da v0.1 + dedupe)

**Files:** Create `app/src/SaidaAdb.cs`; Modify `app/tests/Testes.cs`

**Produces:** `static class SaidaAdb`: `List<string> SeriaisProntos(string saida)`; `List<ServicoMdns> ServicosMdns(string saida)` (`ServicoMdns { string Nome; bool Pareamento; string Endereco; }`); `bool CodigoValido(string codigo)`; `bool PareamentoOk(string saida)`; `bool ErroDeAudio(string log)`; `string PrimeiroErro(string log)`.

- [ ] Steps TDD iguais (casos da v0.1: tabs, offline/unauthorized ignorados, serviços de outro tipo ignorados, código com espaços).

### Task 3: `ConfigArquivo` (JSON)

**Files:** Create `app/src/ConfigArquivo.cs`; Modify `app/tests/Testes.cs`

**Produces:** `static class ConfigArquivo`: `Configuracao Ler(string caminho)` (ausente/corrompido → `Padrao()`), `void Salvar(string caminho, Configuracao c)` (cria a pasta), `string CaminhoPadrao()`.

- [ ] TDD: ida e volta; arquivo ausente; lixo no arquivo; JSON parcial mantém padrões nos campos faltando; valor inválido normalizado.

### Task 4: `Adb` (efeitos: executar, listar, conectar, parear, desligar depuração)

**Files:** Create `app/src/Adb.cs`

**Produces:** `class Aparelho { string Serial; string Modelo; string Exibicao; }`; `static class Adb`: `string Pasta` (scrcpy embutido ou PATH), `string Executar(int timeoutMs, params string[] args)`, `List<Aparelho> Listar()` (dedupe por `ro.serialno`, modelo por `ro.product.model`), `bool TentarConectarPorMdns()`, `List<Aparelho> EsperarAparelhos(int segundos)`, `string Parear(string codigo, Action<string> status)` (devolve serial ou null), `void Acordar(string serial)`, `void DesligarDepuracao(string serial)`.

- [ ] Verificação manual: com o celular ligado, `Listar()` mostra 1 item por aparelho (moto e13 visto por IP+mDNS vira 1).

### Task 5: Janelas e `Program`

**Files:** Create `app/src/Assistente.cs`, `app/src/Painel.cs`, `app/src/Program.cs`

- Painel: grupos da spec; seletor de celular + "Atualizar"; avisos dinâmicos; Restaurar padrão; Iniciar (salva config → aparelho → assistente se preciso → acorda → esconde painel → scrcpy → ao fechar: desliga depuração se marcado, reabre sem som se erro de áudio e desmarca Som, aviso se erro rápido → mostra painel).
- Assistente: igual à v0.1 + lembrete do bloco do painel rápido.
- `Program.Main`: `[STAThread]`, instância única (Mutex), `Application.Run(new Painel())`.

- [ ] Verificação manual dos fluxos.

### Task 6: Ícone

**Files:** Create `app/gerar-icone.ps1`, `app/icone.ico`

- Fundo branco → transparente, corta margem, quadrado, PNG em 16/24/32/48/64/128/256 dentro do `.ico`.
- [ ] Verificar visualmente o PNG 256.

### Task 7: Instalador, limpeza e docs

**Files:** Modify `instalador/Ponte.iss`, `instalador/build.ps1`, `README.md`, `.gitignore`; Delete `app/Celular.ps1`, `app/Ponte.Core.psm1`, `app/tests/Ponte.Core.Tests.ps1`

- `build.ps1` chama `app\build.ps1` (compila + roda testes) e passa a pasta de saída ao ISCC.
- `.iss`: `CelularRemoto.exe`, ícone, atalhos Iniciar + Área de Trabalho (task marcada), `[InstallDelete]` dos arquivos/atalho da v0.1, `UninstallDisplayName=Celular Remoto`, versão 0.2.0.
- [ ] Build completo, instalar por cima da v0.1, testar atalhos e fluxo, commit.

### Task 8: Publicar (depois do ok do Yago e da licença do ícone)

- [ ] push, tag `v0.2.0`, Release com o exe.
