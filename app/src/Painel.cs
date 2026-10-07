// Janela principal: configuracoes + seletor de celular + botao Iniciar.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CelularRemoto
{
    public class Painel : Form
    {
        static readonly Color CorAviso = Color.FromArgb(176, 96, 0);

        readonly string caminhoConfig = ConfigArquivo.CaminhoPadrao();
        Configuracao config;

        ComboBox celular, resolucao, quadros, qualidade;
        Button atualizar, restaurar, iniciar, abrirAssimMesmo;
        TaskCompletionSource<bool> pularEspera;
        CheckBox sempreNoTopo, telaCheia, apagarTela, desligarTela, mostrarToques, desligarDepuracao, som, gravar, acentos, manterAcordado;
        Label avisoDesempenho, avisoDepuracao, status;
        LinkLabel creditos;

        public Painel()
        {
            Text = "Celular Remoto";
            Font = new Font("Segoe UI", 10);
            Icon = Program.Icone;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(12);

            MontarTela();
            config = ConfigArquivo.Ler(caminhoConfig);
            MostrarConfig(config);
            Shown += async (s, e) => await AtualizarCelulares();
        }

        // ---------- montagem ----------

        static FlowLayoutPanel Coluna()
        {
            return new FlowLayoutPanel {
                FlowDirection = FlowDirection.TopDown, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Fill
            };
        }

        static FlowLayoutPanel Linha()
        {
            return new FlowLayoutPanel {
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false,
                AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0)
            };
        }

        static GroupBox Grupo(string titulo, params Control[] itens)
        {
            var g = new GroupBox { Text = titulo, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Width = 460, MinimumSize = new Size(460, 0), Padding = new Padding(10, 6, 10, 8) };
            var c = Coluna();
            c.Controls.AddRange(itens);
            g.Controls.Add(c);
            return g;
        }

        static Label Rotulo(string texto)
        {
            return new Label { Text = texto, Width = 100, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 6, 0, 0) };
        }

        static ComboBox Lista(int largura, params string[] itens)
        {
            var c = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = largura };
            c.Items.AddRange(itens);
            return c;
        }

        static CheckBox Opcao(string texto)
        {
            return new CheckBox { Text = texto, AutoSize = true };
        }

        static Label Aviso(string texto)
        {
            return new Label { Text = "⚠ " + texto, ForeColor = CorAviso, MaximumSize = new Size(430, 0), AutoSize = true, Visible = false };
        }

        void MontarTela()
        {
            celular = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 250 };
            atualizar = new Button { Text = "Atualizar", AutoSize = true };
            var linhaCelular = Linha();
            linhaCelular.Controls.AddRange(new Control[] { Rotulo("Celular"), celular, atualizar });

            resolucao = Lista(250, "Econômica (800p)", "Equilibrada (1280p)", "Alta (1920p) ⚠", "Máxima (resolução real) ⚠");
            quadros = Lista(250, "30 por segundo", "60 por segundo", "90 por segundo ⚠", "120 por segundo ⚠");
            qualidade = Lista(250, "Normal", "Alta ⚠", "Muito alta ⚠");
            avisoDesempenho = Aviso("Resolução, quadros ou qualidade altos gastam mais bateria e podem esquentar o celular.");

            var linhaRes = Linha(); linhaRes.Controls.AddRange(new Control[] { Rotulo("Resolução"), resolucao });
            var linhaFps = Linha(); linhaFps.Controls.AddRange(new Control[] { Rotulo("Quadros"), quadros });
            var linhaQual = Linha(); linhaQual.Controls.AddRange(new Control[] { Rotulo("Qualidade"), qualidade });

            sempreNoTopo = Opcao("Sempre por cima dos outros apps");
            telaCheia = Opcao("Abrir em tela cheia");
            apagarTela = Opcao("Apagar a tela do celular enquanto usa");
            manterAcordado = Opcao("Manter o celular acordado enquanto a janela estiver aberta");
            desligarTela = Opcao("Desligar a tela do celular ao fechar");
            mostrarToques = Opcao("Mostrar os toques na tela");
            desligarDepuracao = Opcao("Desligar a Depuração por Wi-Fi ao fechar (mais seguro)");
            avisoDepuracao = Aviso("Na próxima vez não tem conexão rápida: você vai precisar religar a Depuração por Wi-Fi no celular antes de clicar em Iniciar.");
            som = Opcao("Som do celular no PC");
            gravar = Opcao("Gravar a tela (salva na pasta Vídeos)");
            acentos = Opcao("Digitar com acentos (desligue para jogos que usam W A S D)");

            restaurar = new Button { Text = "Restaurar padrão", AutoSize = true };
            iniciar = new Button { Text = "▶  Iniciar", Width = 150, Height = 40, Font = new Font("Segoe UI", 11, FontStyle.Bold) };
            status = new Label { AutoSize = true, MaximumSize = new Size(460, 0), ForeColor = Color.DimGray, Margin = new Padding(0, 6, 0, 0) };
            creditos = new LinkLabel { Text = "Créditos e licenças", AutoSize = true, Margin = new Padding(0, 8, 0, 0) };
            abrirAssimMesmo = new Button { Text = "Abrir assim mesmo (vou digitar o PIN)", AutoSize = true, Visible = false };

            var botoes = new TableLayoutPanel { ColumnCount = 2, Width = 460, Height = 46, Margin = new Padding(0, 8, 0, 0) };
            botoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            botoes.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            restaurar.Anchor = AnchorStyles.Left;
            iniciar.Anchor = AnchorStyles.Right;
            botoes.Controls.Add(restaurar, 0, 0);
            botoes.Controls.Add(iniciar, 1, 0);

            var tudo = Coluna();
            tudo.Controls.AddRange(new Control[] {
                linhaCelular,
                Grupo("Imagem e desempenho", linhaRes, linhaFps, linhaQual, avisoDesempenho),
                Grupo("Janela", sempreNoTopo, telaCheia),
                Grupo("Celular", apagarTela, manterAcordado, desligarTela, mostrarToques, desligarDepuracao, avisoDepuracao),
                Grupo("Som", som),
                Grupo("Teclado", acentos),
                Grupo("Extras", gravar),
                botoes,
                status,
                abrirAssimMesmo,
                creditos
            });
            Controls.Add(tudo);
            AcceptButton = iniciar;

            EventHandler avisos = (s, e) => AtualizarAvisos();
            resolucao.SelectedIndexChanged += avisos;
            quadros.SelectedIndexChanged += avisos;
            qualidade.SelectedIndexChanged += avisos;
            desligarDepuracao.CheckedChanged += avisos;
            atualizar.Click += async (s, e) => await AtualizarCelulares();
            restaurar.Click += (s, e) => { var p = Configuracao.Padrao(); p.UltimoSerial = config.UltimoSerial; MostrarConfig(p); };
            iniciar.Click += async (s, e) => await Iniciar();
            creditos.LinkClicked += (s, e) => AbrirCreditos();
            abrirAssimMesmo.Click += (s, e) => { if (pularEspera != null) pularEspera.TrySetResult(true); };
        }

        // CREDITOS.md instalado ao lado do exe; sem ele, a pagina do projeto
        static void AbrirCreditos()
        {
            var arquivo = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CREDITOS.md");
            if (System.IO.File.Exists(arquivo)) System.Diagnostics.Process.Start("notepad.exe", "\"" + arquivo + "\"");
            else System.Diagnostics.Process.Start("https://github.com/Yag0Buen0/ponte/blob/main/CREDITOS.md");
        }

        // ---------- configuracao <-> tela ----------

        void MostrarConfig(Configuracao c)
        {
            resolucao.SelectedIndex = Array.IndexOf(Configuracao.Resolucoes, c.Resolucao);
            quadros.SelectedIndex = Array.IndexOf(Configuracao.Quadros, c.Fps);
            qualidade.SelectedIndex = Array.IndexOf(Configuracao.Bitrates, c.BitrateMbps);
            sempreNoTopo.Checked = c.SempreNoTopo;
            telaCheia.Checked = c.TelaCheia;
            apagarTela.Checked = c.ApagarTela;
            desligarTela.Checked = c.DesligarTelaAoFechar;
            mostrarToques.Checked = c.MostrarToques;
            desligarDepuracao.Checked = c.DesligarDepuracaoAoFechar;
            som.Checked = c.Som;
            gravar.Checked = c.Gravar;
            acentos.Checked = c.DigitarComAcentos;
            manterAcordado.Checked = c.ManterAcordado;
            AtualizarAvisos();
        }

        Configuracao LerTela()
        {
            return new Configuracao {
                Resolucao = Configuracao.Resolucoes[resolucao.SelectedIndex],
                Fps = Configuracao.Quadros[quadros.SelectedIndex],
                BitrateMbps = Configuracao.Bitrates[qualidade.SelectedIndex],
                SempreNoTopo = sempreNoTopo.Checked,
                TelaCheia = telaCheia.Checked,
                ApagarTela = apagarTela.Checked,
                DesligarTelaAoFechar = desligarTela.Checked,
                MostrarToques = mostrarToques.Checked,
                DesligarDepuracaoAoFechar = desligarDepuracao.Checked,
                Som = som.Checked,
                Gravar = gravar.Checked,
                DigitarComAcentos = acentos.Checked,
                ManterAcordado = manterAcordado.Checked,
                UltimoSerial = config.UltimoSerial
            };
        }

        void AtualizarAvisos()
        {
            if (resolucao.SelectedIndex < 0 || quadros.SelectedIndex < 0 || qualidade.SelectedIndex < 0) return;
            avisoDesempenho.Visible = Opcoes.AvisoDesempenho(LerTela());
            avisoDepuracao.Visible = desligarDepuracao.Checked;
        }

        void Salvar()
        {
            try { ConfigArquivo.Salvar(caminhoConfig, config); }
            catch (Exception ex) { status.Text = "Não consegui salvar as configurações: " + ex.Message; }
        }

        // ---------- celulares ----------

        void MostrarCelulares(List<Aparelho> lista)
        {
            celular.Items.Clear();
            foreach (var a in lista) celular.Items.Add(a);
            var ultimo = lista.FirstOrDefault(a => a.Id == config.UltimoSerial);
            if (ultimo != null) celular.SelectedItem = ultimo;
            else if (lista.Count == 1) celular.SelectedIndex = 0;
            status.Text = lista.Count == 0 ? "Nenhum celular conectado. Clique em Iniciar para conectar."
                : lista.Count > 1 && celular.SelectedIndex < 0 ? "Mais de um celular: escolha qual usar." : "";
        }

        async Task AtualizarCelulares()
        {
            Ocupado(true, "Procurando celulares...");
            try { MostrarCelulares(await Task.Run(() => Adb.EsperarAparelhos(3))); }
            finally { Ocupado(false, null); }
        }

        void Ocupado(bool sim, string texto)
        {
            iniciar.Enabled = atualizar.Enabled = celular.Enabled = !sim;
            UseWaitCursor = sim;
            if (texto != null) status.Text = texto;
        }

        // ---------- iniciar ----------

        async Task Iniciar()
        {
            config = LerTela();
            Salvar();

            Ocupado(true, "Procurando o celular...");
            Aparelho aparelho;
            try { aparelho = await EscolherAparelho(); }
            finally { Ocupado(false, null); }
            if (aparelho == null) return;

            config.UltimoSerial = aparelho.Id;
            Salvar();
            status.Text = "";

            // Estava dormindo: espera a animacao de acordar da Samsung terminar, senao ela acende
            // a tela de novo logo depois que o scrcpy apaga
            if (!await Task.Run(() => Adb.Acordar(aparelho.Serial))) await Task.Delay(1500);
            if (!await EsperarDesbloqueio(aparelho.Serial)) return;
            status.Text = "";
            Hide();
            try
            {
                var c = config;
                var r = await Task.Run(() => Adb.RodarScrcpy(c, aparelho.Serial));
                if (r.ErroAudio && config.Som)
                {
                    // PC sem saida de audio: reabre sem som e lembra disso
                    config.Som = false;
                    som.Checked = false;
                    Salvar();
                    c = config;
                    r = await Task.Run(() => Adb.RodarScrcpy(c, aparelho.Serial));
                }
                if (config.DesligarDepuracaoAoFechar)
                    await Task.Run(() => Adb.DesligarDepuracao(aparelho.Serial));
                if (r.ErroRapido != null)
                    MessageBox.Show(this, "O celular fechou com erro:\n" + r.ErroRapido, "Celular Remoto",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                Show();
                Activate();
            }
        }

        // A tela de bloqueio nao aparece no PC (fica preta): espera o usuario desbloquear no celular,
        // ou abre assim mesmo para quem digita o PIN as cegas pelo teclado
        async Task<bool> EsperarDesbloqueio(string serial)
        {
            if (!await Task.Run(() => Adb.Bloqueado(serial))) return true;
            status.Text = "Celular bloqueado: desbloqueie no celular. Abro sozinho assim que desbloquear.\n" +
                          "(A tela de bloqueio não aparece no PC, por segurança do Android.)";
            pularEspera = new TaskCompletionSource<bool>();
            abrirAssimMesmo.Visible = true;
            iniciar.Enabled = false;
            try
            {
                var fim = DateTime.Now.AddMinutes(2);
                while (DateTime.Now < fim)
                {
                    if (await Task.WhenAny(Task.Delay(1000), pularEspera.Task) == pularEspera.Task) return true;
                    if (!await Task.Run(() => Adb.Bloqueado(serial)))
                    {
                        // A animacao de desbloqueio (Samsung) acende a tela de novo logo depois;
                        // se o scrcpy abrir antes dela, o --turn-screen-off nao "pega"
                        await Task.Delay(1500);
                        return true;
                    }
                }
                status.Text = "Ainda bloqueado. Desbloqueie o celular e clique em Iniciar.";
                return false;
            }
            finally
            {
                abrirAssimMesmo.Visible = false;
                iniciar.Enabled = true;
                pularEspera = null;
            }
        }

        // Celular selecionado se ainda estiver conectado; senao procura; nada -> assistente de pareamento
        async Task<Aparelho> EscolherAparelho()
        {
            var escolhido = celular.SelectedItem as Aparelho;
            var lista = await Task.Run(() => Adb.EsperarAparelhos(4));
            if (lista.Count == 0)
            {
                using (var a = new Assistente()) a.ShowDialog(this);
                lista = await Task.Run(() => Adb.Listar());
            }
            if (escolhido != null && lista.All(a => a.Id != escolhido.Id)) escolhido = null;
            if (escolhido != null) config.UltimoSerial = escolhido.Id;
            MostrarCelulares(lista);

            if (lista.Count == 0) { status.Text = "Celular não conectado."; return null; }
            var atual = celular.SelectedItem as Aparelho;
            if (atual == null) { status.Text = "Mais de um celular: escolha qual usar e clique em Iniciar."; return null; }
            return atual;
        }
    }
}
