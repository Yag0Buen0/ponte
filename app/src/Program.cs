using System;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace CelularRemoto
{
    static class Program
    {
        // Icone embutido no exe (/win32icon); sem ele, o icone padrao do Windows
        public static readonly Icon Icone = CarregarIcone();

        static Icon CarregarIcone()
        {
            try { return Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch (Exception) { return SystemIcons.Application; }
        }

        [STAThread]
        static void Main()
        {
            bool primeira;
            using (new Mutex(true, "CelularRemoto-instancia-unica", out primeira))
            {
                if (!primeira)
                {
                    MessageBox.Show("O Celular Remoto já está aberto.", "Celular Remoto",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                if (!Adb.FerramentasPresentes())
                {
                    MessageBox.Show("Não encontrei o scrcpy/adb. Reinstale o Celular Remoto.", "Celular Remoto",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                Application.Run(new Painel());
            }
        }
    }
}
