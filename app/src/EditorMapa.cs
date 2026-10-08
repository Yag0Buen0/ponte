// Editor do mapa: por cima da janela do celular, mostra uma foto da tela do jogo
// (um pouco escurecida) com os marcadores solidos.
// Clique seleciona e arrasta; com um marcador selecionado, aperte a tecla desejada
// (Ctrl+clique esquerdo/direito = botoes do mouse); Delete remove; rodinha ajusta
// raio do analogico / sensibilidade da camera; Esc cancela.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace CelularRemoto
{
    public class EditorMapa : Form
    {
        const int RaioToque = 24, RaioCamera = 44;

        public MapaTeclas Mapa { get; private set; }
        ElementoMapa selecionado;
        bool arrastando;
        readonly Label ajuda;
        readonly Image fundo;

        public EditorMapa(MapaTeclas mapa, string pacote, Rectangle area, Image fundo)
        {
            Mapa = mapa;
            this.fundo = fundo;
            Text = "Mapa de teclas";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = area;
            TopMost = true;
            ShowInTaskbar = false;
            BackColor = Color.Black;
            DoubleBuffered = true;
            KeyPreview = true;

            var barra = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, BackColor = Color.FromArgb(40, 40, 40),
                Padding = new Padding(4), WrapContents = true };
            barra.Controls.Add(Botao("+ Toque", (s, e) => Adicionar(new ElementoMapa { Tipo = ElementoMapa.Toque, X = 0.5, Y = 0.5 })));
            barra.Controls.Add(Botao("+ Analógico (WASD)", (s, e) => Adicionar(ElementoMapa.NovoAnalogico(0.18, 0.7))));
            barra.Controls.Add(Botao("+ Câmera", (s, e) => {
                if (Mapa.Elementos.Any(x => x.Tipo == ElementoMapa.Camera)) { Avisar("Já existe uma câmera neste mapa."); return; }
                Adicionar(ElementoMapa.NovaCamera(0.7, 0.45));
            }));
            barra.Controls.Add(Botao("Salvar", (s, e) => Salvar()));
            barra.Controls.Add(Botao("Cancelar", (s, e) => { DialogResult = DialogResult.Cancel; Close(); }));
            ajuda = new Label { AutoSize = true, ForeColor = Color.White, Font = new Font("Segoe UI", 9),
                Margin = new Padding(8, 6, 0, 0),
                Text = "App: " + pacote + "  ·  clique seleciona/arrasta · aperte a tecla · Ctrl+clique = botão do mouse · " +
                       "Delete remove · rodinha ajusta · Esc cancela" };
            barra.Controls.Add(ajuda);
            Controls.Add(barra);
        }

        static Button Botao(string texto, EventHandler acao)
        {
            var b = new Button { Text = texto, AutoSize = true, ForeColor = Color.White, FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(70, 70, 70), TabStop = false };
            b.Click += acao;
            return b;
        }

        void Avisar(string msg) { ajuda.Text = msg; }

        void Adicionar(ElementoMapa el)
        {
            Mapa.Elementos.Add(el);
            selecionado = el;
            Avisar(el.Tipo == ElementoMapa.Toque ? "Arraste para o botão do jogo e aperte a tecla (ou Ctrl+clique)."
                : el.Tipo == ElementoMapa.Analogico ? "Arraste para o analógico do jogo; a rodinha muda o tamanho."
                : "Arraste para a área da câmera; a rodinha muda a sensibilidade.");
            Focus();
            Invalidate();
        }

        void Salvar()
        {
            var semTecla = Mapa.Elementos.FirstOrDefault(e => e.Tipo == ElementoMapa.Toque && string.IsNullOrEmpty(e.Tecla));
            if (semTecla != null) { selecionado = semTecla; Avisar("Tem um toque sem tecla: selecione e aperte a tecla."); Invalidate(); return; }
            DialogResult = DialogResult.OK;
            Close();
        }

        // ---------- geometria ----------

        Point NaTela(ElementoMapa el)
        {
            return new Point((int)Math.Round(el.X * ClientSize.Width), (int)Math.Round(el.Y * ClientSize.Height));
        }

        int Raio(ElementoMapa el)
        {
            if (el.Tipo == ElementoMapa.Analogico) return Math.Max(20, (int)Math.Round(el.Raio * ClientSize.Width));
            return el.Tipo == ElementoMapa.Camera ? RaioCamera : RaioToque;
        }

        ElementoMapa Acertou(Point p)
        {
            // Os de cima (adicionados por ultimo) primeiro
            for (int i = Mapa.Elementos.Count - 1; i >= 0; i--)
            {
                var el = Mapa.Elementos[i];
                var c = NaTela(el);
                var dx = p.X - c.X; var dy = p.Y - c.Y;
                if (dx * dx + dy * dy <= Raio(el) * Raio(el)) return el;
            }
            return null;
        }

        // ---------- desenho ----------

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            if (fundo != null)
            {
                g.DrawImage(fundo, ClientRectangle);
                using (var veu = new SolidBrush(Color.FromArgb(90, 0, 0, 0))) g.FillRectangle(veu, ClientRectangle);
            }
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var fonte = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var pequena = new Font("Segoe UI", 8, FontStyle.Bold))
            {
                foreach (var el in Mapa.Elementos)
                {
                    var c = NaTela(el);
                    var r = Raio(el);
                    var cor = el == selecionado ? Color.Gold : Color.White;
                    using (var caneta = new Pen(cor, 3))
                    using (var preenchimento = new SolidBrush(Color.FromArgb(170, 20, 90, 200)))
                    using (var pincel = new SolidBrush(cor))
                    {
                        var circulo = new Rectangle(c.X - r, c.Y - r, 2 * r, 2 * r);
                        g.FillEllipse(preenchimento, circulo);
                        g.DrawEllipse(caneta, circulo);
                        if (el.Tipo == ElementoMapa.Toque)
                            Centralizar(g, Teclas.Exibir(el.Tecla), fonte, pincel, c);
                        else if (el.Tipo == ElementoMapa.Analogico)
                        {
                            Centralizar(g, el.Cima, fonte, pincel, new Point(c.X, c.Y - r + 12));
                            Centralizar(g, el.Baixo, fonte, pincel, new Point(c.X, c.Y + r - 12));
                            Centralizar(g, el.Esquerda, fonte, pincel, new Point(c.X - r + 12, c.Y));
                            Centralizar(g, el.Direita, fonte, pincel, new Point(c.X + r - 12, c.Y));
                        }
                        else
                        {
                            Centralizar(g, "Câmera", pequena, pincel, new Point(c.X, c.Y - 8));
                            Centralizar(g, "x" + el.Sensibilidade.ToString("0.0"), pequena, pincel, new Point(c.X, c.Y + 9));
                        }
                    }
                }
            }
        }

        static void Centralizar(Graphics g, string texto, Font f, Brush b, Point centro)
        {
            var t = g.MeasureString(texto, f);
            g.DrawString(texto, f, b, centro.X - t.Width / 2, centro.Y - t.Height / 2);
        }

        // ---------- mouse ----------

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (ModifierKeys == Keys.Control && selecionado != null && selecionado.Tipo == ElementoMapa.Toque &&
                (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right))
            {
                selecionado.Tecla = e.Button == MouseButtons.Left ? ElementoMapa.MouseEsquerdo : ElementoMapa.MouseDireito;
                Invalidate();
                return;
            }
            selecionado = Acertou(e.Location);
            arrastando = selecionado != null && e.Button == MouseButtons.Left;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!arrastando || selecionado == null || ClientSize.Width == 0 || ClientSize.Height == 0) return;
            selecionado.X = Math.Min(Math.Max(e.X / (double)ClientSize.Width, 0), 1);
            selecionado.Y = Math.Min(Math.Max(e.Y / (double)ClientSize.Height, 0), 1);
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); arrastando = false; }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (selecionado == null) return;
            var passo = e.Delta > 0 ? 1 : -1;
            if (selecionado.Tipo == ElementoMapa.Analogico)
                selecionado.Raio = Math.Min(Math.Max(selecionado.Raio + passo * 0.005, 0.02), 0.2);
            else if (selecionado.Tipo == ElementoMapa.Camera)
                selecionado.Sensibilidade = Math.Round(Math.Min(Math.Max(selecionado.Sensibilidade + passo * 0.1, 0.1), 5), 1);
            Invalidate();
        }

        // ---------- teclado ----------

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var tecla = keyData & Keys.KeyCode;
            if (tecla == Keys.Escape) { DialogResult = DialogResult.Cancel; Close(); return true; }
            if (selecionado == null) return base.ProcessCmdKey(ref msg, keyData);
            if (tecla == Keys.Delete)
            {
                Mapa.Elementos.Remove(selecionado);
                selecionado = null;
                Invalidate();
                return true;
            }
            if (selecionado.Tipo == ElementoMapa.Toque && tecla != Keys.F1 && tecla != Keys.F2 && tecla != Keys.None)
            {
                selecionado.Tecla = Teclas.Nome(tecla);
                Invalidate();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
