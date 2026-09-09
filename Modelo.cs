using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

// ---------------------------------------------------------------------------
// Um navegador que o FirawSelector sabe abrir. Perfil e navegador tambem: o
// "Chrome (Trabalho)" e uma entrada propria, com os argumentos dele.
// ---------------------------------------------------------------------------
class Navegador
{
    public string Id = "";           // chave estavel: chrome, chrome@Profile 1, firefox@dev
    public string Nome = "";
    public string Exe = "";
    public string Args = "";         // argumentos fixos (perfil, por exemplo)
    public string ArgsPrivado = "";  // o que abre janela privativa nesta familia
    public string Familia = "outro"; // chromium | firefox | ie | outro
    public bool Manual;              // adicionado a mao pelo usuario
    public bool Oculto;

    /// <summary>
    /// Icone escolhido a mao: "algo.ico", ou "algo.exe,3" para pegar o de indice
    /// 3 de dentro do arquivo. Vazio = o icone do proprio programa. Serve
    /// principalmente para separar perfis, que compartilham o mesmo .exe e por
    /// isso nasceriam todos com a mesma carinha.
    /// </summary>
    public string IconeArquivo = "";

    // Um cache por tamanho: a mesma entrada e desenhada a 32 na janela de
    // escolha e a 20 na lista do Studio, e um cache so devolveria a errada.
    readonly Dictionary<int, Icon> cacheIcone = new Dictionary<int, Icon>();

    public bool Existe { get { return !string.IsNullOrEmpty(Exe) && File.Exists(Exe); } }

    public static void SeparaIcone(string valor, string caindoEm, out string arquivo, out int indice)
    {
        arquivo = caindoEm;
        indice = 0;
        if (string.IsNullOrEmpty(valor)) return;

        arquivo = valor;
        int v = arquivo.LastIndexOf(',');
        // > 1 e nao > 0 de proposito: "C:,0" nao existe, mas o dois-pontos da
        // unidade nao pode ser confundido com separador.
        if (v > 1)
        {
            int n;
            if (int.TryParse(arquivo.Substring(v + 1).Trim(), out n))
            {
                indice = n;
                arquivo = arquivo.Substring(0, v);
            }
        }
    }

    public Icon Icone(int tamanho)
    {
        Icon achado;
        if (cacheIcone.TryGetValue(tamanho, out achado)) return achado;

        string arq;
        int idx;
        SeparaIcone(IconeArquivo, Exe, out arq, out idx);

        achado = Amb.IconeDoArquivo(arq, idx, tamanho);
        // Icone escolhido que sumiu (pendrive fora, arquivo apagado) nao pode
        // deixar a linha sem imagem: cai no icone do programa.
        if (achado == null && !string.Equals(arq, Exe, StringComparison.OrdinalIgnoreCase))
            achado = Amb.IconeDoArquivo(Exe, 0, tamanho);

        cacheIcone[tamanho] = achado;
        return achado;
    }

    public void EsqueceIcone()
    {
        cacheIcone.Clear();
    }

    public void Abrir(string url, bool privado)
    {
        string args = Args == null ? "" : Args.Trim();
        if (privado && !string.IsNullOrEmpty(ArgsPrivado))
            args = (args + " " + ArgsPrivado).Trim();

        // O endereco vai por ultimo e entre aspas: sem elas, um "&" no meio da
        // URL faz o proprio shell cortar o comando em dois.
        string linha = (args.Length > 0 ? args + " " : "") + "\"" + url + "\"";
        System.Diagnostics.ProcessStartInfo psi =
            new System.Diagnostics.ProcessStartInfo(Exe, linha);
        psi.UseShellExecute = true;
        psi.WorkingDirectory = Path.GetDirectoryName(Exe);
        System.Diagnostics.Process.Start(psi);
    }

    public override string ToString() { return Nome; }

    // ---------- ida e volta para o INI ----------

    public string Serializa()
    {
        return string.Join("|", new string[] {
            Ini.Escapa(Id), Ini.Escapa(Nome), Ini.Escapa(Exe), Ini.Escapa(Args),
            Ini.Escapa(ArgsPrivado), Ini.Escapa(Familia),
            Manual ? "1" : "0", Oculto ? "1" : "0", Ini.Escapa(IconeArquivo)
        });
    }

    public static Navegador Le(string linha)
    {
        string[] p = linha.Split('|');
        if (p.Length < 6) return null;
        Navegador n = new Navegador();
        n.Id = Ini.Desescapa(p[0]);
        n.Nome = Ini.Desescapa(p[1]);
        n.Exe = Ini.Desescapa(p[2]);
        n.Args = Ini.Desescapa(p[3]);
        n.ArgsPrivado = Ini.Desescapa(p[4]);
        n.Familia = Ini.Desescapa(p[5]);
        n.Manual = p.Length > 6 && p[6] == "1";
        n.Oculto = p.Length > 7 && p[7] == "1";
        // Campo novo: config gravado por versao anterior nao tem, e nao ter e
        // exatamente o padrao (icone do proprio programa).
        n.IconeArquivo = p.Length > 8 ? Ini.Desescapa(p[8]) : "";
        return n;
    }

    // ---------- descoberta ----------

    public static string FamiliaDoExe(string exe)
    {
        string f = "";
        try { f = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant(); }
        catch { }

        if (f == "firefox" || f == "waterfox" || f == "librewolf" || f == "palemoon")
            return "firefox";
        if (f == "iexplore") return "ie";
        if (f == "chrome" || f == "msedge" || f == "brave" || f == "vivaldi" ||
            f == "opera" || f == "chromium" || f == "thorium" || f == "yandex")
            return "chromium";
        return "outro";
    }

    public static string PrivadoPadrao(string exe)
    {
        string f = "";
        try { f = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant(); }
        catch { }

        if (f == "msedge") return "-inprivate";
        if (f == "opera") return "--private";
        if (f == "iexplore") return "-private";
        if (FamiliaDoExe(exe) == "firefox") return "-private-window";
        if (FamiliaDoExe(exe) == "chromium") return "--incognito";
        return "";
    }

    /// <summary>Tira o .exe de uma linha de comando do registro.</summary>
    public static string ExeDoComando(string cmd)
    {
        if (string.IsNullOrEmpty(cmd)) return "";
        cmd = cmd.Trim();
        if (cmd.StartsWith("\""))
        {
            int fim = cmd.IndexOf('"', 1);
            if (fim > 1) return cmd.Substring(1, fim - 1);
            return cmd.Trim('"');
        }
        int p = cmd.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (p > 0) return cmd.Substring(0, p + 4);
        return cmd;
    }

    static void LeClientes(RegistryKey raiz, string caminho, List<Navegador> saida)
    {
        RegistryKey k = null;
        try { k = raiz.OpenSubKey(caminho); }
        catch { }
        if (k == null) return;

        foreach (string nomeChave in k.GetSubKeyNames())
        {
            if (string.Equals(nomeChave, Amb.Produto, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                RegistryKey sub = k.OpenSubKey(nomeChave);
                if (sub == null) continue;

                RegistryKey cmd = sub.OpenSubKey(@"shell\open\command");
                if (cmd == null) continue;

                string exe = ExeDoComando((string)cmd.GetValue(""));
                if (string.IsNullOrEmpty(exe) || !File.Exists(exe)) continue;

                string nome = (string)sub.GetValue("");
                if (string.IsNullOrEmpty(nome)) nome = nomeChave;

                Acrescenta(saida, Cria(nome, exe, "", ""));
            }
            catch { }
        }
    }

    static Navegador Cria(string nome, string exe, string args, string sufixoId)
    {
        Navegador n = new Navegador();
        n.Nome = nome;
        n.Exe = exe;
        n.Args = args;
        n.Familia = FamiliaDoExe(exe);
        n.ArgsPrivado = PrivadoPadrao(exe);
        string bruto = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
        n.Id = string.IsNullOrEmpty(sufixoId) ? bruto : bruto + "@" + sufixoId;
        return n;
    }

    static void Acrescenta(List<Navegador> lista, Navegador n)
    {
        if (n == null || !n.Existe) return;
        foreach (Navegador j in lista)
        {
            if (string.Equals(j.Exe, n.Exe, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(j.Args, n.Args, StringComparison.OrdinalIgnoreCase)) return;
        }
        lista.Add(n);
    }

    static void TentaCaminho(List<Navegador> saida, string nome, string relativo)
    {
        string[] bases = new string[] {
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        };
        foreach (string b in bases)
        {
            if (string.IsNullOrEmpty(b)) continue;
            string exe = Path.Combine(b, relativo);
            if (File.Exists(exe)) { Acrescenta(saida, Cria(nome, exe, "", "")); return; }
        }
    }

    /// <summary>Varre o registro e os lugares de sempre.</summary>
    public static List<Navegador> Detecta()
    {
        List<Navegador> saida = new List<Navegador>();

        LeClientes(Registry.CurrentUser, @"SOFTWARE\Clients\StartMenuInternet", saida);
        LeClientes(Registry.LocalMachine, @"SOFTWARE\Clients\StartMenuInternet", saida);
        // Navegador de 32 bits numa maquina de 64 mora no galho WOW6432Node, e
        // um processo de 64 bits nao o enxerga sem apontar o caminho na mao.
        LeClientes(Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Clients\StartMenuInternet", saida);

        TentaCaminho(saida, "Google Chrome", @"Google\Chrome\Application\chrome.exe");
        TentaCaminho(saida, "Microsoft Edge", @"Microsoft\Edge\Application\msedge.exe");
        TentaCaminho(saida, "Mozilla Firefox", @"Mozilla Firefox\firefox.exe");
        TentaCaminho(saida, "Brave", @"BraveSoftware\Brave-Browser\Application\brave.exe");
        TentaCaminho(saida, "Vivaldi", @"Vivaldi\Application\vivaldi.exe");
        TentaCaminho(saida, "Opera", @"Programs\Opera\opera.exe");
        TentaCaminho(saida, "Internet Explorer", @"Internet Explorer\iexplore.exe");

        return saida;
    }

    // ---------- perfis ----------

    static string PastaDados(string exe)
    {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string f = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant();
        if (f == "chrome") return Path.Combine(local, @"Google\Chrome\User Data");
        if (f == "msedge") return Path.Combine(local, @"Microsoft\Edge\User Data");
        if (f == "brave") return Path.Combine(local, @"BraveSoftware\Brave-Browser\User Data");
        if (f == "vivaldi") return Path.Combine(local, @"Vivaldi\User Data");
        if (f == "chromium") return Path.Combine(local, @"Chromium\User Data");
        return null;
    }

    static string NomeDoPerfil(string pastaPerfil, string cair)
    {
        try
        {
            string prefs = Path.Combine(pastaPerfil, "Preferences");
            if (!File.Exists(prefs)) return cair;
            string txt = File.ReadAllText(prefs, Encoding.UTF8);
            // O apelido do perfil mora dentro do bloco "profile". Procurar
            // "name" solto no arquivo inteiro pega o nome de qualquer extensao.
            int p = txt.IndexOf("\"profile\"");
            if (p < 0) return cair;
            int fim = Math.Min(txt.Length, p + 8000);
            Match m = Regex.Match(txt.Substring(p, fim - p), "\"name\"\\s*:\\s*\"([^\"]*)\"");
            if (m.Success && m.Groups[1].Value.Trim().Length > 0)
                return m.Groups[1].Value.Replace("\\u0027", "'");
        }
        catch { }
        return cair;
    }

    /// <summary>Perfis de Chrome/Edge/Brave/Vivaldi e do Firefox, como entradas proprias.</summary>
    public static List<Navegador> DetectaPerfis(List<Navegador> baseNavegadores)
    {
        List<Navegador> saida = new List<Navegador>();

        foreach (Navegador n in baseNavegadores)
        {
            if (n.Familia == "chromium" && string.IsNullOrEmpty(n.Args))
            {
                string dados = PastaDados(n.Exe);
                if (dados == null || !Directory.Exists(dados)) continue;

                List<string> pastas = new List<string>();
                if (Directory.Exists(Path.Combine(dados, "Default"))) pastas.Add("Default");
                try
                {
                    foreach (string d in Directory.GetDirectories(dados, "Profile *"))
                        pastas.Add(Path.GetFileName(d));
                }
                catch { }

                // Um perfil so nao vira entrada extra: seria o mesmo navegador duas vezes.
                if (pastas.Count < 2) continue;

                foreach (string pasta in pastas)
                {
                    string apelido = NomeDoPerfil(Path.Combine(dados, pasta), pasta);
                    Navegador p = Cria(n.Nome + " - " + apelido, n.Exe,
                        "--profile-directory=\"" + pasta + "\"", pasta);
                    saida.Add(p);
                }
            }
            else if (n.Familia == "firefox" && string.IsNullOrEmpty(n.Args))
            {
                string ini = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Mozilla\Firefox\profiles.ini");
                if (!File.Exists(ini)) continue;

                List<string> nomes = new List<string>();
                try
                {
                    foreach (string linha in File.ReadAllLines(ini))
                    {
                        string t = linha.Trim();
                        if (t.StartsWith("Name=", StringComparison.OrdinalIgnoreCase))
                            nomes.Add(t.Substring(5).Trim());
                    }
                }
                catch { }

                if (nomes.Count < 2) continue;
                foreach (string apelido in nomes)
                {
                    Navegador p = Cria(n.Nome + " - " + apelido, n.Exe,
                        "-P \"" + apelido + "\"", apelido);
                    saida.Add(p);
                }
            }
        }
        return saida;
    }
}

// ---------------------------------------------------------------------------
// Regra: um padrao, um navegador, uma ordem. A primeira que casar vence.
// ---------------------------------------------------------------------------
class Regra
{
    public string Padrao = "";
    public string NavegadorId = "";
    public string Tipo = "host";   // host | url | regex
    public bool Privado;
    public bool Ativo = true;

    public bool Casa(string url, string host)
    {
        if (!Ativo || string.IsNullOrEmpty(Padrao)) return false;
        try
        {
            if (Tipo == "regex")
                return Regex.IsMatch(url, Padrao, RegexOptions.IgnoreCase);

            string alvo = Tipo == "host" ? host : url;
            if (string.IsNullOrEmpty(alvo)) return false;

            // "*.exemplo.com" tambem casa com "exemplo.com": quem escreve a regra
            // quer o dominio inteiro, e a raiz e parte do dominio.
            if (Tipo == "host" && Padrao.StartsWith("*."))
            {
                string raiz = Padrao.Substring(2);
                if (string.Equals(alvo, raiz, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return Regex.IsMatch(alvo, Coringa(Padrao), RegexOptions.IgnoreCase);
        }
        catch { return false; }
    }

    public static string Coringa(string padrao)
    {
        return "^" + Regex.Escape(padrao).Replace("\\*", ".*").Replace("\\?", ".") + "$";
    }

    public static bool RegexValida(string padrao, out string erro)
    {
        erro = null;
        try { Regex.IsMatch("teste", padrao); return true; }
        catch (Exception ex) { erro = ex.Message; return false; }
    }

    public string Serializa()
    {
        return string.Join("|", new string[] {
            Ini.Escapa(Padrao), Ini.Escapa(NavegadorId), Tipo,
            Privado ? "1" : "0", Ativo ? "1" : "0"
        });
    }

    public static Regra Le(string linha)
    {
        string[] p = linha.Split('|');
        if (p.Length < 3) return null;
        Regra r = new Regra();
        r.Padrao = Ini.Desescapa(p[0]);
        r.NavegadorId = Ini.Desescapa(p[1]);
        r.Tipo = p[2];
        r.Privado = p.Length > 3 && p[3] == "1";
        r.Ativo = p.Length < 5 || p[4] == "1";
        return r;
    }
}

// ---------------------------------------------------------------------------
// Leitor/gravador de INI. Nao usa a API do Windows: ela corta valores longos e
// nao sabe escrever UTF-8, e a lista de regras passa dos dois casos.
// ---------------------------------------------------------------------------
static class Ini
{
    public static string Escapa(string s)
    {
        if (s == null) return "";
        return s.Replace("%", "%25").Replace("|", "%7C").Replace("\r", "").Replace("\n", " ");
    }

    public static string Desescapa(string s)
    {
        if (s == null) return "";
        return s.Replace("%7C", "|").Replace("%25", "%");
    }

    public static Dictionary<string, List<string>> Ler(string arquivo)
    {
        Dictionary<string, List<string>> secoes = new Dictionary<string, List<string>>();
        if (!File.Exists(arquivo)) return secoes;

        string atual = "geral";
        secoes[atual] = new List<string>();
        foreach (string bruta in File.ReadAllLines(arquivo, Encoding.UTF8))
        {
            string linha = bruta.Trim();
            if (linha.Length == 0 || linha.StartsWith(";") || linha.StartsWith("#")) continue;
            if (linha.StartsWith("[") && linha.EndsWith("]"))
            {
                atual = linha.Substring(1, linha.Length - 2).Trim().ToLowerInvariant();
                if (!secoes.ContainsKey(atual)) secoes[atual] = new List<string>();
                continue;
            }
            if (!secoes.ContainsKey(atual)) secoes[atual] = new List<string>();
            secoes[atual].Add(linha);
        }
        return secoes;
    }

    public static string Valor(List<string> secao, string chave, string cair)
    {
        if (secao == null) return cair;
        foreach (string linha in secao)
        {
            int p = linha.IndexOf('=');
            if (p <= 0) continue;
            if (string.Equals(linha.Substring(0, p).Trim(), chave, StringComparison.OrdinalIgnoreCase))
                return linha.Substring(p + 1).Trim();
        }
        return cair;
    }

    public static bool Bool(List<string> secao, string chave, bool cair)
    {
        string v = Valor(secao, chave, cair ? "1" : "0");
        return v == "1" || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
    }

    public static int Inteiro(List<string> secao, string chave, int cair)
    {
        int n;
        if (int.TryParse(Valor(secao, chave, ""), out n)) return n;
        return cair;
    }
}

// ---------------------------------------------------------------------------
// A configuracao inteira, em um arquivo so.
// ---------------------------------------------------------------------------
class Cfg
{
    public string Lingua = "auto";
    public string Tema = "sistema";
    public string Modo = "perguntar";   // perguntar | regras
    public string PadraoId = "";
    public string SegundoId = "";
    public int Tempo = 0;
    public bool Lembrar = true;
    public bool ForcarShift = true;
    public bool LimparRastreio = false;
    public bool ExpandirCurtas = false;
    public bool Log = false;

    public List<Navegador> Navegadores = new List<Navegador>();
    public List<Regra> Regras = new List<Regra>();
    public Dictionary<string, string> Lembrados = new Dictionary<string, string>();

    public Navegador Por(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (Navegador n in Navegadores)
            if (n.Id == id) return n;
        return null;
    }

    public Navegador Padrao
    {
        get
        {
            Navegador n = Por(PadraoId);
            if (n != null && n.Existe && !n.Oculto) return n;
            foreach (Navegador j in Navegadores)
                if (j.Existe && !j.Oculto) return j;
            return null;
        }
    }

    public List<Navegador> Visiveis()
    {
        List<Navegador> v = new List<Navegador>();
        foreach (Navegador n in Navegadores)
            if (n.Existe && !n.Oculto) v.Add(n);
        return v;
    }

    /// <summary>Primeira carga: acha os navegadores e escolhe um padrao sozinho.</summary>
    public void Semear()
    {
        Navegadores = Navegador.Detecta();
        if (string.IsNullOrEmpty(PadraoId))
        {
            // Preferencia de partida: o que a pessoa provavelmente ja usava.
            string[] ordem = new string[] { "firefox", "chrome", "msedge", "brave", "vivaldi", "opera" };
            foreach (string alvo in ordem)
            {
                foreach (Navegador n in Navegadores)
                {
                    if (n.Id == alvo) { PadraoId = n.Id; break; }
                }
                if (!string.IsNullOrEmpty(PadraoId)) break;
            }
            if (string.IsNullOrEmpty(PadraoId) && Navegadores.Count > 0)
                PadraoId = Navegadores[0].Id;
        }
        if (string.IsNullOrEmpty(SegundoId))
        {
            foreach (Navegador n in Navegadores)
                if (n.Id != PadraoId) { SegundoId = n.Id; break; }
        }
    }

    public static Cfg Carregar()
    {
        Cfg c = new Cfg();
        string arq = Amb.CaminhoIni();
        Dictionary<string, List<string>> s = Ini.Ler(arq);

        bool novo = !File.Exists(arq);
        List<string> g = s.ContainsKey("geral") ? s["geral"] : null;

        c.Lingua = Ini.Valor(g, "idioma", "auto");
        c.Tema = Ini.Valor(g, "tema", "sistema");
        c.Modo = Ini.Valor(g, "modo", "perguntar");
        c.PadraoId = Ini.Valor(g, "padrao", "");
        c.SegundoId = Ini.Valor(g, "segundo", "");
        c.Tempo = Ini.Inteiro(g, "tempo", 0);
        c.Lembrar = Ini.Bool(g, "lembrar", true);
        c.ForcarShift = Ini.Bool(g, "forcarShift", true);
        c.LimparRastreio = Ini.Bool(g, "limparRastreio", false);
        c.ExpandirCurtas = Ini.Bool(g, "expandirCurtas", false);
        c.Log = Ini.Bool(g, "log", false);

        if (s.ContainsKey("navegadores"))
        {
            foreach (string linha in s["navegadores"])
            {
                int p = linha.IndexOf('=');
                if (p <= 0) continue;
                Navegador n = Navegador.Le(linha.Substring(p + 1).Trim());
                if (n != null) c.Navegadores.Add(n);
            }
        }

        if (s.ContainsKey("regras"))
        {
            foreach (string linha in s["regras"])
            {
                int p = linha.IndexOf('=');
                if (p <= 0) continue;
                Regra r = Regra.Le(linha.Substring(p + 1).Trim());
                if (r != null) c.Regras.Add(r);
            }
        }

        if (s.ContainsKey("lembrados"))
        {
            foreach (string linha in s["lembrados"])
            {
                int p = linha.IndexOf('=');
                if (p <= 0) continue;
                string host = linha.Substring(0, p).Trim().ToLowerInvariant();
                c.Lembrados[host] = linha.Substring(p + 1).Trim();
            }
        }

        if (novo || c.Navegadores.Count == 0)
        {
            c.Semear();
            if (novo) c.Salvar();
        }

        Idioma.Definir(c.Lingua);
        Cores.Aplicar(c.Tema);
        return c;
    }

    public void Salvar()
    {
        string arq = Amb.CaminhoIni();
        try { Directory.CreateDirectory(Path.GetDirectoryName(arq)); }
        catch { }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("; " + Amb.Produto + " " + Amb.Versao + " - " + Amb.Site);
        sb.AppendLine("; Editavel a mao. O Studio reescreve este arquivo ao salvar.");
        sb.AppendLine();
        sb.AppendLine("[geral]");
        sb.AppendLine("idioma=" + Lingua);
        sb.AppendLine("tema=" + Tema);
        sb.AppendLine("modo=" + Modo);
        sb.AppendLine("padrao=" + PadraoId);
        sb.AppendLine("segundo=" + SegundoId);
        sb.AppendLine("tempo=" + Tempo);
        sb.AppendLine("lembrar=" + (Lembrar ? "1" : "0"));
        sb.AppendLine("forcarShift=" + (ForcarShift ? "1" : "0"));
        sb.AppendLine("limparRastreio=" + (LimparRastreio ? "1" : "0"));
        sb.AppendLine("expandirCurtas=" + (ExpandirCurtas ? "1" : "0"));
        sb.AppendLine("log=" + (Log ? "1" : "0"));

        sb.AppendLine();
        sb.AppendLine("[navegadores]");
        for (int i = 0; i < Navegadores.Count; i++)
            sb.AppendLine((i + 1).ToString("0000") + "=" + Navegadores[i].Serializa());

        sb.AppendLine();
        sb.AppendLine("[regras]");
        for (int i = 0; i < Regras.Count; i++)
            sb.AppendLine((i + 1).ToString("0000") + "=" + Regras[i].Serializa());

        sb.AppendLine();
        sb.AppendLine("[lembrados]");
        foreach (KeyValuePair<string, string> kv in Lembrados)
            sb.AppendLine(kv.Key + "=" + kv.Value);

        File.WriteAllText(arq, sb.ToString(), new UTF8Encoding(false));
    }
}

// ---------------------------------------------------------------------------
// Onde a decisao acontece.
// ---------------------------------------------------------------------------
static class Motor
{
    static readonly string[] Rastreio = new string[] {
        "utm_source", "utm_medium", "utm_campaign", "utm_term", "utm_content", "utm_id",
        "fbclid", "gclid", "gbraid", "wbraid", "dclid", "msclkid", "mc_eid", "mc_cid",
        "igshid", "yclid", "twclid", "ttclid", "vero_id", "oly_enc_id", "_hsenc", "_hsmi",
        "ref_src", "ref_url", "spm", "scm"
    };

    /// <summary>
    /// O que chega na linha de comando nem sempre e uma URL: a Pesquisa do
    /// Windows manda "microsoft-edge:?...&url=<endereco codificado>".
    /// </summary>
    public static string Normaliza(string bruto)
    {
        if (string.IsNullOrEmpty(bruto)) return "";
        string s = bruto.Trim().Trim('"');

        if (s.StartsWith("microsoft-edge:", StringComparison.OrdinalIgnoreCase))
        {
            string resto = s.Substring("microsoft-edge:".Length);
            Match m = Regex.Match(resto, "[?&]url=([^&]+)", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                try { return Uri.UnescapeDataString(m.Groups[1].Value); }
                catch { return m.Groups[1].Value; }
            }
            resto = resto.TrimStart('?', '/');
            if (resto.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return resto;
            return s;
        }

        // Arquivo local arrastado para cima do programa.
        if (File.Exists(s))
        {
            try { return new Uri(s).AbsoluteUri; }
            catch { return s; }
        }

        if (!Regex.IsMatch(s, "^[a-zA-Z][a-zA-Z0-9+.-]*:"))
            s = "http://" + s;
        return s;
    }

    public static string Host(string url)
    {
        try { return new Uri(url).Host.ToLowerInvariant(); }
        catch
        {
            Match m = Regex.Match(url, "^[a-zA-Z][a-zA-Z0-9+.-]*://([^/:?#]+)");
            return m.Success ? m.Groups[1].Value.ToLowerInvariant() : "";
        }
    }

    public static string LimpaRastreio(string url)
    {
        try
        {
            int p = url.IndexOf('?');
            if (p < 0) return url;

            string fim = "";
            string consulta = url.Substring(p + 1);
            int h = consulta.IndexOf('#');
            if (h >= 0) { fim = consulta.Substring(h); consulta = consulta.Substring(0, h); }

            List<string> fica = new List<string>();
            foreach (string par in consulta.Split('&'))
            {
                if (par.Length == 0) continue;
                int e = par.IndexOf('=');
                string chave = (e > 0 ? par.Substring(0, e) : par).ToLowerInvariant();
                bool sujo = false;
                foreach (string r in Rastreio)
                    if (chave == r) { sujo = true; break; }
                if (!sujo && chave.StartsWith("utm_")) sujo = true;
                if (!sujo) fica.Add(par);
            }
            string baseUrl = url.Substring(0, p);
            if (fica.Count == 0) return baseUrl + fim;
            return baseUrl + "?" + string.Join("&", fica.ToArray()) + fim;
        }
        catch { return url; }
    }

    /// <summary>
    /// Segue os redirecionamentos do encurtador para decidir pelo destino real.
    /// Rede pode nao responder, entao o prazo e curto e a falha devolve o original.
    /// </summary>
    public static string Expande(string url)
    {
        try
        {
            string atual = url;
            for (int salto = 0; salto < 5; salto++)
            {
                System.Net.HttpWebRequest req =
                    (System.Net.HttpWebRequest)System.Net.WebRequest.Create(atual);
                req.Method = "HEAD";
                req.AllowAutoRedirect = false;
                req.Timeout = 2500;
                req.UserAgent = Amb.Produto + "/" + Amb.Versao;

                using (System.Net.HttpWebResponse res = (System.Net.HttpWebResponse)req.GetResponse())
                {
                    int cod = (int)res.StatusCode;
                    if (cod < 300 || cod > 399) return atual;
                    string destino = res.Headers["Location"];
                    if (string.IsNullOrEmpty(destino)) return atual;
                    atual = new Uri(new Uri(atual), destino).AbsoluteUri;
                }
            }
            return atual;
        }
        catch { return url; }
    }

    /// <summary>
    /// Decide o navegador. Devolve null quando a janela de escolha tem de abrir.
    /// </summary>
    public static Navegador Escolhe(Cfg c, string url, bool shift,
        out bool privado, out string motivo)
    {
        privado = false;
        motivo = "";
        string host = Host(url);

        if (shift && c.ForcarShift)
        {
            motivo = Idioma.T("por.shift");
            return null;
        }

        foreach (Regra r in c.Regras)
        {
            if (!r.Casa(url, host)) continue;
            Navegador n = c.Por(r.NavegadorId);
            if (n == null || !n.Existe) continue;
            privado = r.Privado;
            motivo = Idioma.T("por.regra", r.Padrao);
            return n;
        }

        if (c.Lembrar && !string.IsNullOrEmpty(host) && c.Lembrados.ContainsKey(host))
        {
            Navegador n = c.Por(c.Lembrados[host]);
            if (n != null && n.Existe)
            {
                motivo = Idioma.T("por.lembrado");
                return n;
            }
        }

        if (c.Modo == "regras")
        {
            Navegador n = c.Padrao;
            if (n != null)
            {
                motivo = Idioma.T("por.padrao");
                return n;
            }
        }

        return null;
    }
}

// ---------------------------------------------------------------------------
// O registro do Windows: e isso que faz o FirawSelector aparecer na lista de
// navegadores padrao.
// ---------------------------------------------------------------------------
static class Registrar
{
    public static bool EstaRegistrado()
    {
        try
        {
            RegistryKey k = Registry.CurrentUser.OpenSubKey(Amb.ChaveCliente);
            return k != null;
        }
        catch { return false; }
    }

    public static bool EhPadrao()
    {
        try
        {
            RegistryKey k = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice");
            if (k == null) return false;
            string prog = (string)k.GetValue("ProgId", "");
            return string.Equals(prog, Amb.ProgIdUrl, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    static void ProgId(string id, string descricao, bool protocolo, string exe)
    {
        RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + id);
        k.SetValue("", descricao);
        if (protocolo) k.SetValue("URL Protocol", "");
        k.CreateSubKey("DefaultIcon").SetValue("", "\"" + exe + "\",0");
        k.CreateSubKey(@"shell\open\command").SetValue("", "\"" + exe + "\" \"%1\"");
        k.Close();
    }

    public static void Registra(string exe)
    {
        ProgId(Amb.ProgIdUrl, Amb.Produto + " URL", true, exe);
        ProgId(Amb.ProgIdHtml, Amb.Produto + " HTML", false, exe);

        RegistryKey c = Registry.CurrentUser.CreateSubKey(Amb.ChaveCliente);
        c.SetValue("", Amb.Produto);
        c.CreateSubKey("DefaultIcon").SetValue("", "\"" + exe + "\",0");
        c.CreateSubKey(@"shell\open\command").SetValue("", "\"" + exe + "\"");

        RegistryKey cap = c.CreateSubKey("Capabilities");
        cap.SetValue("ApplicationName", Amb.Produto);
        cap.SetValue("ApplicationDescription",
            "Escolhe o navegador de cada link / Picks the browser for each link");
        cap.SetValue("ApplicationIcon", "\"" + exe + "\",0");

        RegistryKey url = cap.CreateSubKey("URLAssociations");
        url.SetValue("http", Amb.ProgIdUrl);
        url.SetValue("https", Amb.ProgIdUrl);
        url.SetValue("ftp", Amb.ProgIdUrl);

        RegistryKey arq = cap.CreateSubKey("FileAssociations");
        foreach (string ext in new string[] { ".htm", ".html", ".shtml", ".xht", ".xhtml" })
            arq.SetValue(ext, Amb.ProgIdHtml);

        cap.CreateSubKey("StartMenu").SetValue("StartMenuInternet", Amb.Produto);
        c.Close();

        RegistryKey reg = Registry.CurrentUser.CreateSubKey(@"Software\RegisteredApplications");
        reg.SetValue(Amb.Produto, Amb.ChaveCliente + @"\Capabilities");
        reg.Close();
    }

    public static void Remove()
    {
        try { Registry.CurrentUser.DeleteSubKeyTree(Amb.ChaveCliente, false); } catch { }
        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + Amb.ProgIdUrl, false); } catch { }
        try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\" + Amb.ProgIdHtml, false); } catch { }
        try
        {
            RegistryKey reg = Registry.CurrentUser.OpenSubKey(@"Software\RegisteredApplications", true);
            if (reg != null) reg.DeleteValue(Amb.Produto, false);
        }
        catch { }
        DefineCapturaEdge(false, null);
    }

    // O Windows manda os links da Pesquisa e dos widgets por microsoft-edge:,
    // que ignora o navegador padrao. Assumir o protocolo e o unico jeito.
    const string ChaveEdge = @"Software\Classes\microsoft-edge\shell\open\command";

    public static bool CapturaEdge()
    {
        try
        {
            RegistryKey k = Registry.CurrentUser.OpenSubKey(ChaveEdge);
            if (k == null) return false;
            string v = (string)k.GetValue("", "");
            return v.IndexOf(Amb.Produto, StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch { return false; }
    }

    public static void DefineCapturaEdge(bool ligado, string exe)
    {
        if (ligado)
        {
            RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Software\Classes\microsoft-edge");
            k.SetValue("", "URL:microsoft-edge");
            k.SetValue("URL Protocol", "");
            k.CreateSubKey(@"shell\open\command").SetValue("", "\"" + exe + "\" \"%1\"");
            k.Close();
        }
        else
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\microsoft-edge", false); }
            catch { }
        }
    }

    public static void AbrePadroesWindows()
    {
        try { System.Diagnostics.Process.Start("ms-settings:defaultapps"); }
        catch { Amb.AbreExterno("control.exe"); }
    }
}
