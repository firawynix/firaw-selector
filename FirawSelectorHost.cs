using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;

// ---------------------------------------------------------------------------
// Ponte entre as extensoes e o aplicativo. O navegador fala Native Messaging:
// cada JSON vem precedido pelo tamanho em 32 bits. O host valida o endereco,
// inicia o motor em modo de pergunta e responde antes de encerrar.
// ---------------------------------------------------------------------------
static class ProgramaHost
{
    const int MaximoEntrada = 1024 * 1024;

    static int Main(string[] args)
    {
        try
        {
            string recebido = LeMensagem(Console.OpenStandardInput());
            JavaScriptSerializer json = new JavaScriptSerializer();
            Dictionary<string, object> msg =
                json.DeserializeObject(recebido) as Dictionary<string, object>;
            if (msg == null) throw new Exception("Mensagem invalida.");

            string acao = Valor(msg, "action");
            if (string.Equals(acao, "ping", StringComparison.OrdinalIgnoreCase))
            {
                Responde(json, true, "", false);
                return 0;
            }

            if (!string.IsNullOrEmpty(acao) &&
                !string.Equals(acao, "open", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Acao desconhecida.");

            string url = Valor(msg, "url");
            Uri destino;
            if (!Uri.TryCreate(url, UriKind.Absolute, out destino) ||
                (destino.Scheme != Uri.UriSchemeHttp && destino.Scheme != Uri.UriSchemeHttps))
                throw new Exception("O endereco precisa usar http ou https.");

            string motor = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Amb.Produto + ".exe");
            if (!File.Exists(motor)) throw new Exception("FirawSelector.exe nao foi encontrado.");

            ProcessStartInfo psi = new ProcessStartInfo(motor, "--ask " + Argumento(url));
            psi.UseShellExecute = false;
            psi.WorkingDirectory = Path.GetDirectoryName(motor);
            Process.Start(psi);

            Responde(json, true, "", true);
            return 0;
        }
        catch (Exception ex)
        {
            try { Responde(new JavaScriptSerializer(), false, ex.Message, false); }
            catch { }
            return 1;
        }
    }

    static string Valor(Dictionary<string, object> msg, string chave)
    {
        object valor;
        return msg.TryGetValue(chave, out valor) && valor != null ? valor.ToString() : "";
    }

    static string LeMensagem(Stream entrada)
    {
        byte[] cabecalho = LeExato(entrada, 4);
        int tamanho = BitConverter.ToInt32(cabecalho, 0);
        if (tamanho <= 0 || tamanho > MaximoEntrada)
            throw new Exception("Tamanho de mensagem invalido.");
        return Encoding.UTF8.GetString(LeExato(entrada, tamanho));
    }

    static byte[] LeExato(Stream entrada, int tamanho)
    {
        byte[] dados = new byte[tamanho];
        int pos = 0;
        while (pos < tamanho)
        {
            int n = entrada.Read(dados, pos, tamanho - pos);
            if (n <= 0) throw new EndOfStreamException("Mensagem incompleta.");
            pos += n;
        }
        return dados;
    }

    static void Responde(JavaScriptSerializer json, bool ok, string erro, bool iniciado)
    {
        Dictionary<string, object> resposta = new Dictionary<string, object>();
        resposta["ok"] = ok;
        resposta["version"] = Amb.Versao;
        if (iniciado) resposta["launched"] = true;
        if (!string.IsNullOrEmpty(erro)) resposta["error"] = erro;

        byte[] corpo = Encoding.UTF8.GetBytes(json.Serialize(resposta));
        byte[] tamanho = BitConverter.GetBytes(corpo.Length);
        Stream saida = Console.OpenStandardOutput();
        saida.Write(tamanho, 0, tamanho.Length);
        saida.Write(corpo, 0, corpo.Length);
        saida.Flush();
    }

    // Escapamento da linha de comando do Windows. As barras anteriores a uma
    // aspa precisam ser duplicadas, e as barras finais tambem, antes do fecho.
    static string Argumento(string valor)
    {
        StringBuilder s = new StringBuilder();
        s.Append('"');
        int barras = 0;
        foreach (char c in valor)
        {
            if (c == '\\')
            {
                barras++;
                continue;
            }
            if (c == '"')
            {
                s.Append('\\', barras * 2 + 1);
                s.Append('"');
                barras = 0;
                continue;
            }
            if (barras > 0) s.Append('\\', barras);
            barras = 0;
            s.Append(c);
        }
        if (barras > 0) s.Append('\\', barras * 2);
        s.Append('"');
        return s.ToString();
    }
}
