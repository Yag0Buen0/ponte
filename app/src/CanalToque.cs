// Segunda conexao de controle com o celular (scrcpy-server sem video/audio) para injetar
// toques rapidos e com varios dedos ao mesmo tempo.
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace CelularRemoto
{
    public class CanalToque : IDisposable
    {
        const string VersaoServidor = "4.1";
        const string CaminhoNoCelular = "/data/local/tmp/celular-remoto-server.jar";

        readonly string serial;
        Process servidor;
        TcpClient cliente;
        NetworkStream fluxo;
        int porta;
        readonly object trava = new object();

        public bool Conectado { get { return fluxo != null; } }

        public CanalToque(string serial) { this.serial = serial; }

        // Bloqueante: chamar fora da thread da interface
        public void Abrir()
        {
            var scid = new Random().Next(1, int.MaxValue).ToString("x8");
            var saida = Adb.Executar(10000, "-s", serial, "forward", "tcp:0", "localabstract:scrcpy_" + scid).Trim();
            if (!int.TryParse(saida, out porta)) throw new IOException("adb forward falhou: " + saida);

            // Copia propria: o scrcpy apaga a dele ao fechar e nao convem sobrescrever o arquivo em uso
            Adb.Executar(30000, "-s", serial, "push", Adb.ArquivoServidor(), CaminhoNoCelular);
            var comando = "CLASSPATH=" + CaminhoNoCelular + " app_process / com.genymobile.scrcpy.Server " +
                VersaoServidor + " scid=" + scid + " log_level=info video=false audio=false control=true " +
                "tunnel_forward=true send_device_meta=false clipboard_autosync=false power_on=false cleanup=false";
            servidor = Adb.Iniciar("-s", serial, "shell", comando);

            // O adb aceita a conexao mesmo antes do servidor ouvir: so vale quando chega o byte inicial
            for (int tentativa = 0; tentativa < 100; tentativa++)
            {
                var c = new TcpClient { NoDelay = true };
                try
                {
                    c.Connect("127.0.0.1", porta);
                    var f = c.GetStream();
                    f.ReadTimeout = 2000;
                    if (f.ReadByte() >= 0)
                    {
                        f.ReadTimeout = Timeout.Infinite;
                        cliente = c;
                        fluxo = f;
                        new Thread(Descartar) { IsBackground = true }.Start();
                        return;
                    }
                }
                catch (IOException) { }
                catch (SocketException) { }
                c.Close();
                Thread.Sleep(100);
            }
            Dispose();
            throw new IOException("O canal de toques não respondeu.");
        }

        // O celular pode mandar mensagens (ex.: area de transferencia); le e ignora para nao travar
        void Descartar()
        {
            var b = new byte[256];
            try { while (fluxo != null && fluxo.Read(b, 0, b.Length) > 0) { } }
            catch (Exception) { }
        }

        public void Tocar(byte acao, long pointerId, Ponto p, Tela t)
        {
            var msg = MensagemToque.Criar(acao, pointerId, p.X, p.Y, t.Largura, t.Altura, acao == MensagemToque.Up ? 0f : 1f);
            lock (trava)
            {
                if (fluxo == null) return;
                try { fluxo.Write(msg, 0, msg.Length); }
                catch (IOException) { }
            }
        }

        public void Dispose()
        {
            lock (trava)
            {
                if (fluxo != null) { try { fluxo.Close(); } catch (Exception) { } fluxo = null; }
                if (cliente != null) { cliente.Close(); cliente = null; }
            }
            if (servidor != null)
            {
                try { if (!servidor.HasExited) servidor.Kill(); } catch (Exception) { }
                servidor.Dispose();
                servidor = null;
            }
            if (porta > 0)
            {
                try { Adb.Executar(5000, "-s", serial, "forward", "--remove", "tcp:" + porta); } catch (Exception) { }
                porta = 0;
            }
        }
    }
}
