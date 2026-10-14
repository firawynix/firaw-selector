using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web.Script.Serialization;
using Microsoft.Win32;

// Registro opcional do host incluído no MSIX. Só as localizações declaradas
// no manifesto saem da virtualização; não instala outro executável.
static class IntegracaoStore
{
    static readonly string[] Raizes = {
        @"Software\Google\Chrome\NativeMessagingHosts",
        @"Software\Microsoft\Edge\NativeMessagingHosts",
        @"Software\Mozilla\NativeMessagingHosts"
    };
    static string Pasta { get { return Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FirawSelectorStore", "Integracao"); } }
    static string Ativacao { get { return Path.Combine(Pasta, "enabled"); } }
    static string Backup { get { return Path.Combine(Pasta, "previous.json"); } }

    public static bool Disponivel
    {
        get
        {
            int build;
            object valor = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                "CurrentBuildNumber", "0");
            return Amb.EmPacoteMSIX() && int.TryParse(Convert.ToString(valor), out build) && build >= 22000;
        }
    }
    public static bool Ativa { get { return Disponivel && File.Exists(Ativacao); } }

    static string Manifesto(int indice)
    {
        return Path.Combine(Pasta, Amb.HostNativo + (indice == 2 ? ".firefox.json" : ".chromium.json"));
    }

    static string Atual(string raiz)
    {
        using (RegistryKey chave = Registry.CurrentUser.OpenSubKey(raiz + "\\" + Amb.HostNativo))
            return chave == null ? null : chave.GetValue("") as string;
    }

    public static void Conectar()
    {
        if (!Disponivel) throw new InvalidOperationException(Idioma.T("op.ext.win11"));
        Directory.CreateDirectory(Pasta);
        if (!File.Exists(Ativacao))
        {
            Dictionary<string, string> anteriores = new Dictionary<string, string>();
            foreach (string raiz in Raizes) anteriores[raiz] = Atual(raiz);
            File.WriteAllText(Backup, new JavaScriptSerializer().Serialize(anteriores), new UTF8Encoding(false));
        }
        Registra(true);
        File.WriteAllText(Ativacao, "1", Encoding.ASCII);
    }

    static void Registra(bool substituir)
    {
        string alias = File.ReadAllText(Path.Combine(Amb.PastaExe, "native-host-alias.txt")).Trim();
        if (alias != "FirawSelectorNativeHost.exe" && alias != "FirawSelectorNativeHostLab.exe")
            throw new InvalidOperationException("Invalid packaged Native Messaging alias.");
        string host = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", alias);
        if (!File.Exists(host)) throw new FileNotFoundException("Native Messaging host missing.", host);
        string[] ids = { Amb.ExtensaoChromiumId, "pbpiicfdmcemelbgclfbeomgadeakbpm",
            "feegdboplakfllglibmfjcocjognbino", "ekjeojgbkjlhlmgdddpomnabkinmgajf" };
        JavaScriptSerializer json = new JavaScriptSerializer();
        for (int i = 0; i < Raizes.Length; i++)
        {
            string manifesto = Manifesto(i);
            Dictionary<string, object> dados = new Dictionary<string, object>();
            dados["name"] = Amb.HostNativo;
            dados["description"] = "FirawSelector Store Native Messaging Host";
            dados["path"] = host;
            dados["type"] = "stdio";
            if (i == 2) dados["allowed_extensions"] = new string[] { Amb.ExtensaoFirefoxId };
            else dados["allowed_origins"] = Array.ConvertAll(ids, id => "chrome-extension://" + id + "/");
            File.WriteAllText(manifesto, json.Serialize(dados), new UTF8Encoding(false));
            // Uma instalação clássica escolhida depois tem prioridade; não a
            // substitui silenciosamente durante uma atualização do MSIX.
            if (!substituir && !string.Equals(Atual(Raizes[i]), manifesto, StringComparison.OrdinalIgnoreCase)) continue;
            using (RegistryKey chave = Registry.CurrentUser.CreateSubKey(Raizes[i] + "\\" + Amb.HostNativo))
                chave.SetValue("", manifesto, RegistryValueKind.String);
        }
    }

    public static void AtualizaSeAtiva()
    {
        if (!Ativa) return;
        try { Registra(false); }
        catch (Exception ex) { Amb.Registra("Native Messaging: " + ex.Message); }
    }

    public static void Desconectar()
    {
        if (!Disponivel) return;
        Dictionary<string, string> anteriores = File.Exists(Backup)
            ? new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(File.ReadAllText(Backup))
            : new Dictionary<string, string>();
        for (int i = 0; i < Raizes.Length; i++)
        {
            if (!string.Equals(Atual(Raizes[i]), Manifesto(i), StringComparison.OrdinalIgnoreCase)) continue;
            string anterior;
            using (RegistryKey chave = Registry.CurrentUser.OpenSubKey(Raizes[i] + "\\" + Amb.HostNativo, true))
            {
                if (chave == null) continue;
                if (anteriores.TryGetValue(Raizes[i], out anterior) && anterior != null) chave.SetValue("", anterior);
                else chave.DeleteValue("", false);
            }
        }
        if (File.Exists(Ativacao)) File.Delete(Ativacao);
    }
}
