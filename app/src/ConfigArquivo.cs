// Le e salva a configuracao em JSON (%APPDATA%\CelularRemoto\config.json).
using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace CelularRemoto
{
    public static class ConfigArquivo
    {
        public static string CaminhoPadrao()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "CelularRemoto", "config.json");
        }

        // Arquivo ausente ou corrompido -> padroes (nunca falha)
        public static Configuracao Ler(string caminho)
        {
            try
            {
                using (var f = File.OpenRead(caminho))
                {
                    var c = (Configuracao)new DataContractJsonSerializer(typeof(Configuracao)).ReadObject(f);
                    c.Normalizar();
                    return c;
                }
            }
            catch (Exception)
            {
                return Configuracao.Padrao();
            }
        }

        public static void Salvar(string caminho, Configuracao c)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(caminho));
            using (var f = File.Create(caminho))
                new DataContractJsonSerializer(typeof(Configuracao)).WriteObject(f, c);
        }
    }
}
