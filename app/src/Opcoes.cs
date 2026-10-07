// Configuracao do usuario e traducao dela para argumentos do scrcpy. Sem efeitos colaterais.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CelularRemoto
{
    [DataContract]
    public class Configuracao
    {
        public static readonly int[] Resolucoes = { 800, 1280, 1920, 0 };   // 0 = maxima (sem limite)
        public static readonly int[] Quadros = { 30, 60, 90, 120 };
        public static readonly int[] Bitrates = { 8, 16, 24 };              // Mbps

        [DataMember] public int Resolucao;
        [DataMember] public int Fps;
        [DataMember] public int BitrateMbps;
        [DataMember] public bool SempreNoTopo;
        [DataMember] public bool TelaCheia;
        [DataMember] public bool ApagarTela;
        [DataMember] public bool DesligarTelaAoFechar;
        [DataMember] public bool MostrarToques;
        [DataMember] public bool Som;
        [DataMember] public bool Gravar;
        [DataMember] public bool DesligarDepuracaoAoFechar;
        [DataMember] public bool DigitarComAcentos;
        [DataMember] public bool ManterAcordado;
        [DataMember] public string UltimoSerial;

        public static Configuracao Padrao()
        {
            var c = new Configuracao();
            c.AplicarPadroes();
            return c;
        }

        void AplicarPadroes()
        {
            Resolucao = 1280; Fps = 60; BitrateMbps = 8;
            SempreNoTopo = true; TelaCheia = false; ApagarTela = true;
            DesligarTelaAoFechar = false; MostrarToques = false; Som = true;
            Gravar = false; DesligarDepuracaoAoFechar = false; DigitarComAcentos = true; ManterAcordado = true; UltimoSerial = null;
        }

        // O desserializador nao chama construtor: campos ausentes no JSON ficam com o padrao
        [OnDeserializing]
        void AntesDeLer(StreamingContext contexto) { AplicarPadroes(); }

        public void Normalizar()
        {
            var p = Padrao();
            if (!Resolucoes.Contains(Resolucao)) Resolucao = p.Resolucao;
            if (!Quadros.Contains(Fps)) Fps = p.Fps;
            if (!Bitrates.Contains(BitrateMbps)) BitrateMbps = p.BitrateMbps;
        }
    }

    public static class Opcoes
    {
        public static List<string> MontarArgumentos(Configuracao c, string serial, DateTime agora, string pastaVideos)
        {
            var a = new List<string> {
                "-s", serial,
                "--window-title=Celular Remoto",
                "--no-window-aspect-ratio-lock", "--render-fit=stretched",
                // Com a tela do celular apagada (Samsung), o Direct3D descartava quadros em rajadas; OpenGL fica fluido
                "--render-driver=opengl"
            };
            if (c.Resolucao > 0) a.Add("--max-size=" + c.Resolucao);
            a.Add("--max-fps=" + c.Fps);
            a.Add("--video-bit-rate=" + c.BitrateMbps + "M");
            if (c.SempreNoTopo) a.Add("--always-on-top");
            if (c.TelaCheia) a.Add("--fullscreen");
            if (c.ApagarTela) a.Add("--turn-screen-off");
            // Com a tela fisica apagada ninguem "mexe" no celular: no tempo de tela dele (ex.: 30 s)
            // ele dorme de verdade e a janela fica preta. O scrcpy devolve o valor original ao fechar.
            if (c.ManterAcordado) a.Add("--screen-off-timeout=3600");
            if (c.DesligarTelaAoFechar) a.Add("--power-off-on-close");
            if (c.MostrarToques) a.Add("--show-touches");
            if (!c.Som) a.Add("--no-audio");
            if (c.Gravar) a.Add("--record=" + CaminhoGravacao(pastaVideos, agora));
            // Letras como texto pronto: acentos (´ + a = á) chegam certos; atrapalha jogos (WASD)
            if (c.DigitarComAcentos) a.Add("--prefer-text");
            return a;
        }

        public static string CaminhoGravacao(string pastaVideos, DateTime agora)
        {
            return Path.Combine(pastaVideos, "Celular Remoto " + agora.ToString("yyyy-MM-dd HH-mm-ss") + ".mp4");
        }

        // Niveis que gastam mais bateria e esquentam o celular
        public static bool AvisoDesempenho(Configuracao c)
        {
            return c.Resolucao == 1920 || c.Resolucao == 0 || c.Fps > 60 || c.BitrateMbps > 8;
        }

        // Monta a linha de comando do Windows: aspas em argumento com espaco ou aspas
        public static string JuntarArgumentos(IEnumerable<string> args)
        {
            return string.Join(" ", args.Select(Citar));
        }

        static string Citar(string arg)
        {
            if (arg.Length > 0 && arg.IndexOfAny(new[] { ' ', '\t', '"' }) < 0) return arg;
            var sb = new StringBuilder("\"");
            foreach (var ch in arg)
            {
                if (ch == '"') sb.Append('\\');
                sb.Append(ch);
            }
            return sb.Append('"').ToString();
        }
    }
}
