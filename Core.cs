using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Microsoft.Win32;

// ---------------------------------------------------------------------------
// Ambiente: constantes do produto, chamadas do Windows e utilidades de sistema.
// ---------------------------------------------------------------------------
static class Amb
{
    public const string Produto = "FirawSelector";
    public const string Marca = "Firawynix";
    public const string Versao = "1.0.1";
    public const string Site = "https://firawselector.firawynix.com.br";
    public const string Repo = "https://github.com/firawynix/firaw-selector";

    public const string ChaveDesinstalar =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\FirawSelector";
    public const string ChaveCliente = @"Software\Clients\StartMenuInternet\FirawSelector";
    public const string ProgIdUrl = "FirawSelectorURL";
    public const string ProgIdHtml = "FirawSelectorHTML";

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
    public static extern int SetPreferredAppMode(int mode);

    [DllImport("uxtheme.dll", EntryPoint = "#136")]
    public static extern void FlushMenuThemes();

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr wp, IntPtr lp);

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int PrivateExtractIcons(string file, int index, int cx, int cy,
        IntPtr[] icons, int[] ids, int count, int flags);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr hIcon);

    public const int WM_NCLBUTTONDOWN = 0xA1;
    public const int WM_NCHITTEST = 0x84;
    public const int HTCAPTION = 2;
    public const int VK_SHIFT = 0x10;

    public static bool ShiftPressionado()
    {
        try { return (GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0; }
        catch { return false; }
    }

    public static bool PrefereEscuro()
    {
        try
        {
            object v = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", 1);
            return v != null && Convert.ToInt32(v) == 0;
        }
        catch { return false; }
    }

    public static void BarraEscura(IntPtr hwnd)
    {
        int on = 1;
        try { DwmSetWindowAttribute(hwnd, 20, ref on, sizeof(int)); }
        catch { }
    }

    /// <summary>Pinta a borda da janela com a cor da marca (Windows 11 build 22000+).</summary>
    public static void BordaColorida(IntPtr hwnd, Color cor)
    {
        // DWMWA_BORDER_COLOR = 34, no formato 0x00BBGGRR.
        int v = (cor.B << 16) | (cor.G << 8) | cor.R;
        try { DwmSetWindowAttribute(hwnd, 34, ref v, sizeof(int)); }
        catch { }
    }

    public static string Windows()
    {
        try
        {
            RegistryKey k = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (k == null) return Environment.OSVersion.VersionString;

            string nome = (string)k.GetValue("ProductName", "Windows");
            string build = (string)k.GetValue("CurrentBuild", "");
            string versao = (string)k.GetValue("DisplayVersion", "");

            int b;
            // O registro segue dizendo "Windows 10" no 11; a build e que separa.
            if (int.TryParse(build, out b) && b >= 22000 && nome.Contains("Windows 10"))
                nome = nome.Replace("Windows 10", "Windows 11");

            string txt = nome;
            if (!string.IsNullOrEmpty(versao)) txt += " " + versao;
            txt += " (build " + build + ")";
            return txt;
        }
        catch { return Environment.OSVersion.VersionString; }
    }

    public static int BuildWindows()
    {
        try
        {
            RegistryKey k = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            int b;
            if (k != null && int.TryParse((string)k.GetValue("CurrentBuild", "0"), out b)) return b;
        }
        catch { }
        return 0;
    }

    public static string DotNet(out int release)
    {
        release = 0;
        try
        {
            RegistryKey k = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full");
            if (k == null) return "4.0";

            object r = k.GetValue("Release");
            if (r == null) return "4.0";
            release = Convert.ToInt32(r);

            if (release >= 533320) return "4.8.1";
            if (release >= 528040) return "4.8";
            if (release >= 461808) return "4.7.2";
            if (release >= 460798) return "4.7";
            if (release >= 394802) return "4.6.2";
            if (release >= 393295) return "4.6.1";
            if (release >= 379893) return "4.5.2";
            return "4.5";
        }
        catch { return "?"; }
    }

    /// <summary>Fonte de glifos do Windows usada nos botoes da barra de titulo.</summary>
    public static string FonteGlifos()
    {
        foreach (FontFamily f in FontFamily.Families)
            if (f.Name == "Segoe Fluent Icons") return "Segoe Fluent Icons";
        foreach (FontFamily f in FontFamily.Families)
            if (f.Name == "Segoe MDL2 Assets") return "Segoe MDL2 Assets";
        return null;
    }

    public static string PastaDados
    {
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), Produto);
        }
    }

    public static string PastaExe
    {
        get
        {
            try { return Path.GetDirectoryName(Application.ExecutablePath); }
            catch { return Environment.CurrentDirectory; }
        }
    }

    /// <summary>
    /// Onde o config vive. Um config.ini ao lado do .exe manda no do perfil — e
    /// assim o programa roda portatil, de um pendrive, sem sujar a maquina.
    /// </summary>
    public static string CaminhoIni()
    {
        string aoLado = Path.Combine(PastaExe, "config.ini");
        if (File.Exists(aoLado)) return aoLado;
        return Path.Combine(PastaDados, "config.ini");
    }

    public static string CaminhoLog()
    {
        return Path.Combine(PastaDados, "firawselector.log");
    }

    public static void Registra(string linha)
    {
        try
        {
            Directory.CreateDirectory(PastaDados);
            string arq = CaminhoLog();
            // Registro curto de proposito: 200 linhas acham o erro de ontem e nao
            // viram um arquivo de 40 MB esquecido no perfil do usuario.
            if (File.Exists(arq) && new FileInfo(arq).Length > 200 * 1024)
            {
                string[] velhas = File.ReadAllLines(arq);
                List<string> corte = new List<string>();
                for (int i = Math.Max(0, velhas.Length - 200); i < velhas.Length; i++)
                    corte.Add(velhas[i]);
                File.WriteAllLines(arq, corte.ToArray(), Encoding.UTF8);
            }
            File.AppendAllText(arq,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\t" + linha + Environment.NewLine,
                Encoding.UTF8);
        }
        catch { }
    }

    /// <summary>Icone de um executavel no tamanho pedido (o de 32 esticado fica feio).</summary>
    public static Icon IconeDoArquivo(string arquivo, int indice, int tamanho)
    {
        if (string.IsNullOrEmpty(arquivo) || !File.Exists(arquivo)) return null;
        IntPtr[] h = new IntPtr[1];
        int[] ids = new int[1];
        try
        {
            int n = PrivateExtractIcons(arquivo, indice, tamanho, tamanho, h, ids, 1, 0);
            if (n > 0 && h[0] != IntPtr.Zero)
            {
                Icon ic = (Icon)Icon.FromHandle(h[0]).Clone();
                DestroyIcon(h[0]);
                return ic;
            }
        }
        catch { }
        return null;
    }

    public static void AbreExterno(string alvo)
    {
        try { System.Diagnostics.Process.Start(alvo); }
        catch { }
    }
}

// ---------------------------------------------------------------------------
// Paleta. O ciano da marca (#22d3ee) e o mesmo do portfolio e do console do ERP.
// ---------------------------------------------------------------------------
class Cores
{
    public bool Escuro;
    public Color Fundo, Painel, Campo, Linha, Texto, Texto2, Texto3;
    public Color Acento, AcentoEsc, AcentoTexto, SobreAcento;
    public Color Bom, Aviso, Ruim, Fechar;

    public static Cores Atual = Escura();

    public static Cores Escura()
    {
        Cores c = new Cores();
        c.Escuro = true;
        c.Fundo = Color.FromArgb(15, 18, 24);
        c.Painel = Color.FromArgb(22, 27, 34);
        c.Campo = Color.FromArgb(30, 36, 45);
        c.Linha = Color.FromArgb(45, 54, 66);
        c.Texto = Color.FromArgb(230, 237, 243);
        c.Texto2 = Color.FromArgb(148, 163, 184);
        c.Texto3 = Color.FromArgb(100, 116, 139);
        c.Acento = Color.FromArgb(34, 211, 238);
        c.AcentoEsc = Color.FromArgb(8, 145, 178);
        c.AcentoTexto = Color.FromArgb(103, 232, 249);
        c.SobreAcento = Color.FromArgb(6, 22, 30);
        c.Bom = Color.FromArgb(52, 211, 153);
        c.Aviso = Color.FromArgb(251, 191, 36);
        c.Ruim = Color.FromArgb(248, 113, 113);
        c.Fechar = Color.FromArgb(196, 43, 28);
        return c;
    }

    public static Cores Clara()
    {
        Cores c = new Cores();
        c.Escuro = false;
        c.Fundo = Color.FromArgb(246, 248, 250);
        c.Painel = Color.FromArgb(255, 255, 255);
        c.Campo = Color.FromArgb(255, 255, 255);
        c.Linha = Color.FromArgb(208, 216, 224);
        c.Texto = Color.FromArgb(23, 31, 41);
        c.Texto2 = Color.FromArgb(85, 100, 118);
        c.Texto3 = Color.FromArgb(130, 145, 163);
        c.Acento = Color.FromArgb(8, 145, 178);
        c.AcentoEsc = Color.FromArgb(14, 116, 144);
        c.AcentoTexto = Color.FromArgb(14, 116, 144);
        c.SobreAcento = Color.FromArgb(255, 255, 255);
        c.Bom = Color.FromArgb(16, 145, 105);
        c.Aviso = Color.FromArgb(180, 120, 10);
        c.Ruim = Color.FromArgb(190, 45, 45);
        c.Fechar = Color.FromArgb(196, 43, 28);
        return c;
    }

    /// <summary>tema: sistema | escuro | claro.</summary>
    public static void Aplicar(string tema)
    {
        bool escuro;
        if (tema == "claro") escuro = false;
        else if (tema == "escuro") escuro = true;
        else escuro = Amb.PrefereEscuro();

        Atual = escuro ? Escura() : Clara();
        try
        {
            Amb.SetPreferredAppMode(escuro ? 2 : 3);
            Amb.FlushMenuThemes();
        }
        catch { }
    }
}

// ---------------------------------------------------------------------------
// Idioma. Sem arquivo externo: as duas tabelas moram aqui e o binario e um so.
// ---------------------------------------------------------------------------
static class Idioma
{
    static readonly Dictionary<string, string> pt = new Dictionary<string, string>();
    static readonly Dictionary<string, string> en = new Dictionary<string, string>();
    static bool usaPt = true;

    /// <summary>"pt" ou "en" — o que esta valendo agora.</summary>
    public static string Codigo { get { return usaPt ? "pt" : "en"; } }

    static void A(string chave, string ptTxt, string enTxt)
    {
        pt[chave] = ptTxt;
        en[chave] = enTxt;
    }

    static Idioma()
    {
        // -------- geral --------
        A("app.tagline", "Escolha o navegador a cada link", "Pick the browser for every link");
        A("btn.ok", "OK", "OK");
        A("btn.cancelar", "Cancelar", "Cancel");
        A("btn.fechar", "Fechar", "Close");
        A("btn.salvar", "Salvar", "Save");
        A("btn.adicionar", "Adicionar", "Add");
        A("btn.editar", "Editar", "Edit");
        A("btn.remover", "Remover", "Remove");
        A("btn.subir", "Subir", "Move up");
        A("btn.descer", "Descer", "Move down");
        A("btn.testar", "Testar", "Test");
        A("btn.procurar", "Procurar", "Browse");
        A("btn.copiar", "Copiar", "Copy");
        A("btn.limpar", "Limpar", "Clear");
        A("btn.sim", "Sim", "Yes");
        A("btn.nao", "Não", "No");

        // -------- janela de escolha --------
        A("esc.titulo", "Abrir link com", "Open link with");
        A("esc.dica", "1-9 escolhe · Enter usa o padrão · Esc cancela · Ctrl abre privativo",
                      "1-9 to pick · Enter for default · Esc cancels · Ctrl for private");
        A("esc.lembrar", "Sempre usar este navegador para {0}", "Always use this browser for {0}");
        A("esc.privativo", "Janela privativa", "Private window");
        A("esc.copiar", "Copiar link", "Copy link");
        A("esc.editar", "Editar", "Edit");
        A("esc.auto", "Abrindo em {0}s", "Opening in {0}s");
        A("esc.vazio", "Nenhum navegador encontrado. Abra o FirawSelector Studio.",
                       "No browser found. Open FirawSelector Studio.");
        A("esc.copiado", "Link copiado", "Link copied");
        A("esc.editarTit", "Editar o link antes de abrir", "Edit the link before opening");

        // -------- abas do Studio --------
        A("aba.navegadores", "Navegadores", "Browsers");
        A("aba.regras", "Regras", "Rules");
        A("aba.opcoes", "Opções", "Options");
        A("aba.registro", "Registro", "Log");
        A("aba.sobre", "Sobre", "About");

        // -------- navegadores --------
        A("nav.titulo", "Navegadores encontrados no computador", "Browsers found on this computer");
        A("nav.reprocurar", "Procurar de novo", "Scan again");
        A("nav.perfis", "Buscar perfis", "Find profiles");
        A("nav.manual", "Adicionar à mão", "Add manually");
        A("nav.padrao", "padrão", "default");
        A("nav.segundo", "segundo", "second");
        A("nav.oculto", "oculto", "hidden");
        A("nav.definirPadrao", "Definir como padrão", "Set as default");
        A("nav.definirSegundo", "Definir como segundo", "Set as second");
        A("nav.ocultar", "Ocultar / mostrar", "Hide / show");
        A("nav.abrirTeste", "Abrir um link de teste", "Open a test link");
        A("nav.nome", "Nome", "Name");
        A("nav.caminho", "Programa (.exe)", "Program (.exe)");
        A("nav.args", "Argumentos extras", "Extra arguments");
        A("nav.argsPriv", "Argumentos de janela privativa", "Private window arguments");
        A("nav.novo", "Novo navegador", "New browser");
        A("nav.icone", "Ícone — opcional (.ico, ou .exe/.dll de onde extrair)",
                       "Icon — optional (.ico, or an .exe/.dll to extract from)");
        A("nav.iconeVazio", "vazio: usa o ícone do próprio programa",
                            "empty: uses the program's own icon");
        A("nav.iconeFiltro",
          "Ícones e programas|*.ico;*.exe;*.dll|Ícone (*.ico)|*.ico|Programa (*.exe;*.dll)|*.exe;*.dll",
          "Icons and programs|*.ico;*.exe;*.dll|Icon (*.ico)|*.ico|Program (*.exe;*.dll)|*.exe;*.dll");
        A("nav.iconeIndice", "Qual ícone de dentro do arquivo (0 = o primeiro)",
                             "Which icon inside the file (0 = the first one)");
        A("nav.iconeRuim", "Não consegui ler ícone desse arquivo.",
                           "Could not read an icon from that file.");
        A("nav.achados", "{0} navegador(es) · {1} perfil(is)", "{0} browser(s) · {1} profile(s)");
        A("nav.semExe", "Escolha o programa do navegador.", "Pick the browser program.");
        A("nav.perfisAchados", "{0} perfil(is) adicionado(s).", "{0} profile(s) added.");
        A("nav.perfisNenhum", "Nenhum perfil novo encontrado.", "No new profile found.");

        // -------- regras --------
        A("reg.titulo", "Regras — a primeira que casar vence", "Rules — first match wins");
        A("reg.padraoTxt", "Padrão", "Pattern");
        A("reg.tipo", "Tipo", "Kind");
        A("reg.navegador", "Navegador", "Browser");
        A("reg.privativo", "Privativo", "Private");
        A("reg.ativa", "Ativa", "Active");
        A("reg.tipo.host", "domínio", "domain");
        A("reg.tipo.url", "endereço", "address");
        A("reg.tipo.regex", "expressão", "regex");
        A("reg.nova", "Nova regra", "New rule");
        A("reg.testar", "Testar um endereço:", "Test an address:");
        A("reg.resultado", "{0}  —  {1}", "{0}  —  {1}");
        A("reg.semCasar", "nenhuma regra casou", "no rule matched");
        A("reg.ajuda",
          "domínio: intranet.local, *.google.com   ·   endereço: https://*/docs/*   ·   expressão: regex ECMAScript no endereço inteiro",
          "domain: intranet.local, *.google.com   ·   address: https://*/docs/*   ·   regex: ECMAScript over the whole address");
        A("reg.vazia", "Escreva o padrão da regra.", "Type the rule pattern.");
        A("reg.semNavegador", "Escolha o navegador da regra.", "Pick the rule browser.");
        A("reg.regexRuim", "Expressão inválida: {0}", "Invalid regex: {0}");

        // -------- opcoes --------
        A("op.quando", "Quando um link chega", "When a link arrives");
        A("op.modo.perguntar", "Sempre perguntar qual navegador", "Always ask which browser");
        A("op.modo.regras", "Seguir as regras; sem regra, usar o padrão",
                            "Follow the rules; with no rule, use the default");
        A("op.tempo", "Escolher sozinho depois de (segundos, 0 desliga)",
                      "Auto-pick after (seconds, 0 turns it off)");
        A("op.lembrar", "Lembrar a escolha por site", "Remember the choice per site");
        A("op.esquecer", "Esquecer os {0} site(s) lembrado(s)", "Forget the {0} remembered site(s)");
        A("op.shift", "Segurar Shift sempre abre a janela de escolha",
                      "Holding Shift always opens the picker");
        A("op.limpar", "Tirar parâmetros de rastreio (utm_*, fbclid, gclid...)",
                       "Strip tracking parameters (utm_*, fbclid, gclid...)");
        A("op.expandir", "Expandir links encurtados antes de decidir",
                         "Expand shortened links before deciding");
        A("op.log", "Guardar registro dos links em arquivo", "Keep a log file of the links");
        A("op.idioma", "Idioma", "Language");
        A("op.idioma.auto", "Seguir o Windows", "Follow Windows");
        A("op.tema", "Tema", "Theme");
        A("op.tema.sistema", "Seguir o Windows", "Follow Windows");
        A("op.tema.escuro", "Escuro", "Dark");
        A("op.tema.claro", "Claro", "Light");
        A("op.windows", "Lugar do FirawSelector no Windows", "FirawSelector's place in Windows");
        A("op.registrar", "Registrar como navegador", "Register as a browser");
        A("op.desregistrar", "Remover o registro", "Remove the registration");
        A("op.padroesWin", "Abrir Configurações do Windows", "Open Windows Settings");
        A("op.edge", "Capturar também os links microsoft-edge: (Pesquisa e widgets)",
                     "Also capture microsoft-edge: links (Search and widgets)");
        A("op.registrado", "registrado", "registered");
        A("op.naoRegistrado", "não registrado", "not registered");
        A("op.ehPadrao", "é o navegador padrão", "is the default browser");
        A("op.naoEhPadrao", "não é o navegador padrão", "is not the default browser");
        A("op.passoPadrao",
          "Registrado. Agora escolha o FirawSelector em Configurações > Aplicativos > Aplicativos padrão.",
          "Registered. Now pick FirawSelector in Settings > Apps > Default apps.");
        A("op.desregistrado", "Registro removido.", "Registration removed.");
        A("op.esquecido", "Lembrados apagados.", "Remembered sites cleared.");
        A("op.compacto", "Janela de escolha enxuta: só ícone, nome e número",
                         "Slim picker: icon, name and number only");
        A("op.exportar", "Exportar configuração", "Export configuration");
        A("op.importar", "Importar configuração", "Import configuration");
        A("op.iniFiltro", "Configuração do FirawSelector (*.ini)|*.ini|Todos os arquivos|*.*",
                          "FirawSelector configuration (*.ini)|*.ini|All files|*.*");
        A("op.exportado", "Configuração salva em {0}", "Configuration saved to {0}");
        A("op.importado", "Configuração importada.", "Configuration imported.");
        A("op.importarAviso",
          "Importar substitui os navegadores, as regras e as opções que estão aqui agora. Continuar?",
          "Importing replaces the browsers, rules and options you have now. Continue?");
        A("op.importarRuim", "Não consegui ler esse arquivo: {0}",
                             "Could not read that file: {0}");

        // -------- faixa de aviso --------
        A("faixa.naoPadrao", "O FirawSelector não é o navegador padrão do Windows.",
                             "FirawSelector is not the default Windows browser.");
        A("faixa.tornarPadrao", "Tornar padrão", "Make default");

        // -------- registro --------
        A("lg.titulo", "Últimos links que passaram por aqui", "Last links that came through");
        A("lg.limpar", "Limpar", "Clear");
        A("lg.vazio", "Nada registrado ainda. Ligue o registro nas Opções.",
                      "Nothing logged yet. Turn the log on in Options.");
        A("lg.abrirPasta", "Abrir a pasta", "Open the folder");

        // -------- motivos --------
        A("por.regra", "regra \"{0}\"", "rule \"{0}\"");
        A("por.lembrado", "lembrado deste site", "remembered for this site");
        A("por.padrao", "navegador padrão", "default browser");
        A("por.escolhido", "você escolheu", "you picked");
        A("por.shift", "Shift forçou a escolha", "Shift forced the picker");
        A("por.semNada", "nenhum navegador configurado", "no browser configured");

        // -------- sobre --------
        A("sobre.o", "O que ele faz", "What it does");
        A("sobre.texto",
          "O FirawSelector se registra como navegador do Windows. Quando um link é aberto por qualquer programa, ele decide: se alguma regra casar, manda direto para o navegador certo; se nenhuma casar, abre a janela de escolha.",
          "FirawSelector registers itself as a Windows browser. When any program opens a link, it decides: if a rule matches, it goes straight to the right browser; if none matches, the picker opens.");
        A("sobre.config", "Configuração", "Configuration");
        A("sobre.site", "Site do produto", "Product site");
        A("sobre.repo", "Código", "Source");

        // -------- instalador --------
        A("inst.titulo", "Instalar o {0} {1}", "Install {0} {1}");
        A("inst.verificacao", "Verificação do sistema", "System check");
        A("inst.onde", "Instalar em", "Install to");
        A("inst.mesa", "Criar atalho na área de trabalho", "Create a desktop shortcut");
        A("inst.iniciar", "Adicionar ao menu Iniciar", "Add to the Start menu");
        A("inst.registrar", "Registrar como navegador do Windows", "Register as a Windows browser");
        A("inst.instalar", "Instalar", "Install");
        A("inst.atualizar", "Atualizar", "Update");
        A("inst.desinstalar", "Desinstalar", "Uninstall");
        A("inst.nada", "O instalador traz tudo dentro dele: nada é baixado.",
                       "The installer carries everything: nothing is downloaded.");
        A("inst.ja", "Já instalado em {0}. Atualizar troca os programas e mantém suas regras.",
                     "Already installed at {0}. Updating swaps the programs and keeps your rules.");
        A("inst.copiando", "Copiando arquivos...", "Copying files...");
        A("inst.atalhos", "Criando atalhos...", "Creating shortcuts...");
        A("inst.pronto", "Instalado em {0}", "Installed at {0}");
        A("inst.abrirAgora", "{0} instalado.\n\nAbrir agora para configurar os navegadores?",
                             "{0} installed.\n\nOpen it now to set up the browsers?");
        A("inst.aberto", "Um dos programas está aberto agora. Feche as janelas do {0} e tente de novo.",
                         "One of the programs is running. Close the {0} windows and try again.");
        A("inst.falhou", "Falhou: {0}", "Failed: {0}");
        A("inst.escolhaLocal", "Escolha onde instalar.", "Pick where to install.");
        A("inst.confirmaRemover",
          "Remover o {0}?\n\nSerão apagados: os programas em {1} e {2} atalho(s).\nAs regras e o config ficam em {3}.",
          "Remove {0}?\n\nThis deletes: the programs at {1} and {2} shortcut(s).\nRules and config stay at {3}.");
        A("inst.removido", "{0} removido.", "{0} removed.");
        A("inst.presos", "{0} arquivo(s) estavam abertos e ficaram para trás em {1}.",
                         "{0} file(s) were in use and were left behind at {1}.");
        A("inst.eraPadrao",
          "O FirawSelector era o navegador padrão. Escolha outro em Configurações > Aplicativos padrão.",
          "FirawSelector was the default browser. Pick another one in Settings > Default apps.");
        A("inst.windows", "Windows", "Windows");
        A("inst.arquitetura", "Arquitetura", "Architecture");
        A("inst.dotnet", ".NET Framework", ".NET Framework");
        A("inst.fonte", "Fonte dos ícones", "Icon font");
        A("inst.navegadores", "Navegadores", "Browsers");
        A("inst.bits", "{0}  -  o programa é AnyCPU, roda nos dois",
                       "{0}  -  the program is AnyCPU, it runs on both");
        A("inst.semFonte", "nenhuma  -  os botões usam texto", "none  -  the buttons fall back to text");
        A("inst.recomendado", "{0}  -  funciona, mas o recomendado é 4.8",
                              "{0}  -  works, but 4.8 is recommended");
        A("inst.achou", "{0} encontrado(s)", "{0} found");
        A("inst.nenhum", "nenhum encontrado  -  dá para adicionar à mão depois",
                         "none found  -  you can add them by hand later");
    }

    /// <summary>cfg: auto | pt | en.</summary>
    public static void Definir(string cfg)
    {
        if (cfg == "pt") { usaPt = true; return; }
        if (cfg == "en") { usaPt = false; return; }
        try
        {
            usaPt = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName
                .Equals("pt", StringComparison.OrdinalIgnoreCase);
        }
        catch { usaPt = true; }
    }

    public static string T(string chave)
    {
        Dictionary<string, string> d = usaPt ? pt : en;
        string v;
        if (d.TryGetValue(chave, out v)) return v;
        return chave;
    }

    public static string T(string chave, params object[] args)
    {
        try { return string.Format(T(chave), args); }
        catch { return T(chave); }
    }
}
