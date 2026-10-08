// Executa adb e scrcpy (embutidos na pasta scrcpy\ ao lado do exe, ou do PATH).
// Metodos bloqueantes: chamar fora da thread da interface (Task.Run).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace CelularRemoto
{
    public class Aparelho
    {
        public string Serial;   // como o adb enxerga a conexao (pode mudar: IP:porta ou nome mDNS)
        public string Id;       // ro.serialno (fixo do aparelho)
        public string Modelo;

        public override string ToString()
        {
            var fim = Id.Length > 4 ? Id.Substring(Id.Length - 4) : Id;
            return Modelo + "  (…" + fim + ")";
        }
    }

    public class ResultadoScrcpy
    {
        public bool ErroAudio;
        public string ErroRapido;   // primeira linha de erro, se fechou logo no inicio
    }

    public static class Adb
    {
        static string Ferramenta(string nome)
        {
            var embutido = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scrcpy", nome + ".exe");
            return File.Exists(embutido) ? embutido : nome;
        }

        public static string ArquivoServidor()
        {
            return Path.Combine(Path.GetDirectoryName(Ferramenta("scrcpy")) ?? "", "scrcpy-server");
        }

        public static bool FerramentasPresentes()
        {
            return File.Exists(Ferramenta("adb")) && File.Exists(Ferramenta("scrcpy"));
        }

        // O servidor do adb herda os pipes de quem o inicia; por isso ele sobe sem redirecionamento
        static void GarantirServidor()
        {
            var psi = new ProcessStartInfo(Ferramenta("adb"), "start-server") {
                UseShellExecute = false, CreateNoWindow = true
            };
            using (var p = Process.Start(psi)) p.WaitForExit(15000);
        }

        public static string Executar(int timeoutMs, params string[] args)
        {
            var psi = new ProcessStartInfo(Ferramenta("adb"), Opcoes.JuntarArgumentos(args)) {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            var saida = new StringBuilder();
            using (var p = new Process { StartInfo = psi })
            {
                DataReceivedEventHandler juntar = (s, e) => { if (e.Data != null) lock (saida) saida.AppendLine(e.Data); };
                p.OutputDataReceived += juntar;
                p.ErrorDataReceived += juntar;
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                if (!p.WaitForExit(timeoutMs)) { try { p.Kill(); } catch (InvalidOperationException) { } }
                else p.WaitForExit();   // garante que os eventos de saida terminaram
            }
            lock (saida) return saida.ToString();
        }

        // Processo do adb que continua rodando (ex.: servidor do canal de toques); saida descartada
        public static Process Iniciar(params string[] args)
        {
            var psi = new ProcessStartInfo(Ferramenta("adb"), Opcoes.JuntarArgumentos(args)) {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            var p = Process.Start(psi);
            p.OutputDataReceived += (s, e) => { };
            p.ErrorDataReceived += (s, e) => { };
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            return p;
        }

        // Foto da tela atual do celular (PNG); null se falhar
        public static System.Drawing.Image CapturarTela(string serial)
        {
            var psi = new ProcessStartInfo(Ferramenta("adb"), Opcoes.JuntarArgumentos(new[] { "-s", serial, "exec-out", "screencap", "-p" })) {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true
            };
            try
            {
                using (var p = Process.Start(psi))
                {
                    var memoria = new MemoryStream();
                    var copia = p.StandardOutput.BaseStream.CopyToAsync(memoria);
                    if (!copia.Wait(15000)) { try { p.Kill(); } catch (InvalidOperationException) { } return null; }
                    p.WaitForExit(5000);
                    memoria.Position = 0;
                    return memoria.Length > 0 ? System.Drawing.Image.FromStream(memoria) : null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Tela LerTela(string serial)
        {
            return SaidaAdb.Tela(Executar(8000, "-s", serial, "shell", "dumpsys input | grep 'Viewport INTERNAL'"));
        }

        public static string PacoteEmFoco(string serial)
        {
            return SaidaAdb.PacoteEmFoco(Executar(8000, "-s", serial, "shell", "dumpsys window | grep mCurrentFocus"));
        }

        static string Prop(string serial, string nome)
        {
            return Executar(8000, "-s", serial, "shell", "getprop", nome).Trim();
        }

        // Um item por aparelho: o mesmo celular visto por IP e por mDNS vira um so (prefere o nome mDNS)
        public static List<Aparelho> Listar()
        {
            GarantirServidor();
            var seriais = SaidaAdb.SeriaisProntos(Executar(10000, "devices"))
                .OrderByDescending(s => s.Contains("_adb-tls-connect"));
            var lista = new List<Aparelho>();
            foreach (var serial in seriais)
            {
                var id = Prop(serial, "ro.serialno");
                if (id.Length == 0) id = serial;
                if (lista.Any(a => a.Id == id)) continue;
                var modelo = Prop(serial, "ro.product.model");
                lista.Add(new Aparelho { Serial = serial, Id = id, Modelo = modelo.Length > 0 ? modelo : "Celular" });
            }
            return lista;
        }

        public static bool TentarConectarPorMdns()
        {
            GarantirServidor();
            var conexoes = SaidaAdb.ServicosMdns(Executar(10000, "mdns", "services")).Where(s => !s.Pareamento).ToList();
            foreach (var s in conexoes) Executar(10000, "connect", s.Endereco);
            return conexoes.Count > 0;
        }

        // Repete a busca por alguns segundos (o adb reconecta sozinho via mDNS logo depois de subir)
        public static List<Aparelho> EsperarAparelhos(int segundos)
        {
            var fim = DateTime.Now.AddSeconds(segundos);
            while (true)
            {
                var lista = Listar();
                if (lista.Count > 0) return lista;
                if (TentarConectarPorMdns())
                {
                    lista = Listar();
                    if (lista.Count > 0) return lista;
                }
                if (DateTime.Now >= fim) return lista;
                Thread.Sleep(700);
            }
        }

        // Pareia pelo codigo de 6 digitos; a porta de pareamento e achada por mDNS. true = conectou.
        public static bool Parear(string codigo, Action<string> status)
        {
            GarantirServidor();
            status("Procurando o celular na rede...");
            ServicoMdns pareamento = null;
            var fim = DateTime.Now.AddSeconds(15);
            while (pareamento == null && DateTime.Now < fim)
            {
                pareamento = SaidaAdb.ServicosMdns(Executar(10000, "mdns", "services")).FirstOrDefault(s => s.Pareamento);
                if (pareamento == null) Thread.Sleep(700);
            }
            if (pareamento == null)
            {
                status("Não achei o celular. Deixe aberta a tela \"Parear dispositivo com código\" e confira o Wi-Fi.");
                return false;
            }

            status("Pareando...");
            if (!SaidaAdb.PareamentoOk(Executar(20000, "pair", pareamento.Endereco, codigo.Trim())))
            {
                status("O pareamento falhou. Gere um código novo no celular e tente de novo.");
                return false;
            }

            status("Pareado! Conectando...");
            if (EsperarAparelhos(15).Count > 0) return true;
            status("Pareado, mas não conectou. Clique em \"Tentar conectar\".");
            return false;
        }

        // Celular dormindo = imagem preta no scrcpy; acorda antes (o desbloqueio fica com o usuario).
        // true = ja estava acordado
        public static bool Acordar(string serial)
        {
            if (SaidaAdb.Acordado(Executar(8000, "-s", serial, "shell", "dumpsys power | grep mWakefulness="))) return true;
            Executar(8000, "-s", serial, "shell", "input", "keyevent", "KEYCODE_WAKEUP");
            return false;
        }

        public static bool Bloqueado(string serial)
        {
            return SaidaAdb.Bloqueado(Executar(8000, "-s", serial, "shell",
                "dumpsys window | grep isKeyguardShowing"));
        }

        public static void DesligarDepuracao(string serial)
        {
            Executar(8000, "-s", serial, "shell", "settings", "put", "global", "adb_wifi_enabled", "0");
        }

        // Roda o scrcpy ate a janela fechar
        public static ResultadoScrcpy RodarScrcpy(Configuracao c, string serial, Action<Process> aoAbrirJanela = null)
        {
            var args = Opcoes.MontarArgumentos(c, serial, DateTime.Now,
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos));
            var psi = new ProcessStartInfo(Ferramenta("scrcpy"), Opcoes.JuntarArgumentos(args)) {
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            };
            var log = new StringBuilder();
            var inicio = DateTime.Now;
            using (var p = new Process { StartInfo = psi })
            {
                DataReceivedEventHandler juntar = (s, e) => { if (e.Data != null) lock (log) log.AppendLine(e.Data); };
                p.OutputDataReceived += juntar;
                p.ErrorDataReceived += juntar;
                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                if (aoAbrirJanela != null)
                {
                    // Espera a janela existir (o mapeador precisa dela)
                    for (int i = 0; i < 100 && !p.HasExited; i++)
                    {
                        p.Refresh();
                        if (p.MainWindowHandle != IntPtr.Zero) { aoAbrirJanela(p); break; }
                        Thread.Sleep(100);
                    }
                }
                p.WaitForExit();
                var texto = log.ToString();
                var rapido = (DateTime.Now - inicio).TotalSeconds < 15;
                return new ResultadoScrcpy {
                    ErroAudio = SaidaAdb.ErroDeAudio(texto),
                    ErroRapido = rapido && p.ExitCode != 0 ? SaidaAdb.PrimeiroErro(texto) : null
                };
            }
        }
    }
}
