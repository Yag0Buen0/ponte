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
              "--render-driver=opengl --max-size=1280 --max-fps=60 --video-bit-rate=8M --always-on-top --turn-screen-off",
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
