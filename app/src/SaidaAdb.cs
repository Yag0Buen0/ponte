// Interpreta a saida do adb e do scrcpy. Sem efeitos colaterais.
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CelularRemoto
{
    public class ServicoMdns
    {
        public string Nome;
        public bool Pareamento;   // _adb-tls-pairing (true) ou _adb-tls-connect (false)
        public string Endereco;   // IP:porta
    }

    public static class SaidaAdb
    {
        static IEnumerable<string> Linhas(string saida)
        {
            return (saida ?? "").Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }

        // `adb devices` -> seriais no estado "device" (ignora offline/unauthorized)
        public static List<string> SeriaisProntos(string saida)
        {
            return Linhas(saida)
                .Select(l => Regex.Match(l, @"^(\S+)\s+device$"))
                .Where(m => m.Success).Select(m => m.Groups[1].Value).ToList();
        }

        // `adb mdns services` -> servicos de pareamento e conexao da Depuracao por Wi-Fi
        public static List<ServicoMdns> ServicosMdns(string saida)
        {
            return Linhas(saida)
                .Select(l => Regex.Match(l, @"^(\S+)\s+_adb-tls-(pairing|connect)\._tcp\s+(\S+:\d+)"))
                .Where(m => m.Success)
                .Select(m => new ServicoMdns {
                    Nome = m.Groups[1].Value,
                    Pareamento = m.Groups[2].Value == "pairing",
                    Endereco = m.Groups[3].Value
                }).ToList();
        }

        public static bool CodigoValido(string codigo)
        {
            return codigo != null && Regex.IsMatch(codigo.Trim(), @"^\d{6}$");
        }

        public static bool PareamentoOk(string saida)
        {
            return (saida ?? "").Contains("Successfully paired");
        }

        // `dumpsys window` -> tela de bloqueio aparecendo (o Android nao deixa captura-la: fica preta no PC)
        public static bool Bloqueado(string dumpsys)
        {
            return (dumpsys ?? "").Contains("isKeyguardShowing=true");
        }

        // PC sem saida de audio padrao: o scrcpy fecha com este erro
        public static bool ErroDeAudio(string log)
        {
            return (log ?? "").Contains("Could not open audio device");
        }

        public static string PrimeiroErro(string log)
        {
            return Linhas(log).FirstOrDefault(l => l.StartsWith("ERROR"));
        }
    }
}
