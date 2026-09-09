using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

// Ferramenta de bancada: acha uma janela pelo titulo, aperta um botao dela e
// tira foto. Serve para conferir a tela sem precisar de alguem clicando.
// Nao entra em nenhum dos tres executaveis do produto.
//
//   janela.exe foto  "<parte do titulo>" saida.png
//   janela.exe clica "<parte do titulo>" "<nome do botao>"
//   janela.exe lista
static class Ferramenta
{
    [DllImport("user32.dll")]
    static extern bool EnumWindows(EnumProc cb, IntPtr p);
    delegate bool EnumProc(IntPtr h, IntPtr p);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    static extern int GetWindowTextW(IntPtr h, StringBuilder s, int max);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr h);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr h, out RECT r);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr h, IntPtr depois, int x, int y, int cx, int cy, uint f);

    [StructLayout(LayoutKind.Sequential)]
    struct RECT { public int L, T, R, B; }

    static string Titulo(IntPtr h)
    {
        StringBuilder sb = new StringBuilder(512);
        GetWindowTextW(h, sb, sb.Capacity);
        return sb.ToString();
    }

    static List<IntPtr> Visiveis()
    {
        List<IntPtr> saida = new List<IntPtr>();
        EnumWindows(delegate(IntPtr h, IntPtr p)
        {
            if (IsWindowVisible(h) && Titulo(h).Length > 0) saida.Add(h);
            return true;
        }, IntPtr.Zero);
        return saida;
    }

    static IntPtr Acha(string parte)
    {
        // De tras para frente: a janela aberta por ultimo (a caixa de dialogo) e
        // a que interessa quando o titulo casa com mais de uma.
        List<IntPtr> todas = Visiveis();
        for (int i = todas.Count - 1; i >= 0; i--)
        {
            if (Titulo(todas[i]).IndexOf(parte, StringComparison.OrdinalIgnoreCase) >= 0)
                return todas[i];
        }
        return IntPtr.Zero;
    }

    static int Foto(string parte, string arquivo)
    {
        IntPtr h = Acha(parte);
        if (h == IntPtr.Zero) { Console.WriteLine("nao achei janela: " + parte); return 2; }

        // Sempre-no-topo por um instante: sem isso a foto pega o que estiver por
        // cima da janela, e ja peguei o Explorador no lugar do programa.
        SetWindowPos(h, new IntPtr(-1), 80, 80, 0, 0, 0x41);
        System.Threading.Thread.Sleep(700);

        RECT r;
        GetWindowRect(h, out r);
        int w = r.R - r.L, alt = r.B - r.T;
        if (w <= 0 || alt <= 0) { Console.WriteLine("janela sem tamanho"); return 3; }

        using (Bitmap bm = new Bitmap(w, alt, PixelFormat.Format32bppArgb))
        {
            using (Graphics g = Graphics.FromImage(bm))
                g.CopyFromScreen(r.L, r.T, 0, 0, new Size(w, alt));
            bm.Save(arquivo, ImageFormat.Png);
        }
        SetWindowPos(h, new IntPtr(-2), 80, 80, 0, 0, 0x41);
        Console.WriteLine("ok: " + arquivo + "  (" + w + "x" + alt + ")  " + Titulo(h));
        return 0;
    }

    static int Clica(string parte, string botao)
    {
        IntPtr h = Acha(parte);
        if (h == IntPtr.Zero) { Console.WriteLine("nao achei janela: " + parte); return 2; }

        AutomationElement janela = AutomationElement.FromHandle(h);
        AutomationElement alvo = janela.FindFirst(TreeScope.Descendants,
            new PropertyCondition(AutomationElement.NameProperty, botao));
        if (alvo == null) { Console.WriteLine("nao achei o botao: " + botao); return 3; }

        object padrao;
        if (!alvo.TryGetCurrentPattern(InvokePattern.Pattern, out padrao))
        {
            Console.WriteLine("botao nao aceita clique por automacao");
            return 4;
        }
        ((InvokePattern)padrao).Invoke();
        Console.WriteLine("cliquei: " + botao);
        return 0;
    }

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length == 0) { Console.WriteLine("use: foto | clica | lista"); return 1; }

        switch (args[0])
        {
            case "lista":
                foreach (IntPtr h in Visiveis()) Console.WriteLine(Titulo(h));
                return 0;
            case "foto":
                if (args.Length < 3) { Console.WriteLine("foto <titulo> <saida.png>"); return 1; }
                return Foto(args[1], args[2]);
            case "clica":
                if (args.Length < 3) { Console.WriteLine("clica <titulo> <botao>"); return 1; }
                return Clica(args[1], args[2]);
        }
        Console.WriteLine("comando desconhecido: " + args[0]);
        return 1;
    }
}
