// Mapeador de teclas: modelo do mapa, mensagem de toque do scrcpy e calculos de posicao.
// Sem efeitos colaterais (testado em app\tests).
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CelularRemoto
{
    public struct Ponto
    {
        public int X, Y;
        public Ponto(int x, int y) { X = x; Y = y; }
    }

    // Tamanho logico atual da tela do celular (ja na rotacao atual)
    public class Tela
    {
        public int Largura, Altura, Rotacao;
    }

    // Mensagem INJECT_TOUCH_EVENT do protocolo de controle do scrcpy 4.1 (32 bytes, big-endian)
    public static class MensagemToque
    {
        public const byte Down = 0, Up = 1, Move = 2;

        public static byte[] Criar(byte acao, long pointerId, int x, int y, int largura, int altura, float pressao)
        {
            var b = new byte[32];
            b[0] = 2;   // TYPE_INJECT_TOUCH_EVENT
            b[1] = acao;
            Escrever(b, 2, (ulong)pointerId, 8);
            Escrever(b, 10, (uint)x, 4);
            Escrever(b, 14, (uint)y, 4);
            Escrever(b, 18, (uint)largura, 2);
            Escrever(b, 20, (uint)altura, 2);
            // pressao em ponto fixo u16: 1.0 -> 0xFFFF
            Escrever(b, 22, pressao >= 1f ? 0xFFFFu : (uint)(Math.Max(0f, pressao) * 65536f), 2);
            // actionButton e buttons ficam 0 (toque de dedo)
            return b;
        }

        static void Escrever(byte[] b, int pos, ulong valor, int tamanho)
        {
            for (int i = tamanho - 1; i >= 0; i--) { b[pos + i] = (byte)(valor & 0xFF); valor >>= 8; }
        }
    }

    [DataContract]
    public class ElementoMapa
    {
        public const string Toque = "toque", Analogico = "analogico", Camera = "camera";
        public const string MouseEsquerdo = "MouseEsquerdo", MouseDireito = "MouseDireito";

        [DataMember] public string Tipo;
        [DataMember] public string Tecla;                          // Toque: nome da tecla (Keys) ou Mouse*
        [DataMember] public string Cima, Baixo, Esquerda, Direita; // Analogico
        [DataMember] public double X, Y;                           // posicao normalizada 0..1
        [DataMember] public double Raio;                           // Analogico: fracao da largura
        [DataMember] public double Sensibilidade;                  // Camera

        public static ElementoMapa NovoAnalogico(double x, double y)
        {
            return new ElementoMapa { Tipo = Analogico, Cima = "W", Baixo = "S", Esquerda = "A", Direita = "D", X = x, Y = y, Raio = 0.06 };
        }

        public static ElementoMapa NovaCamera(double x, double y)
        {
            return new ElementoMapa { Tipo = Camera, X = x, Y = y, Sensibilidade = 1.0 };
        }
    }

    [DataContract]
    public class MapaTeclas
    {
        [DataMember] public List<ElementoMapa> Elementos = new List<ElementoMapa>();

        [OnDeserialized]
        void DepoisDeLer(StreamingContext c) { if (Elementos == null) Elementos = new List<ElementoMapa>(); }
    }

    public static class Mapeamento
    {
        // Posicao normalizada (0..1) -> pixel do celular na rotacao atual (encosta nas bordas)
        public static Ponto ParaCelular(double x, double y, Tela t)
        {
            return new Ponto(
                (int)Math.Round(Math.Min(Math.Max(x, 0), 1) * t.Largura) - (x >= 1 ? 1 : 0),
                (int)Math.Round(Math.Min(Math.Max(y, 0), 1) * t.Altura) - (y >= 1 ? 1 : 0));
        }

        // Ponto do dedo do analogico para as teclas apertadas; null = soltar o dedo
        public static Ponto? PontoAnalogico(ElementoMapa el, Tela t, bool cima, bool baixo, bool esquerda, bool direita)
        {
            if (!cima && !baixo && !esquerda && !direita) return null;
            double dx = (direita ? 1 : 0) - (esquerda ? 1 : 0);
            double dy = (baixo ? 1 : 0) - (cima ? 1 : 0);
            var comprimento = Math.Sqrt(dx * dx + dy * dy);
            if (comprimento > 0) { dx /= comprimento; dy /= comprimento; }
            var centro = ParaCelular(el.X, el.Y, t);
            var raio = el.Raio * t.Largura;
            return new Ponto((int)Math.Round(centro.X + dx * raio), (int)Math.Round(centro.Y + dy * raio));
        }
    }

    public class EventoToque
    {
        public byte Acao;
        public Ponto Ponto;
        public EventoToque(byte acao, Ponto ponto) { Acao = acao; Ponto = ponto; }
    }

    // Dedo virtual da camera: o mouse arrasta a partir do centro da area; ao passar do raio,
    // levanta e recomeca do centro (o jogador nao percebe)
    public class DedoCamera
    {
        public const double FracaoRaio = 0.25;

        readonly Ponto centro;
        readonly double raio, sensibilidade;
        double ox, oy;
        public bool Abaixado { get; private set; }

        public DedoCamera(ElementoMapa el, Tela t)
        {
            centro = Mapeamento.ParaCelular(el.X, el.Y, t);
            raio = FracaoRaio * t.Largura;
            sensibilidade = el.Sensibilidade > 0 ? el.Sensibilidade : 1.0;
        }

        Ponto Atual()
        {
            return new Ponto((int)Math.Round(centro.X + ox), (int)Math.Round(centro.Y + oy));
        }

        public List<EventoToque> Mover(double dx, double dy)
        {
            var ev = new List<EventoToque>();
            if (!Abaixado)
            {
                ev.Add(new EventoToque(MensagemToque.Down, centro));
                Abaixado = true;
                ox = oy = 0;
            }
            ox += dx * sensibilidade;
            oy += dy * sensibilidade;
            if (Math.Sqrt(ox * ox + oy * oy) > raio)
            {
                ev.Add(new EventoToque(MensagemToque.Up, Atual()));
                ev.Add(new EventoToque(MensagemToque.Down, centro));
                ox = dx * sensibilidade;
                oy = dy * sensibilidade;
                var c = Math.Sqrt(ox * ox + oy * oy);
                if (c > raio) { ox *= raio / c; oy *= raio / c; }
            }
            ev.Add(new EventoToque(MensagemToque.Move, Atual()));
            return ev;
        }

        public List<EventoToque> Soltar()
        {
            var ev = new List<EventoToque>();
            if (Abaixado) ev.Add(new EventoToque(MensagemToque.Up, Atual()));
            Abaixado = false;
            return ev;
        }
    }

    public static class MapaArquivo
    {
        public static string Pasta()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CelularRemoto", "mapas");
        }

        public static string CaminhoDoJogo(string pacote)
        {
            foreach (var c in Path.GetInvalidFileNameChars()) pacote = pacote.Replace(c, '_');
            return Path.Combine(Pasta(), pacote + ".json");
        }

        // Ausente ou corrompido -> mapa vazio
        public static MapaTeclas Ler(string caminho)
        {
            try
            {
                using (var f = File.OpenRead(caminho))
                    return (MapaTeclas)new DataContractJsonSerializer(typeof(MapaTeclas)).ReadObject(f);
            }
            catch (Exception)
            {
                return new MapaTeclas();
            }
        }

        public static void Salvar(string caminho, MapaTeclas m)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(caminho));
            using (var f = File.Create(caminho))
                new DataContractJsonSerializer(typeof(MapaTeclas)).WriteObject(f, m);
        }
    }
}
