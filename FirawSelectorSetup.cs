using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

// ---------------------------------------------------------------------------
// Instalador. Traz os dois programas dentro dele: nada e baixado, nada precisa
// de administrador — a instalacao mora no perfil do usuario.
// ---------------------------------------------------------------------------
class Instalador : JanelaFiraw
{
    readonly bool jaInstalado;
    readonly string instalacaoAtual;

    TextBox txtLocal;
    CheckBox chkMesa, chkIniciar, chkRegistrar;
    Panel painelChecagem;
    Label lblStatus;
    Button btnInstalar, btnDesinstalar;

    static string LocalPadrao
    {
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Amb.Produto);
        }
    }

    static string Mesa { get { return Environment.GetFolderPath(Environment.SpecialFolder.Desktop); } }

    static string MenuIniciar
    {
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Windows\Start Menu\Programs");
        }
    }

    public Instalador()
        : base(Idioma.T("inst.titulo", Amb.Produto, Amb.Versao), false, false)
    {
        RegistryKey k = Registry.CurrentUser.OpenSubKey(Amb.ChaveDesinstalar);
        if (k != null)
        {
            instalacaoAtual = (string)k.GetValue("InstallLocation", null);
            jaInstalado = !string.IsNullOrEmpty(instalacaoAtual) && Directory.Exists(instalacaoAtual);
        }

        Monta();
        Checar();
    }

    void Monta()
    {
        int y = 14;

        Label nome = UI.Rotulo(Amb.Produto + " " + Amb.Versao, 20, y);
        nome.Font = new Font("Segoe UI Semibold", 15f, FontStyle.Bold);
        nome.ForeColor = C.Acento;
        Corpo.Controls.Add(nome);
        y += 32;

        Label sub = UI.Rotulo(Idioma.T("sobre.texto"), 20, y);
        sub.MaximumSize = new Size(636, 60);
        sub.ForeColor = C.Texto2;
        Corpo.Controls.Add(sub);
        y += 62;

        Corpo.Controls.Add(UI.Titulo(Idioma.T("inst.verificacao"), 20, y));
        y += 26;

        painelChecagem = UI.Cartao(20, y, 636, 146);
        Corpo.Controls.Add(painelChecagem);
        y += 158;

        Corpo.Controls.Add(UI.Rotulo(Idioma.T("inst.onde"), 20, y));
        y += 22;

        txtLocal = UI.Campo(20, y, 520);
        txtLocal.Text = jaInstalado ? instalacaoAtual : LocalPadrao;
        if (string.IsNullOrEmpty(txtLocal.Text)) txtLocal.Text = LocalPadrao;
        Corpo.Controls.Add(txtLocal);

        Corpo.Controls.Add(UI.Botao(Idioma.T("btn.procurar"), 548, y - 3, 108, delegate
        {
            using (FolderBrowserDialog d = new FolderBrowserDialog())
            {
                d.Description = Idioma.T("inst.onde");
                if (d.ShowDialog(this) == DialogResult.OK)
                    txtLocal.Text = Path.Combine(d.SelectedPath, Amb.Produto);
            }
        }, false));
        y += 38;

        chkMesa = UI.Caixa(Idioma.T("inst.mesa"), 20, y, true);
        Corpo.Controls.Add(chkMesa);
        chkIniciar = UI.Caixa(Idioma.T("inst.iniciar"), 320, y, true);
        Corpo.Controls.Add(chkIniciar);
        y += 26;

        chkRegistrar = UI.Caixa(Idioma.T("inst.registrar"), 20, y, true);
        Corpo.Controls.Add(chkRegistrar);
        y += 36;

        btnInstalar = UI.Botao(jaInstalado ? Idioma.T("inst.atualizar") : Idioma.T("inst.instalar"),
            20, y, 190, Instalar, true);
        Corpo.Controls.Add(btnInstalar);

        btnDesinstalar = UI.Botao(Idioma.T("inst.desinstalar"), 222, y, 150,
            delegate { Desinstalar(txtLocal.Text.Trim(), this); }, false);
        btnDesinstalar.Visible = jaInstalado;
        Corpo.Controls.Add(btnDesinstalar);

        Corpo.Controls.Add(UI.Botao(Idioma.T("btn.fechar"), 556, y, 100,
            delegate { Close(); }, false));
        y += 40;

        lblStatus = UI.Rotulo("", 20, y);
        lblStatus.AutoSize = false;
        lblStatus.Size = new Size(636, 40);
        lblStatus.ForeColor = C.Texto3;
        Corpo.Controls.Add(lblStatus);
        y += 40;

        // Altura pela conta do conteudo: numero escrito a mao ja cortou botao.
        ClientSize = new Size(680, Barra.Height + y + 16 + 2);
    }

    void LinhaChecagem(int i, string titulo, string valor, Color cor)
    {
        Label a = new Label();
        a.Text = titulo;
        a.Location = new Point(14, 12 + i * 26);
        a.Size = new Size(180, 20);
        a.ForeColor = C.Texto2;
        painelChecagem.Controls.Add(a);

        Label b = new Label();
        b.Text = valor;
        b.Location = new Point(198, 12 + i * 26);
        b.Size = new Size(424, 20);
        b.ForeColor = cor;
        painelChecagem.Controls.Add(b);
    }

    void Checar()
    {
        painelChecagem.Controls.Clear();
        int build = Amb.BuildWindows();

        LinhaChecagem(0, Idioma.T("inst.windows"), Amb.Windows(), build >= 17763 ? C.Bom : C.Aviso);

        LinhaChecagem(1, Idioma.T("inst.arquitetura"),
            Idioma.T("inst.bits", Environment.Is64BitOperatingSystem ? "64 bits" : "32 bits"), C.Bom);

        int release;
        string net = Amb.DotNet(out release);
        LinhaChecagem(2, Idioma.T("inst.dotnet"),
            release >= 528040 ? net : Idioma.T("inst.recomendado", net),
            release >= 528040 ? C.Bom : C.Aviso);

        string fonte = Amb.FonteGlifos();
        LinhaChecagem(3, Idioma.T("inst.fonte"),
            fonte != null ? fonte : Idioma.T("inst.semFonte"), fonte != null ? C.Bom : C.Aviso);

        int quantos = Navegador.Detecta().Count;
        LinhaChecagem(4, Idioma.T("inst.navegadores"),
            quantos > 0 ? Idioma.T("inst.achou", quantos) : Idioma.T("inst.nenhum"),
            quantos > 0 ? C.Bom : C.Aviso);

        lblStatus.Text = jaInstalado
            ? Idioma.T("inst.ja", instalacaoAtual)
            : Idioma.T("inst.nada");
    }

    // ---------- instalacao ----------

    static void Extrai(string recurso, string destino)
    {
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(recurso))
        {
            if (s == null) throw new Exception("Recurso ausente no instalador: " + recurso);
            byte[] buf = new byte[s.Length];
            int lidos = 0;
            while (lidos < buf.Length)
            {
                int n = s.Read(buf, lidos, buf.Length - lidos);
                if (n <= 0) break;
                lidos += n;
            }
            File.WriteAllBytes(destino, buf);
        }
    }

    static void ExtraiExtensao(string local, string navegador, string manifesto)
    {
        string pasta = Path.Combine(local, "Extensoes", navegador);
        Directory.CreateDirectory(pasta);
        Extrai(manifesto, Path.Combine(pasta, "manifest.json"));
        Extrai("ext.background.js", Path.Combine(pasta, "background.js"));
        Extrai("ext.content.js", Path.Combine(pasta, "content.js"));
        foreach (string tamanho in new string[] { "16", "32", "48", "128" })
            Extrai("ext.icon" + tamanho + ".png", Path.Combine(pasta, "icon" + tamanho + ".png"));
    }

    static string Json(string valor)
    {
        return valor.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    static void ApontaHost(string chave, string manifesto)
    {
        RegistryKey k = Registry.CurrentUser.CreateSubKey(chave + "\\" + Amb.HostNativo);
        k.SetValue("", manifesto);
        k.Close();
    }

    static void RegistraHostNativo(string local, string host)
    {
        string pasta = Path.Combine(local, "Integracao");
        Directory.CreateDirectory(pasta);

        string chromium = Path.Combine(pasta, Amb.HostNativo + ".chromium.json");
        string firefox = Path.Combine(pasta, Amb.HostNativo + ".firefox.json");

        string comum = "{\n" +
            "  \"name\": \"" + Amb.HostNativo + "\",\n" +
            "  \"description\": \"FirawSelector Native Messaging Host\",\n" +
            "  \"path\": \"" + Json(host) + "\",\n" +
            "  \"type\": \"stdio\",\n";
        File.WriteAllText(chromium, comum +
            "  \"allowed_origins\": [\"chrome-extension://" +
            Amb.ExtensaoChromiumId + "/\"]\n}\n", new UTF8Encoding(false));
        File.WriteAllText(firefox, comum +
            "  \"allowed_extensions\": [\"" + Amb.ExtensaoFirefoxId + "\"]\n}\n",
            new UTF8Encoding(false));

        ApontaHost(@"Software\Google\Chrome\NativeMessagingHosts", chromium);
        ApontaHost(@"Software\Microsoft\Edge\NativeMessagingHosts", chromium);
        ApontaHost(@"Software\Mozilla\NativeMessagingHosts", firefox);
    }

    static void RemoveHostNativo()
    {
        foreach (string raiz in new string[] {
            @"Software\Google\Chrome\NativeMessagingHosts",
            @"Software\Microsoft\Edge\NativeMessagingHosts",
            @"Software\Mozilla\NativeMessagingHosts" })
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(raiz + "\\" + Amb.HostNativo, false); }
            catch { }
        }
    }

    static void CriaAtalho(string lnk, string alvo, string args, string dir, string icone, string desc)
    {
        Type t = Type.GetTypeFromProgID("WScript.Shell");
        object sh = Activator.CreateInstance(t);
        object a = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, sh, new object[] { lnk });
        Type ta = a.GetType();
        ta.InvokeMember("TargetPath", BindingFlags.SetProperty, null, a, new object[] { alvo });
        ta.InvokeMember("Arguments", BindingFlags.SetProperty, null, a, new object[] { args });
        ta.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, a, new object[] { dir });
        ta.InvokeMember("IconLocation", BindingFlags.SetProperty, null, a, new object[] { icone });
        ta.InvokeMember("Description", BindingFlags.SetProperty, null, a, new object[] { desc });
        ta.InvokeMember("Save", BindingFlags.InvokeMethod, null, a, null);
    }

    // Tudo o que a instalacao faz, com ou sem janela. Sem janela (/S) e como o
    // Firawynix Center instala e atualiza por cima.
    static void Executa(string local, bool mesa, bool iniciar, bool registrar, Action<string> status)
    {
        Directory.CreateDirectory(local);

        string motor = Path.Combine(local, Amb.Produto + ".exe");
        string studio = Path.Combine(local, Amb.Produto + " Studio.exe");
        string host = Path.Combine(local, Amb.Produto + " Host.exe");
        string atualizador = Path.Combine(local, "FirawAutoUpdate.exe");
        string desinst = Path.Combine(local, "Desinstalar.exe");

        status(Idioma.T("inst.copiando"));

        try
        {
            Extrai(Amb.Produto + ".exe", motor);
            Extrai(Amb.Produto + " Studio.exe", studio);
            Extrai(Amb.Produto + " Host.exe", host);
            Extrai("FirawAutoUpdate.exe", atualizador);
            ExtraiExtensao(local, "Chrome", "ext.chrome.manifest.json");
            ExtraiExtensao(local, "Edge", "ext.edge.manifest.json");
            ExtraiExtensao(local, "Opera", "ext.opera.manifest.json");
            ExtraiExtensao(local, "Firefox", "ext.firefox.manifest.json");
        }
        catch (IOException ex)
        {
            throw new ArquivoEmUso(ex);
        }

        File.Copy(Assembly.GetExecutingAssembly().Location, desinst, true);
        RegistraHostNativo(local, host);

        status(Idioma.T("inst.atalhos"));

        string desc = Amb.Produto + " - " + Idioma.T("app.tagline");
        if (mesa)
            CriaAtalho(Path.Combine(Mesa, Amb.Produto + ".lnk"), studio, "", local, studio + ",0", desc);
        if (iniciar)
            CriaAtalho(Path.Combine(MenuIniciar, Amb.Produto + ".lnk"), studio, "", local, studio + ",0", desc);

        long tamanho = 0;
        foreach (string f in Directory.GetFiles(local, "*", SearchOption.AllDirectories))
        {
            try { tamanho += new FileInfo(f).Length; }
            catch { }
        }

        RegistryKey k = Registry.CurrentUser.CreateSubKey(Amb.ChaveDesinstalar);
        k.SetValue("DisplayName", Amb.Produto);
        k.SetValue("DisplayVersion", Amb.Versao);
        k.SetValue("DisplayIcon", studio);
        k.SetValue("Publisher", Amb.Marca);
        k.SetValue("URLInfoAbout", Amb.Site);
        k.SetValue("InstallLocation", local);
        k.SetValue("UninstallString", "\"" + desinst + "\" --uninstall");
        k.SetValue("EstimatedSize", (int)(tamanho / 1024), RegistryValueKind.DWord);
        k.SetValue("NoModify", 1, RegistryValueKind.DWord);
        k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        k.Close();

        if (registrar) Registrar.Registra(motor);
    }

    // Instalar/atualizar sem janela. Nao cria atalho (o Center cria os dele; quem
    // instalou na mao ja tem os seus) e o resultado vai no codigo de saida:
    // 0 = ok, 2 = arquivo em uso (programa aberto), 1 = outra falha.
    static int InstalaSemJanela(string pasta)
    {
        string local = pasta;
        if (string.IsNullOrEmpty(local))
        {
            RegistryKey k = Registry.CurrentUser.OpenSubKey(Amb.ChaveDesinstalar);
            if (k != null) local = (string)k.GetValue("InstallLocation", null);
        }
        if (string.IsNullOrEmpty(local)) local = LocalPadrao;

        int codigo;
        string recado;
        try
        {
            Executa(local, false, false, true, delegate(string texto) { });
            codigo = 0;
            recado = "ok";
        }
        catch (ArquivoEmUso ex) { codigo = 2; recado = "arquivo em uso: " + ex.Message; }
        catch (Exception ex) { codigo = 1; recado = ex.Message; }

        try
        {
            File.AppendAllText(Path.Combine(Path.GetTempPath(), "firawselector-setup.log"),
                DateTime.Now.ToString("s") + "  " + Amb.Versao + "  " + local + "  " + recado + Environment.NewLine);
        }
        catch { }
        return codigo;
    }

    void Instalar(object s, EventArgs e)
    {
        string local = txtLocal.Text.Trim();
        if (string.IsNullOrEmpty(local))
        {
            UI.Aviso(this, Idioma.T("inst.escolhaLocal"));
            return;
        }

        btnInstalar.Enabled = false;
        try
        {
            try
            {
                Executa(local, chkMesa.Checked, chkIniciar.Checked, chkRegistrar.Checked,
                    delegate(string texto) { lblStatus.Text = texto; Application.DoEvents(); });
            }
            catch (ArquivoEmUso)
            {
                UI.Aviso(this, Idioma.T("inst.aberto", Amb.Produto));
                return;
            }

            string studio = Path.Combine(local, Amb.Produto + " Studio.exe");
            lblStatus.Text = Idioma.T("inst.pronto", local);

            if (UI.Confirma(this, Idioma.T("inst.abrirAgora", Amb.Produto)))
            {
                System.Diagnostics.Process.Start(studio);
                if (chkRegistrar.Checked) Registrar.AbrePadroesWindows();
                Close();
            }
            else
            {
                btnDesinstalar.Visible = true;
                btnInstalar.Text = Idioma.T("inst.atualizar");
            }
        }
        catch (Exception ex)
        {
            UI.Aviso(this, Idioma.T("inst.falhou", ex.Message));
            lblStatus.Text = Idioma.T("inst.falhou", ex.Message);
        }
        finally { btnInstalar.Enabled = true; }
    }

    // ---------- desinstalacao ----------

    static List<string> AtalhosNossos(string local)
    {
        List<string> achados = new List<string>();
        string[] pastas = new string[] { Mesa, MenuIniciar,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar") };

        Type t = Type.GetTypeFromProgID("WScript.Shell");
        object sh = Activator.CreateInstance(t);

        foreach (string dir in pastas)
        {
            if (!Directory.Exists(dir)) continue;
            string[] arquivos;
            try { arquivos = Directory.GetFiles(dir, "*.lnk"); }
            catch { continue; }

            foreach (string lnk in arquivos)
            {
                try
                {
                    object a = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, sh,
                        new object[] { lnk });
                    Type ta = a.GetType();
                    string alvo = (string)ta.InvokeMember("TargetPath", BindingFlags.GetProperty, null, a, null);
                    string desc = (string)ta.InvokeMember("Description", BindingFlags.GetProperty, null, a, null);

                    bool nosso = !string.IsNullOrEmpty(alvo) && !string.IsNullOrEmpty(local) &&
                                 alvo.StartsWith(local, StringComparison.OrdinalIgnoreCase);
                    bool etiquetado = !string.IsNullOrEmpty(desc) &&
                                      desc.StartsWith(Amb.Produto, StringComparison.OrdinalIgnoreCase);
                    if (nosso || etiquetado) achados.Add(lnk);
                }
                catch { }
            }
        }
        return achados;
    }

    public static void Desinstalar(string local, IWin32Window dono)
    {
        Desinstalar(local, dono, false);
    }

    // silencioso = sem pergunta e sem recado no fim (o Center chama "Desinstalar.exe /S")
    public static void Desinstalar(string local, IWin32Window dono, bool silencioso)
    {
        if (string.IsNullOrEmpty(local))
        {
            RegistryKey k = Registry.CurrentUser.OpenSubKey(Amb.ChaveDesinstalar);
            if (k != null) local = (string)k.GetValue("InstallLocation", null);
        }

        bool eraPadrao = Registrar.EhPadrao();
        List<string> atalhos = AtalhosNossos(local);

        string pergunta = Idioma.T("inst.confirmaRemover",
            Amb.Produto, local, atalhos.Count, Amb.PastaDados);

        if (!silencioso && MessageBox.Show(pergunta, Amb.Produto, MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) != DialogResult.Yes) return;

        // O registro de navegador sai primeiro: apagar so os arquivos deixaria o
        // Windows mandando links para um programa que nao existe mais.
        Registrar.Remove();
        RemoveHostNativo();

        foreach (string a in atalhos)
        {
            try { File.Delete(a); }
            catch { }
        }

        string eu = Assembly.GetExecutingAssembly().Location;
        int presos = 0;
        if (!string.IsNullOrEmpty(local) && Directory.Exists(local))
        {
            foreach (string f in Directory.GetFiles(local, "*", SearchOption.AllDirectories))
            {
                if (string.Equals(f, eu, StringComparison.OrdinalIgnoreCase)) continue;
                try { File.Delete(f); }
                catch { presos++; }
            }
        }

        try { Registry.CurrentUser.DeleteSubKeyTree(Amb.ChaveDesinstalar, false); }
        catch { }

        string recado = Idioma.T("inst.removido", Amb.Produto);
        if (presos > 0) recado += "\n" + Idioma.T("inst.presos", presos, local);
        if (eraPadrao) recado += "\n\n" + Idioma.T("inst.eraPadrao");

        if (!silencioso) MessageBox.Show(recado, Amb.Produto, MessageBoxButtons.OK, MessageBoxIcon.Information);

        // Se apaga e apaga a pasta depois que este processo sair.
        try
        {
            string cmd = "/c ping 127.0.0.1 -n 3 > nul & del /f /q \"" + eu + "\" & rd /s /q \"" + local + "\"";
            System.Diagnostics.ProcessStartInfo psi =
                new System.Diagnostics.ProcessStartInfo("cmd.exe", cmd);
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            System.Diagnostics.Process.Start(psi);
        }
        catch { }
    }

    [STAThread]
    static int Main(string[] args)
    {
        string eu = Assembly.GetExecutingAssembly().Location;
        bool desinstalar = LinhaSetup.EhDesinstalar(eu, args);
        bool doTemp = false, silencioso = false;
        foreach (string a in args)
        {
            if (a == "--from-temp") doTemp = true;
            if (LinhaSetup.EhSilencioso(a)) silencioso = true;
        }

        // O instalador respeita a config quando ela ja existe (reinstalacao) e,
        // na primeira vez, segue o idioma e o tema do proprio Windows.
        try
        {
            Cfg c = Cfg.Carregar();
            Idioma.Definir(c.Lingua);
            Cores.Aplicar(c.Tema);
        }
        catch
        {
            Idioma.Definir("auto");
            Cores.Aplicar("sistema");
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (desinstalar)
        {
            string local = null;
            RegistryKey k = Registry.CurrentUser.OpenSubKey(Amb.ChaveDesinstalar);
            if (k != null) local = (string)k.GetValue("InstallLocation", null);

            // Rodando de dentro da pasta que vai sumir: segue do temporario.
            if (!doTemp && !string.IsNullOrEmpty(local) &&
                eu.StartsWith(local, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    string copia = Path.Combine(Path.GetTempPath(), "firawselector-desinstalar.exe");
                    File.Copy(eu, copia, true);
                    System.Diagnostics.Process p = System.Diagnostics.Process.Start(copia,
                        "--uninstall --from-temp" + (silencioso ? " /S" : ""));
                    // Sem janela, quem chamou (o Center) confere o resultado quando
                    // este processo sai: espera a copia terminar antes.
                    if (silencioso && p != null)
                    {
                        p.WaitForExit();
                        return p.ExitCode;
                    }
                    return 0;
                }
                catch { }
            }

            Desinstalar(local, null, silencioso);
            return 0;
        }

        if (silencioso) return InstalaSemJanela(LinhaSetup.Pasta(Environment.CommandLine));

        Application.Run(new Instalador());
        return 0;
    }
}

// Linha de comando do instalador, separada para dar para testar sem instalar
// nada. /S e /D=<pasta> seguem a convencao do NSIS, que e como o Firawynix
// Center instala e atualiza sem janela.
public static class LinhaSetup
{
    public static bool EhSilencioso(string arg)
    {
        return arg == "/S" || arg == "--silent";
    }

    // O desinstalador e uma copia deste arquivo, e o Center chama so
    // "Desinstalar.exe /S" (sem o --uninstall do registro): pelo nome ele sabe
    // que e para remover — senao reinstalaria tudo por cima.
    public static bool EhDesinstalar(string exe, string[] args)
    {
        foreach (string a in args)
            if (a == "--uninstall" || a == "/uninstall") return true;
        return string.Equals(Path.GetFileName(exe), "Desinstalar.exe", StringComparison.OrdinalIgnoreCase);
    }

    // "/D=" vem por ultimo, sem aspas, e pode ter espaco: sai da linha crua, nao
    // do args[] (que ja cortou nos espacos).
    public static string Pasta(string linha)
    {
        if (string.IsNullOrEmpty(linha)) return null;
        int i = linha.IndexOf(" /D=", StringComparison.Ordinal);
        if (i < 0) return null;
        string p = linha.Substring(i + 4).Trim().Trim('"');
        return p.Length > 0 ? p : null;
    }
}

class ArquivoEmUso : Exception
{
    public ArquivoEmUso(Exception dentro) : base(dentro.Message, dentro) { }
}
