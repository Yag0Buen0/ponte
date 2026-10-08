// Testes das partes puras do Celular Remoto. Runner proprio (sem framework):
// cada metodo publico estatico "Teste_*" e executado; o exit code e o numero de falhas.
// Rodar: powershell -File app\build.ps1 -SoTestes
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CelularRemoto;

static class Testes
{
    static int falhas;
    static string atual;

    static int Main()
    {
        var metodos = typeof(Testes).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.Name.StartsWith("Teste_")).OrderBy(m => m.Name).ToList();
        foreach (var m in metodos)
        {
            atual = m.Name;
            try { m.Invoke(null, null); }
            catch (TargetInvocationException e) { Falha("excecao: " + e.InnerException.Message); }
        }
        Console.WriteLine("{0} testes, {1} falha(s)", metodos.Count, falhas);
        return falhas;
    }

    static void Falha(string msg)
    {
        falhas++;
        Console.WriteLine("[FALHOU] " + atual + ": " + msg);
    }

    static void Igual(object esperado, object obtido)
    {
        if (!Equals(esperado, obtido))
            Falha("esperado <" + esperado + "> obtido <" + obtido + ">");
    }

    static void Verdade(bool condicao, string descricao)
    {
        if (!condicao) Falha(descricao);
    }

    static readonly DateTime Agora = new DateTime(2026, 10, 7, 14, 5, 9);
    const string Videos = @"C:\Videos";

    static string Args(Configuracao c)
    {
        return string.Join(" ", Opcoes.MontarArgumentos(c, "S1", Agora, Videos));
    }

    // ---------- Opcoes ----------

    public static void Teste_Opcoes_Padrao()
    {
        var c = Configuracao.Padrao();
        Igual(1280, c.Resolucao);
        Igual(60, c.Fps);
        Igual(8, c.BitrateMbps);
        Verdade(c.SempreNoTopo && c.ApagarTela && c.Som, "sempre no topo, apagar tela e som ligados");
        Verdade(!c.TelaCheia && !c.DesligarTelaAoFechar && !c.MostrarToques && !c.Gravar && !c.DesligarDepuracaoAoFechar,
            "demais opcoes desligadas");
    }

    public static void Teste_Opcoes_ArgumentosPadrao()
    {
        Igual("-s S1 --window-title=Celular Remoto --no-window-aspect-ratio-lock --render-fit=stretched " +
              "--render-driver=opengl --max-size=1280 --max-fps=60 --video-bit-rate=8M --always-on-top --turn-screen-off " +
              "--screen-off-timeout=3600 --prefer-text",
            Args(Configuracao.Padrao()));
    }

    public static void Teste_Opcoes_ResolucaoMaximaSemMaxSize()
    {
        var c = Configuracao.Padrao(); c.Resolucao = 0;
        Verdade(!Args(c).Contains("--max-size"), "maxima nao limita");
    }

    public static void Teste_Opcoes_CadaOpcaoLigaSuaFlag()
    {
        var c = Configuracao.Padrao();
        c.SempreNoTopo = false; c.ApagarTela = false; c.Som = false;
        c.TelaCheia = true; c.DesligarTelaAoFechar = true; c.MostrarToques = true;
        c.Resolucao = 1920; c.Fps = 120; c.BitrateMbps = 24;
        var a = Args(c);
        Verdade(!a.Contains("--always-on-top"), "sem always-on-top");
        Verdade(!a.Contains("--turn-screen-off"), "sem turn-screen-off");
        Verdade(a.Contains("--no-audio"), "com no-audio");
        Verdade(a.Contains("--fullscreen"), "com fullscreen");
        Verdade(a.Contains("--power-off-on-close"), "com power-off-on-close");
        Verdade(a.Contains("--show-touches"), "com show-touches");
        Verdade(a.Contains("--max-size=1920") && a.Contains("--max-fps=120") && a.Contains("--video-bit-rate=24M"),
            "niveis altos");
    }

    public static void Teste_Opcoes_Gravacao()
    {
        var c = Configuracao.Padrao(); c.Gravar = true;
        var lista = Opcoes.MontarArgumentos(c, "S1", Agora, Videos);
        Verdade(lista.Contains(@"--record=C:\Videos\Celular Remoto 2026-10-07 14-05-09.mp4"), "caminho da gravacao");
        Verdade(Opcoes.JuntarArgumentos(lista).Contains(@"""--record=C:\Videos\Celular Remoto 2026-10-07 14-05-09.mp4"""),
            "argumento com espaco vai entre aspas");
    }

    public static void Teste_Opcoes_JuntarArgumentos()
    {
        Igual(@"-s S1 ""--window-title=Celular Remoto"" ""a\""b""",
            Opcoes.JuntarArgumentos(new[] { "-s", "S1", "--window-title=Celular Remoto", "a\"b" }));
    }

    public static void Teste_Opcoes_AvisoDesempenho()
    {
        var c = Configuracao.Padrao();
        Verdade(!Opcoes.AvisoDesempenho(c), "padrao sem aviso");
        c = Configuracao.Padrao(); c.Resolucao = 800; c.Fps = 30;
        Verdade(!Opcoes.AvisoDesempenho(c), "economico sem aviso");
        foreach (var ajuste in new Action<Configuracao>[] {
            x => x.Resolucao = 1920, x => x.Resolucao = 0, x => x.Fps = 90, x => x.Fps = 120,
            x => x.BitrateMbps = 16, x => x.BitrateMbps = 24 })
        {
            c = Configuracao.Padrao(); ajuste(c);
            Verdade(Opcoes.AvisoDesempenho(c), "nivel alto avisa");
        }
    }

    // ---------- SaidaAdb ----------

    public static void Teste_Adb_SeriaisProntos()
    {
        var saida = "List of devices attached\r\n" +
                    "adb-ABC123-xyz._adb-tls-connect._tcp\tdevice\r\n" +
                    "192.168.0.11:5555\toffline\r\n" +
                    "XYZ\tunauthorized\r\n\r\n";
        var r = SaidaAdb.SeriaisProntos(saida);
        Igual(1, r.Count);
        Igual("adb-ABC123-xyz._adb-tls-connect._tcp", r[0]);
    }

    public static void Teste_Adb_ServicosMdns()
    {
        var saida = "List of discovered mdns services\n" +
                    "adb-ABC123-xyz\t_adb-tls-pairing._tcp\t192.168.0.10:33329\n" +
                    "adb-ABC123-xyz\t_adb-tls-connect._tcp\t192.168.0.10:39111\n" +
                    "outro\t_googlecast._tcp\t192.168.0.20:8009\n";
        var s = SaidaAdb.ServicosMdns(saida);
        Igual(2, s.Count);
        Igual("192.168.0.10:33329", s.Single(x => x.Pareamento).Endereco);
        Igual("192.168.0.10:39111", s.Single(x => !x.Pareamento).Endereco);
        Igual(0, SaidaAdb.ServicosMdns("List of discovered mdns services\n").Count);
    }

    public static void Teste_Adb_Codigo()
    {
        Verdade(SaidaAdb.CodigoValido(" 123456 "), "6 digitos com espacos");
        Verdade(!SaidaAdb.CodigoValido("12345"), "5 digitos");
        Verdade(!SaidaAdb.CodigoValido("12a456"), "letra");
        Verdade(!SaidaAdb.CodigoValido(null), "nulo");
    }

    public static void Teste_Adb_Pareamento()
    {
        Verdade(SaidaAdb.PareamentoOk("Successfully paired to 192.168.0.10:33329 [guid=adb-ABC123-xyz]"), "sucesso");
        Verdade(!SaidaAdb.PareamentoOk("error: protocol fault (couldn't read status message): No error"), "falha");
    }

    public static void Teste_Adb_LogDoScrcpy()
    {
        var log = "INFO: Texture: 1080x2340\nERROR: Could not open audio device: No default audio device available\nERROR: Demuxer error\n";
        Verdade(SaidaAdb.ErroDeAudio(log), "erro de audio");
        Verdade(!SaidaAdb.ErroDeAudio("ERROR: Could not find any ADB device"), "outro erro");
        Igual("ERROR: Could not open audio device: No default audio device available", SaidaAdb.PrimeiroErro(log));
        Igual(null, SaidaAdb.PrimeiroErro("INFO: tudo certo"));
    }

    public static void Teste_Adb_Bloqueado()
    {
        Verdade(SaidaAdb.Bloqueado("    mShowingDream=false mDreamingLockscreen=true\n    isKeyguardShowing=true\n"), "bloqueado");
        Verdade(!SaidaAdb.Bloqueado("    mShowingDream=false mDreamingLockscreen=true\n    isKeyguardShowing=false\n"), "desbloqueado");
        Verdade(!SaidaAdb.Bloqueado(""), "sem informacao = nao bloqueia o fluxo");
    }

    public static void Teste_Adb_Acordado()
    {
        Verdade(SaidaAdb.Acordado("  mWakefulness=Awake\n"), "acordado");
        Verdade(!SaidaAdb.Acordado("  mWakefulness=Dozing\n"), "cochilando (AOD)");
        Verdade(!SaidaAdb.Acordado("  mWakefulness=Asleep\n"), "dormindo");
    }

    public static void Teste_Opcoes_ManterAcordado()
    {
        var c = Configuracao.Padrao();
        Verdade(c.ManterAcordado, "padrao ligado");
        c.ManterAcordado = false;
        Verdade(!Args(c).Contains("--screen-off-timeout"), "desligado usa o tempo de tela do celular");
    }

    public static void Teste_Opcoes_MapeadorDesligadoPorPadrao()
    {
        Verdade(!Configuracao.Padrao().MapeadorAtivo, "mapeador comeca desligado");
    }

    public static void Teste_Opcoes_Acentos()
    {
        var c = Configuracao.Padrao();
        Verdade(c.DigitarComAcentos, "padrao ligado");
        Verdade(Args(c).EndsWith("--prefer-text"), "manda texto pronto (acentos)");
        c.DigitarComAcentos = false;
        Verdade(!Args(c).Contains("--prefer-text"), "desligado para jogos");
    }

    // ---------- Mapeador ----------

    public static void Teste_Mapa_MensagemDeToque()
    {
        var b = MensagemToque.Criar(MensagemToque.Down, 7, 100, 2000, 1080, 2340, 1f);
        Igual(32, b.Length);
        Igual("02-00-00-00-00-00-00-00-00-07-00-00-00-64-00-00-07-D0-04-38-09-24-FF-FF-00-00-00-00-00-00-00-00",
            BitConverter.ToString(b));
        var up = MensagemToque.Criar(MensagemToque.Up, 7, 1, 2, 3, 4, 0f);
        Igual((byte)1, up[1]);
        Igual((byte)0, up[22]);
    }

    public static void Teste_Mapa_TelaDoDumpsysInput()
    {
        var saida = "  Viewport INTERNAL: displayId=0, uniqueId=local:1, port=135, orientation=1, densityDpi=450 " +
                    "logicalFrame=[0, 0, 2340, 1080], physicalFrame=[0, 0, 2340, 1080], deviceSize=[1080, 2340], isActive=[1]\n" +
                    "  Viewport INTERNAL: displayId=0, orientation=0, logicalFrame=[0, 0, 1080, 2340], isActive=[0]\n";
        var t = SaidaAdb.Tela(saida);
        Igual(2340, t.Largura); Igual(1080, t.Altura); Igual(1, t.Rotacao);
        Igual(null, SaidaAdb.Tela("nada aqui"));
    }

    public static void Teste_Mapa_PacoteEmFoco()
    {
        Igual("com.dts.freefireth", SaidaAdb.PacoteEmFoco(
            "  mCurrentFocus=Window{b7e493c u0 com.dts.freefireth/com.dts.freefireth.FFMainActivity}"));
        Igual(null, SaidaAdb.PacoteEmFoco("  mCurrentFocus=Window{b7e493c u0 NotificationShade}"));
        Igual(null, SaidaAdb.PacoteEmFoco("  mCurrentFocus=null"));
    }

    public static void Teste_Mapa_NormalizadoParaCelular()
    {
        var t = new Tela { Largura = 2340, Altura = 1080 };
        var p = Mapeamento.ParaCelular(0.5, 0.25, t);
        Igual(1170, p.X); Igual(270, p.Y);
        p = Mapeamento.ParaCelular(1.2, -0.1, t);   // fora da tela: encosta na borda
        Igual(2339, p.X); Igual(0, p.Y);
    }

    public static void Teste_Mapa_Analogico()
    {
        var t = new Tela { Largura = 2000, Altura = 1000 };
        var el = new ElementoMapa { Tipo = ElementoMapa.Analogico, X = 0.2, Y = 0.7, Raio = 0.05 };
        Verdade(Mapeamento.PontoAnalogico(el, t, false, false, false, false) == null, "nada apertado = solta");
        var p = Mapeamento.PontoAnalogico(el, t, true, false, false, false).Value;   // W: para cima
        Igual(400, p.X); Igual(600, p.Y);                                          // centro 400,700 - raio 100
        p = Mapeamento.PontoAnalogico(el, t, true, false, false, true).Value;      // W+D: diagonal
        Igual(471, p.X); Igual(629, p.Y);
        p = Mapeamento.PontoAnalogico(el, t, true, true, false, false).Value;      // W+S se anulam: centro
        Igual(400, p.X); Igual(700, p.Y);
    }

    public static void Teste_Mapa_Camera()
    {
        var t = new Tela { Largura = 2000, Altura = 1000 };
        var cam = new DedoCamera(new ElementoMapa { Tipo = ElementoMapa.Camera, X = 0.7, Y = 0.5, Sensibilidade = 2 }, t);
        var ev = cam.Mover(10, 0);   // primeiro movimento: encosta o dedo no centro e move
        Igual(2, ev.Count);
        Igual(MensagemToque.Down, ev[0].Acao); Igual(1400, ev[0].Ponto.X); Igual(500, ev[0].Ponto.Y);
        Igual(MensagemToque.Move, ev[1].Acao); Igual(1420, ev[1].Ponto.X);
        ev = cam.Mover(0, -5);
        Igual(1, ev.Count); Igual(1420, ev[0].Ponto.X); Igual(490, ev[0].Ponto.Y);
        ev = cam.Mover(300, 0);      // passou do raio (0,25 x 2000 = 500): levanta e recomeca do centro
        Igual(MensagemToque.Up, ev[0].Acao);
        Igual(MensagemToque.Down, ev[1].Acao); Igual(1400, ev[1].Ponto.X);
        Igual(MensagemToque.Move, ev.Last().Acao);
        Igual(1, cam.Soltar().Count);
        Igual(0, cam.Soltar().Count);
    }

    public static void Teste_Mapa_ArquivoIdaEVolta()
    {
        var caminho = ArquivoTemp();
        var m = new MapaTeclas();
        m.Elementos.Add(new ElementoMapa { Tipo = ElementoMapa.Toque, Tecla = "Space", X = 0.9, Y = 0.8 });
        m.Elementos.Add(ElementoMapa.NovoAnalogico(0.2, 0.7));
        MapaArquivo.Salvar(caminho, m);
        var lido = MapaArquivo.Ler(caminho);
        Igual(2, lido.Elementos.Count);
        Igual("Space", lido.Elementos[0].Tecla);
        Igual("W", lido.Elementos[1].Cima);
        Igual(0, MapaArquivo.Ler(ArquivoTemp()).Elementos.Count);
        Directory.Delete(Path.GetDirectoryName(caminho), true);
    }

    // ---------- ConfigArquivo ----------

    static string ArquivoTemp()
    {
        return Path.Combine(Path.GetTempPath(), "celular-remoto-teste-" + Guid.NewGuid().ToString("N"), "config.json");
    }

    public static void Teste_Config_IdaEVolta()
    {
        var caminho = ArquivoTemp();
        var c = Configuracao.Padrao();
        c.Fps = 120; c.Som = false; c.Gravar = true; c.UltimoSerial = "S9";
        ConfigArquivo.Salvar(caminho, c);
        var lido = ConfigArquivo.Ler(caminho);
        Igual(120, lido.Fps); Igual(false, lido.Som); Igual(true, lido.Gravar); Igual("S9", lido.UltimoSerial);
        Directory.Delete(Path.GetDirectoryName(caminho), true);
    }

    public static void Teste_Config_AusenteOuCorrompido()
    {
        Igual(1280, ConfigArquivo.Ler(ArquivoTemp()).Resolucao);
        var caminho = ArquivoTemp();
        Directory.CreateDirectory(Path.GetDirectoryName(caminho));
        File.WriteAllText(caminho, "isso nao e json {{{");
        Igual(60, ConfigArquivo.Ler(caminho).Fps);
        Directory.Delete(Path.GetDirectoryName(caminho), true);
    }

    public static void Teste_Config_ParcialEInvalido()
    {
        var caminho = ArquivoTemp();
        Directory.CreateDirectory(Path.GetDirectoryName(caminho));
        File.WriteAllText(caminho, "{\"Fps\":90,\"Resolucao\":555}");
        var c = ConfigArquivo.Ler(caminho);
        Igual(90, c.Fps);
        Igual(1280, c.Resolucao);
        Verdade(c.Som && c.ApagarTela && c.SempreNoTopo, "campos ausentes ficam no padrao");
        Directory.Delete(Path.GetDirectoryName(caminho), true);
    }

    public static void Teste_Opcoes_Normalizar()
    {
        var c = Configuracao.Padrao();
        c.Resolucao = 777; c.Fps = 0; c.BitrateMbps = -1;
        c.Normalizar();
        Igual(1280, c.Resolucao); Igual(60, c.Fps); Igual(8, c.BitrateMbps);
    }
}
