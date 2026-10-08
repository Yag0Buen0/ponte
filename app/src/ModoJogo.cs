// Modo jogo: com a janela do celular em primeiro plano, F1 liga/desliga e F2 abre o editor.
// Ligado, teclas e cliques mapeados viram toques (pelo CanalToque) e nao chegam ao scrcpy;
// o mouse vira a camera, com o cursor preso no centro da janela.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CelularRemoto
{
    public static class Teclas
    {
        // Lado esquerdo/direito viram a mesma tecla (o editor e o hook enxergam nomes diferentes)
        public static string Nome(Keys k)
        {
            switch (k)
            {
                case Keys.LShiftKey: case Keys.RShiftKey: return Keys.ShiftKey.ToString();
                case Keys.LControlKey: case Keys.RControlKey: return Keys.ControlKey.ToString();
                case Keys.LMenu: case Keys.RMenu: return Keys.Menu.ToString();
                default: return k.ToString();
            }
        }

        public static string Exibir(string nome)
        {
            switch (nome)
            {
                case null: return "?";
                case ElementoMapa.MouseEsquerdo: return "Clique";
                case ElementoMapa.MouseDireito: return "Clique dir.";
                case "Space": return "Espaço";
                case "ShiftKey": return "Shift";
                case "ControlKey": return "Ctrl";
                case "Menu": return "Alt";
                case "Return": return "Enter";
                case "Tab": return "Tab";
                default: return nome.Length == 2 && nome[0] == 'D' && char.IsDigit(nome[1]) ? nome.Substring(1) : nome;
            }
        }
    }

    public class ModoJogo : IDisposable
    {
        const long IdCamera = 1, IdAnalogico = 2, IdBase = 10;

        readonly string serial;
        readonly IntPtr janela;
        readonly Control ui;
        readonly Nativo.HookProc procTeclado, procMouse;   // referencias mantidas para o GC nao coletar
        IntPtr hookTeclado, hookMouse;
        readonly Timer relogio;
        readonly Etiqueta etiqueta;

        CanalToque canal;
        Tela tela;
        MapaTeclas mapa = new MapaTeclas();
        string pacote;
        bool ativo, ativando, editando;
        readonly HashSet<string> apertadas = new HashSet<string>();
        readonly HashSet<long> dedosAbaixados = new HashSet<long>();
        bool analogicoAbaixado;
        DedoCamera camera;
        DateTime ultimoMovimento, ultimaLeituraTela;
        Nativo.PONTO centroFisico;

        public ModoJogo(string serial, IntPtr janela, Control ui)
        {
            this.serial = serial;
            this.janela = janela;
            this.ui = ui;
            procTeclado = AoTeclado;
            procMouse = AoMouse;
            var modulo = Nativo.GetModuleHandle(null);
            hookTeclado = Nativo.SetWindowsHookEx(Nativo.WH_KEYBOARD_LL, procTeclado, modulo, 0);
            hookMouse = Nativo.SetWindowsHookEx(Nativo.WH_MOUSE_LL, procMouse, modulo, 0);
            etiqueta = new Etiqueta();
            relogio = new Timer { Interval = 50 };
            relogio.Tick += AoRelogio;
            relogio.Start();
        }

        bool JanelaEmFoco { get { return Nativo.GetForegroundWindow() == janela; } }

        // ---------- ligar / desligar ----------

        async void Alternar()
        {
            if (ativo) { Desativar(); return; }
            if (ativando || editando) return;
            ativando = true;
            Mostrar("Preparando o modo jogo...");
            try
            {
                var s = serial;
                var r = await Task.Run(() => {
                    var p = Adb.PacoteEmFoco(s) ?? "geral";
                    return Tuple.Create(p, Adb.LerTela(s), MapaArquivo.Ler(MapaArquivo.CaminhoDoJogo(p)));
                });
                pacote = r.Item1; tela = r.Item2; mapa = r.Item3;
                if (tela == null) { Mostrar("Não consegui ler a tela do celular.", 4000); return; }
                if (mapa.Elementos.Count == 0) { Mostrar("Sem mapa para este app. Aperte F2 para criar.", 5000); return; }
                if (canal == null || !canal.Conectado)
                {
                    var c = new CanalToque(serial);
                    await Task.Run(() => c.Abrir());
                    canal = c;
                }
                if (!JanelaEmFoco) { Esconder(); return; }
                Ativar();
            }
            catch (Exception ex)
            {
                Mostrar("Modo jogo falhou: " + ex.Message, 6000);
            }
            finally { ativando = false; }
        }

        void Ativar()
        {
            var el = mapa.Elementos.FirstOrDefault(e => e.Tipo == ElementoMapa.Camera);
            camera = el != null ? new DedoCamera(el, tela) : null;
            ultimaLeituraTela = DateTime.Now;
            if (camera != null)
            {
                var area = Nativo.AreaCliente(janela);
                Nativo.SetCursorPos(area.Left + area.Width / 2, area.Top + area.Height / 2);
                Nativo.GetPhysicalCursorPos(out centroFisico);
                var r = new Nativo.RECT { Esquerda = area.Left, Topo = area.Top, Direita = area.Right, Base = area.Bottom };
                Nativo.ClipCursor(ref r);
            }
            ativo = true;
            Mostrar("MODO JOGO  ·  F1 sai  ·  F2 edita");
        }

        void Desativar()
        {
            SoltarTudo();
            ativo = false;
            Nativo.LiberarCursor(IntPtr.Zero);
            Esconder();
        }

        void SoltarTudo()
        {
            if (canal != null && tela != null)
            {
                foreach (var id in dedosAbaixados.ToList()) canal.Tocar(MensagemToque.Up, id, new Ponto(0, 0), tela);
                if (analogicoAbaixado) canal.Tocar(MensagemToque.Up, IdAnalogico, new Ponto(0, 0), tela);
                if (camera != null) Enviar(IdCamera, camera.Soltar());
            }
            dedosAbaixados.Clear();
            analogicoAbaixado = false;
            apertadas.Clear();
        }

        // ---------- editor ----------

        async void AbrirEditor()
        {
            if (editando || ativando) return;
            if (ativo) Desativar();
            editando = true;
            try
            {
                var s = serial;
                var p = await Task.Run(() => Adb.PacoteEmFoco(s) ?? "geral");
                var caminho = MapaArquivo.CaminhoDoJogo(p);
                using (var editor = new EditorMapa(MapaArquivo.Ler(caminho), p, Nativo.AreaCliente(janela)))
                {
                    if (editor.ShowDialog() == DialogResult.OK)
                    {
                        MapaArquivo.Salvar(caminho, editor.Mapa);
                        Mostrar("Mapa salvo. Aperte F1 para jogar.", 4000);
                    }
                }
            }
            catch (Exception ex)
            {
                Mostrar("Editor falhou: " + ex.Message, 6000);
            }
            finally
            {
                editando = false;
                Nativo.SetForegroundWindow(janela);
            }
        }

        // ---------- toques ----------

        void Enviar(long id, IEnumerable<EventoToque> eventos)
        {
            foreach (var e in eventos) canal.Tocar(e.Acao, id, e.Ponto, tela);
        }

        bool TeclaDoAnalogico(ElementoMapa a, string nome)
        {
            return nome == a.Cima || nome == a.Baixo || nome == a.Esquerda || nome == a.Direita;
        }

        void AtualizarAnalogico(ElementoMapa a)
        {
            var p = Mapeamento.PontoAnalogico(a, tela, apertadas.Contains(a.Cima), apertadas.Contains(a.Baixo),
                apertadas.Contains(a.Esquerda), apertadas.Contains(a.Direita));
            if (p == null)
            {
                if (analogicoAbaixado) canal.Tocar(MensagemToque.Up, IdAnalogico, Mapeamento.ParaCelular(a.X, a.Y, tela), tela);
                analogicoAbaixado = false;
                return;
            }
            if (!analogicoAbaixado)
            {
                canal.Tocar(MensagemToque.Down, IdAnalogico, Mapeamento.ParaCelular(a.X, a.Y, tela), tela);
                analogicoAbaixado = true;
            }
            canal.Tocar(MensagemToque.Move, IdAnalogico, p.Value, tela);
        }

        // true = a tecla/botao e do mapa (foi tratada e nao vai para o scrcpy)
        bool Processar(string nome, bool apertou)
        {
            bool tratado = false;
            if (apertou) { if (!apertadas.Add(nome)) return true; }   // repeticao automatica do teclado
            else apertadas.Remove(nome);

            for (int i = 0; i < mapa.Elementos.Count; i++)
            {
                var el = mapa.Elementos[i];
                if (el.Tipo == ElementoMapa.Toque && el.Tecla == nome)
                {
                    long id = IdBase + i;
                    var p = Mapeamento.ParaCelular(el.X, el.Y, tela);
                    canal.Tocar(apertou ? MensagemToque.Down : MensagemToque.Up, id, p, tela);
                    if (apertou) dedosAbaixados.Add(id); else dedosAbaixados.Remove(id);
                    tratado = true;
                }
                else if (el.Tipo == ElementoMapa.Analogico && TeclaDoAnalogico(el, nome))
                {
                    AtualizarAnalogico(el);
                    tratado = true;
                }
            }
            if (!tratado) apertadas.Remove(nome);
            return tratado;
        }

        // ---------- hooks ----------

        IntPtr AoTeclado(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var k = (Nativo.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Nativo.KBDLLHOOKSTRUCT));
                int msg = wParam.ToInt32();
                bool apertou = msg == Nativo.WM_KEYDOWN || msg == Nativo.WM_SYSKEYDOWN;
                bool soltou = msg == Nativo.WM_KEYUP || msg == Nativo.WM_SYSKEYUP;
                if ((k.flags & Nativo.LLKHF_INJECTED) == 0 && (apertou || soltou) && !editando && JanelaEmFoco)
                {
                    var tecla = (Keys)k.vkCode;
                    if (tecla == Keys.F1 || tecla == Keys.F2)
                    {
                        if (apertou) ui.BeginInvoke(tecla == Keys.F1 ? (Action)Alternar : AbrirEditor);
                        return (IntPtr)1;
                    }
                    if (ativo && Processar(Teclas.Nome(tecla), apertou)) return (IntPtr)1;
                }
            }
            return Nativo.CallNextHookEx(hookTeclado, nCode, wParam, lParam);
        }

        IntPtr AoMouse(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && ativo && JanelaEmFoco)
            {
                var m = (Nativo.MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(Nativo.MSLLHOOKSTRUCT));
                if ((m.flags & Nativo.LLMHF_INJECTED) == 0)
                {
                    switch (wParam.ToInt32())
                    {
                        case Nativo.WM_MOUSEMOVE:
                            if (camera == null) break;
                            int dx = m.pt.X - centroFisico.X, dy = m.pt.Y - centroFisico.Y;
                            if (dx != 0 || dy != 0)
                            {
                                Enviar(IdCamera, camera.Mover(dx, dy));
                                ultimoMovimento = DateTime.Now;
                            }
                            return (IntPtr)1;   // bloqueia: o cursor fica parado no centro
                        case Nativo.WM_LBUTTONDOWN: Processar(ElementoMapa.MouseEsquerdo, true); return (IntPtr)1;
                        case Nativo.WM_LBUTTONUP: Processar(ElementoMapa.MouseEsquerdo, false); return (IntPtr)1;
                        case Nativo.WM_RBUTTONDOWN: Processar(ElementoMapa.MouseDireito, true); return (IntPtr)1;
                        case Nativo.WM_RBUTTONUP: Processar(ElementoMapa.MouseDireito, false); return (IntPtr)1;
                        case Nativo.WM_MBUTTONDOWN:
                        case Nativo.WM_MBUTTONUP:
                        case Nativo.WM_MOUSEWHEEL:
                            return (IntPtr)1;
                    }
                }
            }
            return Nativo.CallNextHookEx(hookMouse, nCode, wParam, lParam);
        }

        // ---------- relogio (50 ms) ----------

        void AoRelogio(object sender, EventArgs e)
        {
            if (!Nativo.IsWindow(janela)) return;
            etiqueta.Posicionar(Nativo.AreaCliente(janela));
            if (!ativo) return;

            // Alt+Tab, clique fora etc.: solta tudo e libera o cursor
            if (!JanelaEmFoco) { Desativar(); return; }

            // Mouse parado: levanta o dedo da camera (o proximo movimento recomeca do centro)
            if (camera != null && camera.Abaixado && (DateTime.Now - ultimoMovimento).TotalMilliseconds > 150)
                Enviar(IdCamera, camera.Soltar());

            // Jogos giram a tela: relê tamanho/rotacao a cada 2 s
            if ((DateTime.Now - ultimaLeituraTela).TotalSeconds >= 2)
            {
                ultimaLeituraTela = DateTime.Now;
                var s = serial;
                Task.Run(() => Adb.LerTela(s)).ContinueWith(t => {
                    var nova = t.Result;
                    if (nova == null || tela == null || !ativo) return;
                    if (nova.Largura != tela.Largura || nova.Altura != tela.Altura)
                    {
                        SoltarTudo();
                        tela = nova;
                        var el = mapa.Elementos.FirstOrDefault(x => x.Tipo == ElementoMapa.Camera);
                        camera = el != null ? new DedoCamera(el, tela) : null;
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
        }

        // ---------- etiqueta ----------

        void Mostrar(string texto, int ms = 0) { etiqueta.Mostrar(texto, ms, Nativo.AreaCliente(janela)); }
        void Esconder() { etiqueta.Esconder(); }

        public void Dispose()
        {
            relogio.Stop();
            relogio.Dispose();
            if (ativo) Desativar();
            Nativo.LiberarCursor(IntPtr.Zero);
            if (hookTeclado != IntPtr.Zero) { Nativo.UnhookWindowsHookEx(hookTeclado); hookTeclado = IntPtr.Zero; }
            if (hookMouse != IntPtr.Zero) { Nativo.UnhookWindowsHookEx(hookMouse); hookMouse = IntPtr.Zero; }
            etiqueta.Dispose();
            if (canal != null)
            {
                var c = canal;
                canal = null;
                Task.Run(() => c.Dispose());
            }
        }
    }

    // Aviso pequeno no topo da janela do celular; nao pega foco nem cliques
    class Etiqueta : Form
    {
        readonly Label texto;
        readonly Timer sumir = new Timer();

        public Etiqueta()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(30, 30, 30);
            Opacity = 0.85;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            texto = new Label { AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(8, 4, 8, 4) };
            Controls.Add(texto);
            sumir.Tick += (s, e) => Esconder();
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x08000000 | 0x80 | 0x20 | 0x8;   // NOACTIVATE, TOOLWINDOW, TRANSPARENT, TOPMOST
                return cp;
            }
        }

        public void Mostrar(string msg, int ms, Rectangle area)
        {
            texto.Text = msg;
            sumir.Stop();
            if (ms > 0) { sumir.Interval = ms; sumir.Start(); }
            Posicionar(area);
            if (!Visible) Show();
        }

        public void Esconder() { sumir.Stop(); Hide(); }

        public void Posicionar(Rectangle area)
        {
            if (Visible) Location = new Point(area.Left + 8, area.Top + 8);
        }
    }
}
