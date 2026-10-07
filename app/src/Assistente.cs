// Janela de pareamento: o usuario digita so o codigo de 6 digitos.
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CelularRemoto
{
    public class Assistente : Form
    {
        readonly TextBox codigo;
        readonly Button parear, tentar;
        readonly Label status;

        public Assistente()
        {
            Text = "Conectar celular";
            Font = new Font("Segoe UI", 10);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(500, 450);
            Icon = Program.Icone;

            var passos = new Label {
                Location = new Point(16, 12), Size = new Size(470, 250),
                Text = "Celular e PC precisam estar no mesmo Wi-Fi.\n\n" +
                       "No celular (Android 11 ou mais novo):\n" +
                       "1. Configurações > Sobre o telefone > toque 7 vezes em \"Número da versão\"\n" +
                       "    (Settings > About phone > Build number)\n" +
                       "2. Configurações > Opções do desenvolvedor > Depuração por Wi-Fi: ligue\n" +
                       "    (Developer options > Wireless debugging)\n" +
                       "3. Toque em \"Depuração por Wi-Fi\" > \"Parear dispositivo com código\"\n" +
                       "    (Pair device with pairing code)\n" +
                       "4. Digite aqui o código de 6 dígitos:"
            };
            codigo = new TextBox { Location = new Point(16, 266), Size = new Size(140, 34), MaxLength = 6, Font = new Font("Segoe UI", 14) };
            parear = new Button { Location = new Point(170, 266), Size = new Size(130, 36), Text = "Parear" };
            tentar = new Button { Location = new Point(312, 266), Size = new Size(172, 36), Text = "Tentar conectar" };
            status = new Label {
                Location = new Point(16, 316), Size = new Size(470, 124), ForeColor = Color.DarkSlateBlue,
                Text = "Já pareou antes? Ligue a Depuração por Wi-Fi no celular e clique em \"Tentar conectar\".\n\n" +
                       "Dica: dá pra pôr um botão no painel rápido do celular: Opções do desenvolvedor > " +
                       "Blocos de desenvolvedor das configurações rápidas > Depuração por Wi-Fi."
            };
            Controls.AddRange(new Control[] { passos, codigo, parear, tentar, status });
            AcceptButton = parear;

            parear.Click += Parear_Click;
            tentar.Click += Tentar_Click;
        }

        void MostrarStatus(string texto)
        {
            if (InvokeRequired) BeginInvoke(new Action<string>(MostrarStatus), texto);
            else status.Text = texto;
        }

        async void Parear_Click(object sender, EventArgs e)
        {
            if (!SaidaAdb.CodigoValido(codigo.Text)) { status.Text = "O código tem 6 dígitos."; return; }
            var texto = codigo.Text;
            await Executar(() => Adb.Parear(texto, MostrarStatus));
        }

        async void Tentar_Click(object sender, EventArgs e)
        {
            status.Text = "Procurando o celular...";
            await Executar(() => {
                var ok = Adb.EsperarAparelhos(8).Count > 0;
                if (!ok) MostrarStatus("Não encontrado. A Depuração por Wi-Fi está ligada e no mesmo Wi-Fi?");
                return ok;
            });
        }

        async Task Executar(Func<bool> acao)
        {
            parear.Enabled = tentar.Enabled = false;
            try
            {
                if (await Task.Run(acao)) { DialogResult = DialogResult.OK; Close(); }
            }
            finally
            {
                if (!IsDisposed) parear.Enabled = tentar.Enabled = true;
            }
        }
    }
}
