using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// ---------------------------------------------------------------------------
// Botao da barra de titulo. Minimizar, maximizar e fechar sao desenhados por
// nos: a janela nao tem barra do Windows, tem a nossa.
// ---------------------------------------------------------------------------
class BotaoBarra : Label
{
    readonly Color hover;
    readonly Color hoverTexto;
    readonly Color normalTexto;
    bool dentro;

    public BotaoBarra(string glifo, string alternativa, Color fundoHover, Color textoHover)
    {
        Cores c = Cores.Atual;
        hover = fundoHover;
        hoverTexto = textoHover;
        normalTexto = c.Texto2;

        string fonte = Amb.FonteGlifos();
        if (fonte != null)
        {
            Text = glifo;
            Font = new Font(fonte, 9.5f);
        }
        else
        {
            // Windows antigo sem a fonte de glifos: texto simples no lugar.
            Text = alternativa;
            Font = new Font("Segoe UI", 9.5f);
        }

        AutoSize = false;
        Size = new Size(46, 38);
        TextAlign = ContentAlignment.MiddleCenter;
        ForeColor = normalTexto;
        BackColor = Color.Transparent;
        Dock = DockStyle.Right;
        Cursor = Cursors.Default;

        MouseEnter += delegate { dentro = true; Pinta(); };
        MouseLeave += delegate { dentro = false; Pinta(); };
    }

    public void TrocaGlifo(string glifo, string alternativa)
    {
        Text = Amb.FonteGlifos() != null ? glifo : alternativa;
    }

    void Pinta()
    {
        BackColor = dentro ? hover : Color.Transparent;
        ForeColor = dentro ? hoverTexto : normalTexto;
    }
}

// ---------------------------------------------------------------------------
// Janela base: sem moldura do Windows, com barra de titulo propria em ciano.
// Arrastar, encaixar nas bordas, maximizar e redimensionar continuam valendo —
// e o "maximizar" respeita a barra de tarefas, que uma janela sem moldura
// cobriria por padrao.
// ---------------------------------------------------------------------------
class JanelaFiraw : Form
{
    protected Cores C { get { return Cores.Atual; } }

    protected Panel Barra;
    protected Panel Corpo;
    Label lblTitulo;
    BotaoBarra bMax;
    readonly bool podeRedimensionar;
    readonly bool podeMaximizar;

    const int Borda = 6;   // faixa de agarrar para redimensionar

    public JanelaFiraw(string titulo, bool redimensionavel, bool maximizavel)
    {
        podeRedimensionar = redimensionavel;
        podeMaximizar = maximizavel;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        // A barra de titulo e nossa, mas o Text da janela continua sendo o nome
        // que o Windows mostra no Alt+Tab e na barra de tarefas. Sem ele o
        // programa aparece la sem nome nenhum.
        Text = titulo;
        BackColor = C.Linha;          // vira a moldura de 1px em volta
        Padding = new Padding(1);
        ForeColor = C.Texto;
        Font = new Font("Segoe UI", 9f);
        KeyPreview = true;
        DoubleBuffered = true;
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
        catch { }

        Corpo = new Panel();
        Corpo.Dock = DockStyle.Fill;
        Corpo.BackColor = C.Fundo;

        Barra = new Panel();
        Barra.Dock = DockStyle.Top;
        Barra.Height = 38;
        Barra.BackColor = C.Painel;
        Barra.Paint += PintaBarra;
        Barra.MouseDown += Arrasta;
        Barra.MouseDoubleClick += AlternaMax;

        PictureBox marca = new PictureBox();
        marca.Size = new Size(18, 18);
        marca.Location = new Point(13, 10);
        marca.SizeMode = PictureBoxSizeMode.Zoom;
        try { if (Icon != null) marca.Image = Icon.ToBitmap(); }
        catch { }
        marca.MouseDown += Arrasta;
        Barra.Controls.Add(marca);

        lblTitulo = new Label();
        lblTitulo.Text = titulo;
        lblTitulo.AutoSize = true;
        lblTitulo.Location = new Point(40, 11);
        lblTitulo.ForeColor = C.Texto;
        lblTitulo.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
        lblTitulo.BackColor = Color.Transparent;
        lblTitulo.MouseDown += Arrasta;
        lblTitulo.MouseDoubleClick += AlternaMax;
        Barra.Controls.Add(lblTitulo);

        // Dock.Right encosta o ULTIMO da fila na direita: a ordem de insercao
        // e a inversa da que aparece na tela.
        BotaoBarra bMin = new BotaoBarra("", "-", C.Linha, C.Texto);
        bMin.Click += delegate { WindowState = FormWindowState.Minimized; };
        Barra.Controls.Add(bMin);

        if (podeMaximizar)
        {
            bMax = new BotaoBarra("", "[ ]", C.Linha, C.Texto);
            bMax.Click += delegate { AlternaMaximizado(); };
            Barra.Controls.Add(bMax);
        }

        BotaoBarra bFechar = new BotaoBarra("", "X", C.Fechar, Color.White);
        bFechar.Click += delegate { Close(); };
        Barra.Controls.Add(bFechar);

        // Fill primeiro, barra depois: o Dock e aplicado do ultimo para o primeiro.
        Controls.Add(Corpo);
        Controls.Add(Barra);
    }

    public string Titulo
    {
        get { return lblTitulo.Text; }
        set { lblTitulo.Text = value; Text = value; }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (C.Escuro) Amb.BarraEscura(Handle);
        Amb.BordaColorida(Handle, C.Acento);
    }

    void PintaBarra(object s, PaintEventArgs e)
    {
        // Risco ciano da marca embaixo da barra, mais forte no canto esquerdo.
        int y = Barra.Height - 1;
        using (Pen p = new Pen(C.Linha))
            e.Graphics.DrawLine(p, 0, y, Barra.Width, y);
        using (Brush b = new LinearGradientBrush(
            new Rectangle(0, y - 1, 220, 2), C.Acento, C.Painel, LinearGradientMode.Horizontal))
            e.Graphics.FillRectangle(b, 0, y - 1, 220, 2);
    }

    void Arrasta(object s, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        // Entregar o arrasto ao Windows e o que mantem o encaixe nas bordas
        // (Aero Snap) funcionando numa janela sem moldura.
        Amb.ReleaseCapture();
        Amb.SendMessage(Handle, Amb.WM_NCLBUTTONDOWN, (IntPtr)Amb.HTCAPTION, IntPtr.Zero);
    }

    void AlternaMax(object s, MouseEventArgs e)
    {
        if (podeMaximizar) AlternaMaximizado();
    }

    protected void AlternaMaximizado()
    {
        if (WindowState == FormWindowState.Maximized)
        {
            WindowState = FormWindowState.Normal;
            if (bMax != null) bMax.TrocaGlifo("", "[ ]");
        }
        else
        {
            // Sem isto a janela sem moldura maximiza por cima da barra de tarefas.
            MaximizedBounds = Screen.FromControl(this).WorkingArea;
            WindowState = FormWindowState.Maximized;
            if (bMax != null) bMax.TrocaGlifo("", "[=]");
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Amb.WM_NCHITTEST && podeRedimensionar &&
            WindowState == FormWindowState.Normal)
        {
            base.WndProc(ref m);
            int lp = m.LParam.ToInt32();
            Point p = PointToClient(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));

            bool esq = p.X <= Borda, dir = p.X >= ClientSize.Width - Borda;
            bool topo = p.Y <= Borda, baixo = p.Y >= ClientSize.Height - Borda;

            if (esq && topo) m.Result = (IntPtr)13;
            else if (dir && topo) m.Result = (IntPtr)14;
            else if (esq && baixo) m.Result = (IntPtr)16;
            else if (dir && baixo) m.Result = (IntPtr)17;
            else if (esq) m.Result = (IntPtr)10;
            else if (dir) m.Result = (IntPtr)11;
            else if (topo) m.Result = (IntPtr)12;
            else if (baixo) m.Result = (IntPtr)15;
            return;
        }
        base.WndProc(ref m);
    }
}

// ---------------------------------------------------------------------------
// Caixa de marcar e botao de opcao desenhados por nos. O controle do Windows
// nao segue tema: no fundo escuro o quadradinho some e ninguem ve o que esta
// ligado.
// ---------------------------------------------------------------------------
class CaixaFiraw : CheckBox
{
    public CaixaFiraw()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
        AutoSize = false;
        Height = 22;
        Cursor = Cursors.Hand;
        // Fonte fixa de proposito: a medida da largura e a pintura tem de usar a
        // mesma. E fundo opaco, nao transparente — num painel com rolagem o
        // fundo transparente e desenhado deslocado e deixa texto fantasma.
        Font = new Font("Segoe UI", 9f);
    }

    public void AjustaLargura()
    {
        Width = TextRenderer.MeasureText(Text, Font).Width + 36;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Cores C = Cores.Atual;
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using (Brush fundo = new SolidBrush(BackColor))
            g.FillRectangle(fundo, ClientRectangle);

        Rectangle cx = new Rectangle(1, (Height - 16) / 2, 16, 16);
        using (Brush b = new SolidBrush(Checked ? C.Acento : C.Campo))
            g.FillRectangle(b, cx);
        using (Pen p = new Pen(Checked ? C.Acento : C.Linha))
            g.DrawRectangle(p, cx);

        if (Checked)
        {
            using (Pen p = new Pen(C.SobreAcento, 2f))
            {
                p.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                p.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                g.DrawLines(p, new Point[] {
                    new Point(cx.X + 4, cx.Y + 8),
                    new Point(cx.X + 7, cx.Y + 11),
                    new Point(cx.X + 12, cx.Y + 5) });
            }
        }

        TextRenderer.DrawText(g, Text, Font,
            new Rectangle(24, 0, Width - 24, Height),
            Enabled ? C.Texto : C.Texto3,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

class OpcaoFiraw : RadioButton
{
    public OpcaoFiraw()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
        AutoSize = false;
        Height = 22;
        Cursor = Cursors.Hand;
        // Fonte fixa de proposito: a medida da largura e a pintura tem de usar a
        // mesma. E fundo opaco, nao transparente — num painel com rolagem o
        // fundo transparente e desenhado deslocado e deixa texto fantasma.
        Font = new Font("Segoe UI", 9f);
    }

    public void AjustaLargura()
    {
        Width = TextRenderer.MeasureText(Text, Font).Width + 36;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Cores C = Cores.Atual;
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using (Brush fundo = new SolidBrush(BackColor))
            g.FillRectangle(fundo, ClientRectangle);

        Rectangle cx = new Rectangle(1, (Height - 16) / 2, 16, 16);
        using (Brush b = new SolidBrush(C.Campo))
            g.FillEllipse(b, cx);
        using (Pen p = new Pen(Checked ? C.Acento : C.Linha))
            g.DrawEllipse(p, cx);
        if (Checked)
        {
            using (Brush b = new SolidBrush(C.Acento))
                g.FillEllipse(b, cx.X + 4, cx.Y + 4, 8, 8);
        }

        TextRenderer.DrawText(g, Text, Font,
            new Rectangle(24, 0, Width - 24, Height),
            Enabled ? C.Texto : C.Texto3,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

// ---------------------------------------------------------------------------
// Fabrica de controles no tema. Evita repetir cor por cor em cada tela.
// ---------------------------------------------------------------------------
static class UI
{
    static Cores C { get { return Cores.Atual; } }

    public static Label Rotulo(string txt, int x, int y)
    {
        Label l = new Label();
        l.Text = txt;
        l.Location = new Point(x, y);
        l.AutoSize = true;
        l.ForeColor = C.Texto;
        l.BackColor = Color.Transparent;
        return l;
    }

    public static Label Fraco(string txt, int x, int y, int largura)
    {
        Label l = Rotulo(txt, x, y);
        l.AutoSize = false;
        l.Width = largura;
        l.Height = 34;
        l.ForeColor = C.Texto2;
        return l;
    }

    public static Label Titulo(string txt, int x, int y)
    {
        Label l = Rotulo(txt, x, y);
        l.Font = new Font("Segoe UI Semibold", 10.5f, FontStyle.Bold);
        l.ForeColor = C.AcentoTexto;
        return l;
    }

    public static TextBox Campo(int x, int y, int largura)
    {
        TextBox t = new TextBox();
        t.Location = new Point(x, y);
        t.Width = largura;
        t.BorderStyle = BorderStyle.FixedSingle;
        t.BackColor = C.Campo;
        t.ForeColor = C.Texto;
        return t;
    }

    public static Button Botao(string txt, int x, int y, int largura, EventHandler clique, bool destaque)
    {
        Button b = new Button();
        b.Text = txt;
        b.Location = new Point(x, y);
        b.Size = new Size(largura, 30);
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = destaque ? C.Acento : C.Linha;
        b.BackColor = destaque ? C.Acento : C.Campo;
        b.ForeColor = destaque ? C.SobreAcento : C.Texto;
        b.FlatAppearance.MouseOverBackColor = destaque ? C.AcentoTexto : C.Linha;
        b.UseVisualStyleBackColor = false;
        if (destaque) b.Font = new Font("Segoe UI Semibold", 9f, FontStyle.Bold);
        if (clique != null) b.Click += clique;
        return b;
    }

    public static CheckBox Caixa(string txt, int x, int y, bool marcada)
    {
        CaixaFiraw c = new CaixaFiraw();
        c.Text = txt;
        c.Location = new Point(x, y);
        c.Checked = marcada;
        c.AjustaLargura();
        return c;
    }

    public static RadioButton Opcao(string txt, int x, int y, bool marcada)
    {
        OpcaoFiraw r = new OpcaoFiraw();
        r.Text = txt;
        r.Location = new Point(x, y);
        r.Checked = marcada;
        r.AjustaLargura();
        return r;
    }

    public static ComboBox Combo(int x, int y, int largura)
    {
        ComboBox cb = new ComboBox();
        cb.Location = new Point(x, y);
        cb.Width = largura;
        cb.DropDownStyle = ComboBoxStyle.DropDownList;
        cb.FlatStyle = FlatStyle.Flat;
        cb.BackColor = C.Campo;
        cb.ForeColor = C.Texto;
        return cb;
    }

    public static NumericUpDown Numero(int x, int y, int largura, int min, int max, int valor)
    {
        NumericUpDown n = new NumericUpDown();
        n.Location = new Point(x, y);
        n.Width = largura;
        n.Minimum = min;
        n.Maximum = max;
        n.Value = Math.Max(min, Math.Min(max, valor));
        n.BorderStyle = BorderStyle.FixedSingle;
        n.BackColor = C.Campo;
        n.ForeColor = C.Texto;
        return n;
    }

    public static Panel Cartao(int x, int y, int largura, int altura)
    {
        Panel p = new Panel();
        p.Location = new Point(x, y);
        p.Size = new Size(largura, altura);
        p.BackColor = C.Painel;
        p.Paint += delegate(object s, PaintEventArgs e)
        {
            Panel alvo = (Panel)s;
            using (Pen pe = new Pen(C.Linha))
                e.Graphics.DrawRectangle(pe, 0, 0, alvo.Width - 1, alvo.Height - 1);
        };
        return p;
    }

    public static ListBox Lista(int x, int y, int largura, int altura, int alturaLinha)
    {
        return Lista(x, y, largura, altura, alturaLinha, null);
    }

    /// <summary>
    /// Lista desenhada por nos: a selecao azul do Windows brigaria com o ciano.
    /// Com <paramref name="pegaIcone"/> cada linha ganha o icone do item — e e
    /// assim que dois perfis do mesmo navegador deixam de ser iguais na tela.
    /// </summary>
    public static ListBox Lista(int x, int y, int largura, int altura, int alturaLinha,
        Converter<object, Icon> pegaIcone)
    {
        ListBox l = new ListBox();
        l.Location = new Point(x, y);
        l.Size = new Size(largura, altura);
        l.BorderStyle = BorderStyle.FixedSingle;
        l.BackColor = C.Campo;
        l.ForeColor = C.Texto;
        l.DrawMode = DrawMode.OwnerDrawFixed;
        l.ItemHeight = alturaLinha;
        l.IntegralHeight = false;
        l.DrawItem += delegate(object s, DrawItemEventArgs e)
        {
            ListBox lb = (ListBox)s;
            if (e.Index < 0) return;

            bool sel = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (Brush fundo = new SolidBrush(sel ? C.Linha : C.Campo))
                e.Graphics.FillRectangle(fundo, e.Bounds);
            if (sel)
            {
                using (Brush faixa = new SolidBrush(C.Acento))
                    e.Graphics.FillRectangle(faixa, e.Bounds.X, e.Bounds.Y, 3, e.Bounds.Height);
            }

            int esquerda = 10;
            if (pegaIcone != null)
            {
                Icon ic = pegaIcone(lb.Items[e.Index]);
                if (ic != null)
                {
                    e.Graphics.DrawIcon(ic,
                        new Rectangle(e.Bounds.X + 10, e.Bounds.Y + (e.Bounds.Height - 20) / 2, 20, 20));
                }
                esquerda = 40;
            }

            string txt = lb.Items[e.Index].ToString();
            using (Brush tinta = new SolidBrush(sel ? C.Texto : C.Texto2))
            using (StringFormat sf = new StringFormat())
            {
                sf.LineAlignment = StringAlignment.Center;
                sf.Trimming = StringTrimming.EllipsisCharacter;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                Rectangle r = new Rectangle(e.Bounds.X + esquerda, e.Bounds.Y,
                    e.Bounds.Width - esquerda - 4, e.Bounds.Height);
                e.Graphics.DrawString(txt, lb.Font, tinta, r, sf);
            }
        };
        return l;
    }

    public static void Aviso(IWin32Window dono, string txt)
    {
        MessageBox.Show(dono, txt, Amb.Produto, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static void Informa(IWin32Window dono, string txt)
    {
        MessageBox.Show(dono, txt, Amb.Produto, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static bool Confirma(IWin32Window dono, string txt)
    {
        return MessageBox.Show(dono, txt, Amb.Produto,
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }
}
