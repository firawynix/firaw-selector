using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

// ---------------------------------------------------------------------------
// Aba da lateral. Ativa = faixa ciano na esquerda e texto forte.
// ---------------------------------------------------------------------------
class AbaLateral : Panel
{
    public readonly string Chave;
    bool ativa, dentro;
    readonly string rotulo;

    Cores C { get { return Cores.Atual; } }

    public AbaLateral(string chave, string texto)
    {
        Chave = chave;
        rotulo = texto;
        DoubleBuffered = true;
        Height = 40;
        Dock = DockStyle.Top;
        Cursor = Cursors.Hand;
        MouseEnter += delegate { dentro = true; Invalidate(); };
        MouseLeave += delegate { dentro = false; Invalidate(); };
    }

    public bool Ativa
    {
        get { return ativa; }
        set { ativa = value; Invalidate(); }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        using (Brush fundo = new SolidBrush(ativa ? C.Fundo : (dentro ? C.Campo : C.Painel)))
            g.FillRectangle(fundo, 0, 0, Width, Height);

        if (ativa)
        {
            using (Brush b = new SolidBrush(C.Acento))
                g.FillRectangle(b, 0, 0, 3, Height);
        }

        using (Brush tinta = new SolidBrush(ativa ? C.Texto : C.Texto2))
        using (Font f = new Font("Segoe UI" + (ativa ? " Semibold" : ""), 9.5f,
                                 ativa ? FontStyle.Bold : FontStyle.Regular))
        using (StringFormat sf = new StringFormat())
        {
            sf.LineAlignment = StringAlignment.Center;
            g.DrawString(rotulo, f, tinta, new Rectangle(18, 0, Width - 22, Height), sf);
        }
    }
}

/// <summary>Uma linha da lista de navegadores: o texto que aparece e o objeto.</summary>
class LinhaNav
{
    public readonly Navegador Nav;
    readonly string texto;

    public LinhaNav(Navegador n, string t) { Nav = n; texto = t; }
    public override string ToString() { return texto; }
}

// ---------------------------------------------------------------------------
// Caixa de um navegador.
// ---------------------------------------------------------------------------
class DialogoNavegador : JanelaFiraw
{
    const int Larg = 660;
    const int Campo = 628;   // 16 a 644

    TextBox txtNome, txtExe, txtArgs, txtPriv, txtIcone, txtRegra;
    NumericUpDown numIcone;
    PictureBox previa;
    ComboBox cmbTipoRegra;
    ListBox lstRegras;
    public Navegador Nav;

    readonly Cfg cfg;
    // Nada e aplicado antes do Salvar: cancelar tem de desfazer tudo.
    readonly List<Regra> minhas = new List<Regra>();
    readonly List<Regra> tiradas = new List<Regra>();

    static readonly string[] TiposRegra = new string[] { "contem", "host", "url", "regex" };

    public DialogoNavegador(Cfg c, Navegador existente)
        : base(existente == null ? Idioma.T("nav.novo") : existente.Nome, false, false)
    {
        ShowInTaskbar = false;
        cfg = c;
        Nav = existente;

        if (existente != null)
        {
            foreach (Regra r in cfg.Regras)
                if (r.NavegadorId == existente.Id) minhas.Add(r);
        }

        int y = 14;
        Corpo.Controls.Add(UI.Rotulo(Idioma.T("nav.nome"), 16, y));
        y += 20;
        txtNome = UI.Campo(16, y, Campo);
        Corpo.Controls.Add(txtNome);
        y += 34;

        Corpo.Controls.Add(UI.Rotulo(Idioma.T("nav.caminho"), 16, y));
        y += 20;
        txtExe = UI.Campo(16, y, 520);
        Corpo.Controls.Add(txtExe);
        Corpo.Controls.Add(UI.Botao(Idioma.T("btn.procurar"), 544, y - 3, 100, EscolheExe, false));
        y += 34;

        // ---- icone ----
        Corpo.Controls.Add(UI.Rotulo(Idioma.T("nav.icone"), 16, y));
        y += 20;

        previa = new PictureBox();
        previa.Location = new Point(16, y - 3);
        previa.Size = new Size(32, 32);
        previa.SizeMode = PictureBoxSizeMode.CenterImage;
        previa.BackColor = C.Campo;
        previa.BorderStyle = BorderStyle.FixedSingle;
        Corpo.Controls.Add(previa);

        txtIcone = UI.Campo(58, y, 292);
        txtIcone.TextChanged += delegate { AtualizaPrevia(); };
        Corpo.Controls.Add(txtIcone);

        // O indice so faz sentido para .exe/.dll, que guardam varios icones.
        numIcone = UI.Numero(358, y - 1, 56, 0, 999, 0);
        numIcone.ValueChanged += delegate { AtualizaPrevia(); };
        Corpo.Controls.Add(numIcone);

        Corpo.Controls.Add(UI.Botao(Idioma.T("btn.procurar"), 422, y - 3, 100, EscolheIcone, false));
        Corpo.Controls.Add(UI.Botao(Idioma.T("btn.limpar"), 530, y - 3, 114, delegate
        {
            txtIcone.Text = "";
            numIcone.Value = 0;
        }, false));
        y += 30;

        Label dicaIcone = UI.Rotulo(Idioma.T("nav.iconeVazio") + "  ·  " + Idioma.T("nav.iconeIndice"), 58, y);
        dicaIcone.ForeColor = C.Texto3;
        dicaIcone.Font = new Font("Segoe UI", 8f);
        Corpo.Controls.Add(dicaIcone);
        y += 26;

        Corpo.Controls.Add(UI.Rotulo(Idioma.T("nav.args"), 16, y));
        y += 20;
        txtArgs = UI.Campo(16, y, Campo);
        Corpo.Controls.Add(txtArgs);
        y += 34;

        Corpo.Controls.Add(UI.Rotulo(Idioma.T("nav.argsPriv"), 16, y));
        y += 20;
        txtPriv = UI.Campo(16, y, Campo);
        Corpo.Controls.Add(txtPriv);
        y += 36;

        // ---- regras deste navegador ----
        // Aqui e o lugar natural de criar a regra: voce esta olhando para o
        // navegador que quer usar. A aba Regras continua existindo porque e la
        // que se ve a ORDEM entre todos — e a ordem decide quem vence.
        Corpo.Controls.Add(UI.Titulo(Idioma.T("nav.regras"), 16, y));
        y += 26;

        cmbTipoRegra = UI.Combo(16, y, 150);
        cmbTipoRegra.Items.AddRange(new object[] {
            Idioma.T("reg.tipo.contem"), Idioma.T("reg.tipo.host"),
            Idioma.T("reg.tipo.url"), Idioma.T("reg.tipo.regex") });
        cmbTipoRegra.SelectedIndex = 0;
        Corpo.Controls.Add(cmbTipoRegra);

        txtRegra = UI.Campo(174, y + 1, 336);
        txtRegra.Font = new Font("Consolas", 9f);
        Corpo.Controls.Add(txtRegra);

        Corpo.Controls.Add(UI.Botao(Idioma.T("nav.regraNova"), 518, y - 2, 126, AcrescentaRegra, false));
        y += 34;

        lstRegras = UI.Lista(16, y, Campo, 116, 24);
        Corpo.Controls.Add(lstRegras);
        y += 122;

        Corpo.Controls.Add(UI.Botao(Idioma.T("btn.remover"), 16, y, 126, TiraRegra, false));
        y += 40;

        Button ok = UI.Botao(Idioma.T("btn.salvar"), 444, y, 100, Salva, true);
        Corpo.Controls.Add(ok);
        Button cancelar = UI.Botao(Idioma.T("btn.cancelar"), 552, y, 92, null, false);
        cancelar.DialogResult = DialogResult.Cancel;
        Corpo.Controls.Add(cancelar);
        CancelButton = cancelar;

        // A altura sai da conta do conteudo, nao de um numero escrito a mao: foi
        // um numero a mao que deixou os botoes cortados pela borda de baixo.
        ClientSize = new Size(Larg, Barra.Height + y + 30 + 16 + 2);

        if (existente != null)
        {
            txtNome.Text = existente.Nome;
            txtExe.Text = existente.Exe;
            txtArgs.Text = existente.Args;
            txtPriv.Text = existente.ArgsPrivado;

            string arq;
            int idx;
            Navegador.SeparaIcone(existente.IconeArquivo, "", out arq, out idx);
            txtIcone.Text = arq;
            numIcone.Value = Math.Max(0, Math.Min(999, idx));
        }
        AtualizaPrevia();
        RecarregaMinhas();
    }

    void RecarregaMinhas()
    {
        lstRegras.Items.Clear();
        if (minhas.Count == 0)
        {
            lstRegras.Items.Add(Idioma.T("nav.regraVazia"));
            return;
        }
        foreach (Regra r in minhas)
        {
            string tipo = Idioma.T("reg.tipo." + r.Tipo);
            lstRegras.Items.Add(r.Padrao + "   ·   " + tipo + (r.Ativo ? "" : "   [ - ]"));
        }
    }

    void AcrescentaRegra(object s, EventArgs e)
    {
        string padrao = txtRegra.Text.Trim();
        if (padrao.Length == 0) { UI.Aviso(this, Idioma.T("reg.vazia")); return; }

        string tipo = TiposRegra[cmbTipoRegra.SelectedIndex];
        if (tipo == "regex")
        {
            string erro;
            if (!Regra.RegexValida(padrao, out erro))
            {
                UI.Aviso(this, Idioma.T("reg.regexRuim", erro));
                return;
            }
        }

        Regra r = new Regra();
        r.Padrao = padrao;
        r.Tipo = tipo;
        r.Ativo = true;
        minhas.Add(r);
        txtRegra.Text = "";
        RecarregaMinhas();
    }

    void TiraRegra(object s, EventArgs e)
    {
        int i = lstRegras.SelectedIndex;
        if (i < 0 || i >= minhas.Count) return;
        // Se ja estava no config, marca para sair de la no Salvar.
        if (cfg.Regras.Contains(minhas[i])) tiradas.Add(minhas[i]);
        minhas.RemoveAt(i);
        RecarregaMinhas();
    }

    /// <summary>Junta caminho e indice de volta no formato do config.</summary>
    string ValorIcone()
    {
        string arq = txtIcone.Text.Trim();
        if (arq.Length == 0) return "";
        int idx = (int)numIcone.Value;
        return idx > 0 ? arq + "," + idx : arq;
    }

    void AtualizaPrevia()
    {
        Icon ic = null;
        string arq = txtIcone.Text.Trim();
        if (arq.Length == 0) arq = txtExe.Text.Trim();
        if (arq.Length > 0) ic = Amb.IconeDoArquivo(arq, (int)numIcone.Value, 32);

        if (previa.Image != null)
        {
            previa.Image.Dispose();
            previa.Image = null;
        }
        try { if (ic != null) previa.Image = ic.ToBitmap(); }
        catch { }
    }

    void EscolheExe(object s, EventArgs e)
    {
        using (OpenFileDialog d = new OpenFileDialog())
        {
            d.Filter = "Programas (*.exe)|*.exe";
            if (d.ShowDialog(this) != DialogResult.OK) return;
            txtExe.Text = d.FileName;
            if (txtNome.Text.Trim().Length == 0)
                txtNome.Text = Path.GetFileNameWithoutExtension(d.FileName);
            if (txtPriv.Text.Trim().Length == 0)
                txtPriv.Text = Navegador.PrivadoPadrao(d.FileName);
            AtualizaPrevia();
        }
    }

    void EscolheIcone(object s, EventArgs e)
    {
        using (OpenFileDialog d = new OpenFileDialog())
        {
            d.Filter = Idioma.T("nav.iconeFiltro");
            if (d.ShowDialog(this) != DialogResult.OK) return;

            if (Amb.IconeDoArquivo(d.FileName, 0, 32) == null)
            {
                UI.Aviso(this, Idioma.T("nav.iconeRuim"));
                return;
            }
            txtIcone.Text = d.FileName;
            numIcone.Value = 0;
        }
    }

    void Salva(object s, EventArgs e)
    {
        string exe = txtExe.Text.Trim();
        if (exe.Length == 0 || !File.Exists(exe))
        {
            UI.Aviso(this, Idioma.T("nav.semExe"));
            return;
        }
        if (Nav == null)
        {
            Nav = new Navegador();
            Nav.Manual = true;
            Nav.Id = Path.GetFileNameWithoutExtension(exe).ToLowerInvariant() + "@" +
                     DateTime.Now.ToString("HHmmss");
        }
        Nav.Nome = txtNome.Text.Trim().Length > 0
            ? txtNome.Text.Trim() : Path.GetFileNameWithoutExtension(exe);
        Nav.Exe = exe;
        Nav.Args = txtArgs.Text.Trim();
        Nav.ArgsPrivado = txtPriv.Text.Trim();
        Nav.Familia = Navegador.FamiliaDoExe(exe);
        Nav.IconeArquivo = ValorIcone();
        Nav.EsqueceIcone();   // sem isto a lista continuaria mostrando o antigo

        // Regras: so agora vao para o config, com o Id ja definido (navegador
        // novo so ganha Id aqui em cima).
        foreach (Regra r in tiradas) cfg.Regras.Remove(r);
        foreach (Regra r in minhas)
        {
            r.NavegadorId = Nav.Id;
            if (!cfg.Regras.Contains(r)) cfg.Regras.Add(r);
        }

        DialogResult = DialogResult.OK;
        Close();
    }
}

// ---------------------------------------------------------------------------
// Caixa de uma regra.
// ---------------------------------------------------------------------------
class DialogoRegra : JanelaFiraw
{
    readonly Cfg cfg;
    TextBox txtPadrao;
    ComboBox cmbTipo, cmbNav;
    CheckBox chkPriv, chkAtiva;
    public Regra Reg;

    // "contem" vem primeiro de proposito: e o que se quer em quase toda regra,
    // e ser o primeiro faz dele o padrao de uma regra nova.
    static readonly string[] Tipos = new string[] { "contem", "host", "url", "regex" };

    public DialogoRegra(Cfg c, Regra existente)
        : base(existente == null ? Idioma.T("reg.nova") : existente.Padrao, false, false)
    {
        cfg = c;
        Reg = existente;
        ShowInTaskbar = false;

        int y = 14;
        Corpo.Controls.Add(UI.Rotulo(Idioma.T("reg.padraoTxt"), 16, y));
        y += 20;
        txtPadrao = UI.Campo(16, y, 588);
        txtPadrao.Font = new Font("Consolas", 9.5f);
        Corpo.Controls.Add(txtPadrao);
        y += 30;

        Label ajuda = UI.Rotulo(Idioma.T("reg.ajuda"), 16, y);
        ajuda.MaximumSize = new Size(588, 40);
        ajuda.ForeColor = C.Texto3;
        ajuda.Font = new Font("Segoe UI", 8f);
        Corpo.Controls.Add(ajuda);
        y += 44;

        Corpo.Controls.Add(UI.Rotulo(Idioma.T("reg.tipo"), 16, y));
        Corpo.Controls.Add(UI.Rotulo(Idioma.T("reg.navegador"), 200, y));
        y += 20;

        cmbTipo = UI.Combo(16, y, 170);
        cmbTipo.Items.AddRange(new object[] {
            Idioma.T("reg.tipo.contem"), Idioma.T("reg.tipo.host"),
            Idioma.T("reg.tipo.url"), Idioma.T("reg.tipo.regex") });
        cmbTipo.SelectedIndex = 0;
        Corpo.Controls.Add(cmbTipo);

        cmbNav = UI.Combo(200, y, 404);
        foreach (Navegador n in cfg.Navegadores) cmbNav.Items.Add(n);
        if (cmbNav.Items.Count > 0) cmbNav.SelectedIndex = 0;
        Corpo.Controls.Add(cmbNav);
        y += 38;

        chkPriv = UI.Caixa(Idioma.T("esc.privativo"), 16, y, false);
        Corpo.Controls.Add(chkPriv);
        chkAtiva = UI.Caixa(Idioma.T("reg.ativa"), 200, y, true);
        Corpo.Controls.Add(chkAtiva);
        y += 38;

        Corpo.Controls.Add(UI.Botao(Idioma.T("btn.salvar"), 404, y, 100, Salva, true));
        Button cancelar = UI.Botao(Idioma.T("btn.cancelar"), 512, y, 92, null, false);
        cancelar.DialogResult = DialogResult.Cancel;
        Corpo.Controls.Add(cancelar);
        CancelButton = cancelar;

        // Altura pela conta do conteudo (veja o comentario em DialogoNavegador).
        ClientSize = new Size(620, Barra.Height + y + 30 + 16 + 2);

        if (existente != null)
        {
            txtPadrao.Text = existente.Padrao;
            cmbTipo.SelectedIndex = Math.Max(0, Array.IndexOf(Tipos, existente.Tipo));
            chkPriv.Checked = existente.Privado;
            chkAtiva.Checked = existente.Ativo;
            for (int i = 0; i < cmbNav.Items.Count; i++)
            {
                if (((Navegador)cmbNav.Items[i]).Id == existente.NavegadorId)
                {
                    cmbNav.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    void Salva(object s, EventArgs e)
    {
        string padrao = txtPadrao.Text.Trim();
        if (padrao.Length == 0) { UI.Aviso(this, Idioma.T("reg.vazia")); return; }
        if (cmbNav.SelectedItem == null) { UI.Aviso(this, Idioma.T("reg.semNavegador")); return; }

        string tipo = Tipos[cmbTipo.SelectedIndex];
        if (tipo == "regex")
        {
            string erro;
            if (!Regra.RegexValida(padrao, out erro))
            {
                UI.Aviso(this, Idioma.T("reg.regexRuim", erro));
                return;
            }
        }

        if (Reg == null) Reg = new Regra();
        Reg.Padrao = padrao;
        Reg.Tipo = tipo;
        Reg.NavegadorId = ((Navegador)cmbNav.SelectedItem).Id;
        Reg.Privado = chkPriv.Checked;
        Reg.Ativo = chkAtiva.Checked;
        DialogResult = DialogResult.OK;
        Close();
    }
}

// ---------------------------------------------------------------------------
// O configurador.
// ---------------------------------------------------------------------------
class Studio : JanelaFiraw
{
    Cfg cfg;
    Panel conteudo;
    readonly List<AbaLateral> abas = new List<AbaLateral>();
    readonly Dictionary<string, Panel> paginas = new Dictionary<string, Panel>();

    ListBox lstNav, lstRegras;
    Panel faixa;
    Label lblContaNav, lblResultado, lblEstadoWin;
    TextBox txtTeste, txtLog;
    string abaInicial;

    /// <summary>Trocar idioma ou tema reconstroi a janela: a paleta e as
    /// legendas sao lidas na montagem, nao a cada pintura.</summary>
    public bool Reabrir;
    public string AbaAoVoltar = "navegadores";

    public Studio(Cfg c, string aba)
        : base(Amb.Produto + " " + Amb.Versao + "  -  " + Idioma.T("app.tagline"), true, true)
    {
        cfg = c;
        abaInicial = aba;
        ClientSize = new Size(940, 660);
        MinimumSize = new Size(820, 560);

        Panel lateral = new Panel();
        lateral.Dock = DockStyle.Left;
        lateral.Width = 190;
        lateral.BackColor = C.Painel;

        conteudo = new Panel();
        conteudo.Dock = DockStyle.Fill;
        conteudo.BackColor = C.Fundo;
        conteudo.Padding = new Padding(18, 14, 18, 14);

        Corpo.Controls.Add(conteudo);
        Corpo.Controls.Add(lateral);
        Corpo.Controls.Add(FaixaPadrao());   // por ultimo: e o primeiro a encostar no topo

        paginas["navegadores"] = PaginaNavegadores();
        paginas["regras"] = PaginaRegras();
        paginas["opcoes"] = PaginaOpcoes();
        paginas["registro"] = PaginaRegistro();
        paginas["sobre"] = PaginaSobre();

        foreach (KeyValuePair<string, Panel> kv in paginas)
        {
            kv.Value.Dock = DockStyle.Fill;
            kv.Value.Visible = false;
            conteudo.Controls.Add(kv.Value);
        }

        // Dock.Top empilha de cima para baixo na ordem inversa da insercao.
        string[] ordem = new string[] { "sobre", "registro", "opcoes", "regras", "navegadores" };
        string[] rotulos = new string[] {
            Idioma.T("aba.sobre"), Idioma.T("aba.registro"), Idioma.T("aba.opcoes"),
            Idioma.T("aba.regras"), Idioma.T("aba.navegadores") };

        for (int i = 0; i < ordem.Length; i++)
        {
            AbaLateral a = new AbaLateral(ordem[i], rotulos[i]);
            a.Click += delegate(object s, EventArgs e) { Mostra(((AbaLateral)s).Chave); };
            lateral.Controls.Add(a);
            abas.Add(a);
        }

        Label rodape = new Label();
        rodape.Dock = DockStyle.Bottom;
        rodape.Height = 46;
        rodape.ForeColor = C.Texto3;
        rodape.Font = new Font("Segoe UI", 8f);
        rodape.Padding = new Padding(18, 6, 8, 6);
        rodape.Text = Amb.Marca + "\n" + Amb.Site.Replace("https://", "");
        lateral.Controls.Add(rodape);

        Mostra(abaInicial);
        FormClosing += delegate { Guarda(); };

        // Avisa o que a leitura consertou — mudanca silenciosa em regra que a
        // pessoa escreveu seria pior do que a regra quebrada.
        if (cfg.RegrasMigradas > 0)
        {
            int quantas = cfg.RegrasMigradas;
            cfg.RegrasMigradas = 0;
            Shown += delegate { UI.Informa(this, Idioma.T("reg.migradas", quantas)); };
        }
    }

    /// <summary>
    /// Faixa de aviso no topo enquanto o FirawSelector nao for o padrao — sem
    /// isso a pessoa instala, fecha e nunca descobre por que os links continuam
    /// abrindo no navegador de antes. Some sozinha quando vira padrao.
    /// </summary>
    Panel FaixaPadrao()
    {
        Panel f = new Panel();
        f.Dock = DockStyle.Top;
        f.Height = 46;
        f.BackColor = C.Painel;
        f.Visible = !Registrar.EhPadrao();
        faixa = f;

        f.Paint += delegate(object s, PaintEventArgs e)
        {
            using (Brush b = new SolidBrush(C.Aviso))
                e.Graphics.FillRectangle(b, 0, 0, 3, f.Height);
            using (Pen p = new Pen(C.Linha))
                e.Graphics.DrawLine(p, 0, f.Height - 1, f.Width, f.Height - 1);
        };

        Label texto = UI.Rotulo(Idioma.T("faixa.naoPadrao"), 18, 14);
        texto.ForeColor = C.Aviso;
        f.Controls.Add(texto);

        Button b2 = UI.Botao(Idioma.T("faixa.tornarPadrao"), 0, 8, 160, delegate
        {
            Registrar.Registra(ExeDoMotor());
            Registrar.AbrePadroesWindows();
        }, true);
        f.Controls.Add(b2);
        // Ancora nao serve aqui: ela guarda a distancia da borda MEDIDA quando o
        // painel ainda tem o tamanho de fabrica, e o botao vai parar fora da
        // tela quando a faixa cresce. Reposicionar no Resize e exato.
        f.Resize += delegate { b2.Left = Math.Max(8, f.Width - b2.Width - 18); };
        b2.Left = Math.Max(8, f.Width - b2.Width - 18);

        return f;
    }

    void Mostra(string chave)
    {
        foreach (AbaLateral a in abas) a.Ativa = a.Chave == chave;
        foreach (KeyValuePair<string, Panel> kv in paginas) kv.Value.Visible = kv.Key == chave;
        AbaAoVoltar = chave;
        if (chave == "registro") CarregaLog();
    }

    void Guarda()
    {
        try { cfg.Salvar(); }
        catch (Exception ex) { Amb.Registra("falha ao salvar: " + ex.Message); }
    }

    // ================= navegadores =================

    /// <summary>
    /// As paginas de lista sao montadas com Dock, nao com Anchor: a lista
    /// ancorada nas quatro bordas crescia por cima da coluna de botoes assim
    /// que a janela ficava maior que o tamanho de projeto.
    /// </summary>
    static Panel Faixa(DockStyle lado, int tamanho)
    {
        Panel f = new Panel();
        f.Dock = lado;
        if (lado == DockStyle.Top || lado == DockStyle.Bottom) f.Height = tamanho;
        else f.Width = tamanho;
        return f;
    }

    Panel PaginaNavegadores()
    {
        Panel p = new Panel();

        Panel topo = Faixa(DockStyle.Top, 52);
        topo.Controls.Add(UI.Titulo(Idioma.T("nav.titulo"), 0, 0));
        lblContaNav = UI.Rotulo("", 0, 26);
        lblContaNav.ForeColor = C.Texto2;
        topo.Controls.Add(lblContaNav);

        Panel direita = Faixa(DockStyle.Right, 216);
        int x = 16, y = 0;
        Button[] bs = new Button[] {
            UI.Botao(Idioma.T("nav.definirPadrao"), x, y, 200, delegate { DefinePadrao(true); }, true),
            UI.Botao(Idioma.T("nav.definirSegundo"), x, y + 36, 200, delegate { DefinePadrao(false); }, false),
            UI.Botao(Idioma.T("nav.ocultar"), x, y + 72, 200, delegate { AlternaOculto(); }, false),
            UI.Botao(Idioma.T("btn.editar"), x, y + 116, 200, delegate { EditaNavegador(); }, false),
            UI.Botao(Idioma.T("nav.manual"), x, y + 152, 200, delegate { NovoNavegador(); }, false),
            UI.Botao(Idioma.T("btn.remover"), x, y + 188, 200, delegate { RemoveNavegador(); }, false),
            UI.Botao(Idioma.T("btn.subir"), x, y + 232, 96, delegate { MoveNavegador(-1); }, false),
            UI.Botao(Idioma.T("btn.descer"), x + 104, y + 232, 96, delegate { MoveNavegador(1); }, false),
            UI.Botao(Idioma.T("nav.reprocurar"), x, y + 276, 200, delegate { Reprocura(false); }, false),
            UI.Botao(Idioma.T("nav.perfis"), x, y + 312, 200, delegate { Reprocura(true); }, false),
            UI.Botao(Idioma.T("nav.abrirTeste"), x, y + 356, 200, delegate { TestaNavegador(); }, false)
        };
        foreach (Button b in bs) direita.Controls.Add(b);

        lstNav = UI.Lista(0, 0, 100, 100, 40, delegate(object item)
        {
            LinhaNav l = item as LinhaNav;
            return l == null ? null : l.Nav.Icone(20);
        });
        lstNav.Dock = DockStyle.Fill;
        lstNav.DoubleClick += delegate { EditaNavegador(); };

        p.Controls.Add(lstNav);
        p.Controls.Add(direita);
        p.Controls.Add(topo);

        RecarregaNavegadores();
        return p;
    }

    void RecarregaNavegadores()
    {
        int sel = lstNav.SelectedIndex;
        lstNav.Items.Clear();

        int perfis = 0;
        foreach (Navegador n in cfg.Navegadores)
        {
            List<string> marcas = new List<string>();
            if (n.Id == cfg.PadraoId) marcas.Add(Idioma.T("nav.padrao"));
            if (n.Id == cfg.SegundoId) marcas.Add(Idioma.T("nav.segundo"));
            if (n.Oculto) marcas.Add(Idioma.T("nav.oculto"));
            if (!n.Existe) marcas.Add("?");
            if (n.Args.Length > 0) perfis++;

            string etiqueta = marcas.Count > 0 ? "   [" + string.Join(", ", marcas.ToArray()) + "]" : "";
            string detalhe = n.Args.Length > 0 ? n.Args : n.Exe;
            lstNav.Items.Add(new LinhaNav(n, n.Nome + etiqueta + "        " + detalhe));
        }

        if (sel >= 0 && sel < lstNav.Items.Count) lstNav.SelectedIndex = sel;
        else if (lstNav.Items.Count > 0) lstNav.SelectedIndex = 0;

        lblContaNav.Text = Idioma.T("nav.achados", cfg.Navegadores.Count, perfis);
    }

    Navegador NavSelecionado()
    {
        LinhaNav l = lstNav.SelectedItem as LinhaNav;
        return l == null ? null : l.Nav;
    }

    void DefinePadrao(bool primeiro)
    {
        Navegador n = NavSelecionado();
        if (n == null) return;
        if (primeiro) cfg.PadraoId = n.Id;
        else cfg.SegundoId = n.Id;
        Guarda();
        RecarregaNavegadores();
    }

    void AlternaOculto()
    {
        Navegador n = NavSelecionado();
        if (n == null) return;
        n.Oculto = !n.Oculto;
        Guarda();
        RecarregaNavegadores();
    }

    void EditaNavegador()
    {
        Navegador n = NavSelecionado();
        if (n == null) return;
        using (DialogoNavegador d = new DialogoNavegador(cfg, n))
        {
            if (d.ShowDialog(this) == DialogResult.OK)
            {
                Guarda();
                RecarregaNavegadores();
            }
        }
    }

    void NovoNavegador()
    {
        using (DialogoNavegador d = new DialogoNavegador(cfg, null))
        {
            if (d.ShowDialog(this) == DialogResult.OK && d.Nav != null)
            {
                cfg.Navegadores.Add(d.Nav);
                Guarda();
                RecarregaNavegadores();
            }
        }
    }

    void RemoveNavegador()
    {
        Navegador n = NavSelecionado();
        if (n == null) return;
        cfg.Navegadores.Remove(n);
        if (cfg.PadraoId == n.Id) cfg.PadraoId = "";
        if (cfg.SegundoId == n.Id) cfg.SegundoId = "";
        Guarda();
        RecarregaNavegadores();
    }

    /// <summary>
    /// A ordem da lista e a ordem dos numeros 1-9 na janela de escolha. Por isso
    /// ela precisa ser arrastavel: o navegador do dia a dia tem de ser o 1.
    /// </summary>
    void MoveNavegador(int passo)
    {
        Navegador n = NavSelecionado();
        if (n == null) return;

        int i = cfg.Navegadores.IndexOf(n);
        int j = i + passo;
        if (i < 0 || j < 0 || j >= cfg.Navegadores.Count) return;

        cfg.Navegadores[i] = cfg.Navegadores[j];
        cfg.Navegadores[j] = n;
        Guarda();
        RecarregaNavegadores();
        lstNav.SelectedIndex = j;
    }

    void Reprocura(bool comPerfis)
    {
        List<Navegador> achados = Navegador.Detecta();
        if (comPerfis) achados.AddRange(Navegador.DetectaPerfis(achados));

        int novos = 0;
        foreach (Navegador n in achados)
        {
            bool ja = false;
            foreach (Navegador j in cfg.Navegadores)
            {
                if (string.Equals(j.Exe, n.Exe, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(j.Args, n.Args, StringComparison.OrdinalIgnoreCase)) { ja = true; break; }
            }
            if (ja) continue;
            cfg.Navegadores.Add(n);
            novos++;
        }
        Guarda();
        RecarregaNavegadores();

        if (comPerfis)
            UI.Informa(this, novos > 0
                ? Idioma.T("nav.perfisAchados", novos) : Idioma.T("nav.perfisNenhum"));
    }

    void TestaNavegador()
    {
        Navegador n = NavSelecionado();
        if (n == null || !n.Existe) return;
        try { n.Abrir(Amb.Site, false); }
        catch (Exception ex) { UI.Aviso(this, ex.Message); }
    }

    // ================= regras =================

    Panel PaginaRegras()
    {
        Panel p = new Panel();

        Panel topo = Faixa(DockStyle.Top, 52);
        topo.Controls.Add(UI.Titulo(Idioma.T("reg.titulo"), 0, 0));
        Label ajuda = UI.Rotulo(Idioma.T("reg.ajuda"), 0, 26);
        ajuda.ForeColor = C.Texto3;
        ajuda.Font = new Font("Segoe UI", 8f);
        topo.Controls.Add(ajuda);

        Panel direita = Faixa(DockStyle.Right, 216);
        int x = 16, y = 0;
        Button[] bs = new Button[] {
            UI.Botao(Idioma.T("reg.nova"), x, y, 200, delegate { NovaRegra(); }, true),
            UI.Botao(Idioma.T("btn.editar"), x, y + 36, 200, delegate { EditaRegra(); }, false),
            UI.Botao(Idioma.T("btn.remover"), x, y + 72, 200, delegate { RemoveRegra(); }, false),
            UI.Botao(Idioma.T("btn.subir"), x, y + 116, 96, delegate { MoveRegra(-1); }, false),
            UI.Botao(Idioma.T("btn.descer"), x + 104, y + 116, 96, delegate { MoveRegra(1); }, false)
        };
        foreach (Button b in bs) direita.Controls.Add(b);

        Panel baixo = Faixa(DockStyle.Bottom, 88);
        baixo.Controls.Add(UI.Rotulo(Idioma.T("reg.testar"), 0, 8));
        txtTeste = UI.Campo(0, 30, 400);
        txtTeste.Text = "https://exemplo.com.br/pagina";
        txtTeste.Font = new Font("Consolas", 9f);
        baixo.Controls.Add(txtTeste);
        baixo.Controls.Add(UI.Botao(Idioma.T("btn.testar"), 408, 27, 100, delegate { Testa(); }, false));
        lblResultado = UI.Rotulo("", 0, 62);
        lblResultado.ForeColor = C.AcentoTexto;
        lblResultado.AutoSize = false;
        lblResultado.Size = new Size(600, 20);
        baixo.Controls.Add(lblResultado);

        lstRegras = UI.Lista(0, 0, 100, 100, 34);
        lstRegras.Dock = DockStyle.Fill;
        lstRegras.DoubleClick += delegate { EditaRegra(); };

        p.Controls.Add(lstRegras);
        p.Controls.Add(direita);
        p.Controls.Add(baixo);
        p.Controls.Add(topo);

        RecarregaRegras();
        return p;
    }

    string TextoTipo(string tipo)
    {
        if (tipo == "contem") return Idioma.T("reg.tipo.contem");
        if (tipo == "url") return Idioma.T("reg.tipo.url");
        if (tipo == "regex") return Idioma.T("reg.tipo.regex");
        return Idioma.T("reg.tipo.host");
    }

    void RecarregaRegras()
    {
        int sel = lstRegras.SelectedIndex;
        lstRegras.Items.Clear();
        for (int i = 0; i < cfg.Regras.Count; i++)
        {
            Regra r = cfg.Regras[i];
            Navegador n = cfg.Por(r.NavegadorId);
            string nome = n != null ? n.Nome : r.NavegadorId + " (?)";
            string extra = "";
            if (r.Privado) extra += "  [" + Idioma.T("esc.privativo") + "]";
            if (!r.Ativo) extra += "  [ - ]";
            lstRegras.Items.Add((i + 1).ToString("00") + "   " + r.Padrao +
                "   ·   " + TextoTipo(r.Tipo) + "   ->   " + nome + extra);
        }
        if (sel >= 0 && sel < lstRegras.Items.Count) lstRegras.SelectedIndex = sel;
        else if (lstRegras.Items.Count > 0) lstRegras.SelectedIndex = 0;
    }

    void NovaRegra()
    {
        using (DialogoRegra d = new DialogoRegra(cfg, null))
        {
            if (d.ShowDialog(this) == DialogResult.OK && d.Reg != null)
            {
                cfg.Regras.Add(d.Reg);
                Guarda();
                RecarregaRegras();
            }
        }
    }

    void EditaRegra()
    {
        int i = lstRegras.SelectedIndex;
        if (i < 0 || i >= cfg.Regras.Count) return;
        using (DialogoRegra d = new DialogoRegra(cfg, cfg.Regras[i]))
        {
            if (d.ShowDialog(this) == DialogResult.OK)
            {
                Guarda();
                RecarregaRegras();
            }
        }
    }

    void RemoveRegra()
    {
        int i = lstRegras.SelectedIndex;
        if (i < 0 || i >= cfg.Regras.Count) return;
        cfg.Regras.RemoveAt(i);
        Guarda();
        RecarregaRegras();
    }

    void MoveRegra(int passo)
    {
        int i = lstRegras.SelectedIndex;
        int j = i + passo;
        if (i < 0 || j < 0 || j >= cfg.Regras.Count) return;
        Regra r = cfg.Regras[i];
        cfg.Regras[i] = cfg.Regras[j];
        cfg.Regras[j] = r;
        RecarregaRegras();
        lstRegras.SelectedIndex = j;
        Guarda();
    }

    void Testa()
    {
        string url = Motor.Normaliza(txtTeste.Text.Trim());
        if (url.Length == 0) return;

        bool priv;
        string motivo;
        Navegador n = Motor.Escolhe(cfg, url, false, out priv, out motivo);

        if (n == null)
        {
            Navegador padrao = cfg.Padrao;
            lblResultado.ForeColor = C.Aviso;
            lblResultado.Text = Idioma.T("reg.semCasar") + "  ->  " +
                (cfg.Modo == "regras" && padrao != null
                    ? padrao.Nome + " (" + Idioma.T("por.padrao") + ")"
                    : Idioma.T("esc.titulo"));
            return;
        }
        lblResultado.ForeColor = C.Bom;
        lblResultado.Text = Idioma.T("reg.resultado",
            n.Nome + (priv ? " (" + Idioma.T("esc.privativo") + ")" : ""), motivo);
    }

    // ================= opcoes =================

    Panel PaginaOpcoes()
    {
        Panel p = new Panel();
        p.AutoScroll = true;
        int y = 0;

        p.Controls.Add(UI.Titulo(Idioma.T("op.quando"), 0, y));
        y += 28;

        RadioButton rPerguntar = UI.Opcao(Idioma.T("op.modo.perguntar"), 4, y, cfg.Modo != "regras");
        p.Controls.Add(rPerguntar);
        y += 24;
        RadioButton rRegras = UI.Opcao(Idioma.T("op.modo.regras"), 4, y, cfg.Modo == "regras");
        p.Controls.Add(rRegras);
        y += 32;

        rPerguntar.CheckedChanged += delegate
        {
            cfg.Modo = rPerguntar.Checked ? "perguntar" : "regras";
            Guarda();
        };

        p.Controls.Add(UI.Rotulo(Idioma.T("op.tempo"), 4, y + 5));
        NumericUpDown numTempo = UI.Numero(430, y, 70, 0, 120, cfg.Tempo);
        numTempo.ValueChanged += delegate { cfg.Tempo = (int)numTempo.Value; Guarda(); };
        p.Controls.Add(numTempo);
        y += 38;

        CheckBox cLembrar = UI.Caixa(Idioma.T("op.lembrar"), 4, y, cfg.Lembrar);
        cLembrar.CheckedChanged += delegate { cfg.Lembrar = cLembrar.Checked; Guarda(); };
        p.Controls.Add(cLembrar);

        Button bEsquecer = UI.Botao(Idioma.T("op.esquecer", cfg.Lembrados.Count), 430, y - 5, 260, null, false);
        bEsquecer.Click += delegate
        {
            cfg.Lembrados.Clear();
            Guarda();
            bEsquecer.Text = Idioma.T("op.esquecer", 0);
            UI.Informa(this, Idioma.T("op.esquecido"));
        };
        p.Controls.Add(bEsquecer);
        y += 28;

        CheckBox cShift = UI.Caixa(Idioma.T("op.shift"), 4, y, cfg.ForcarShift);
        cShift.CheckedChanged += delegate { cfg.ForcarShift = cShift.Checked; Guarda(); };
        p.Controls.Add(cShift);
        y += 26;

        CheckBox cLimpar = UI.Caixa(Idioma.T("op.limpar"), 4, y, cfg.LimparRastreio);
        cLimpar.CheckedChanged += delegate { cfg.LimparRastreio = cLimpar.Checked; Guarda(); };
        p.Controls.Add(cLimpar);
        y += 26;

        CheckBox cExpandir = UI.Caixa(Idioma.T("op.expandir"), 4, y, cfg.ExpandirCurtas);
        cExpandir.CheckedChanged += delegate { cfg.ExpandirCurtas = cExpandir.Checked; Guarda(); };
        p.Controls.Add(cExpandir);
        y += 26;

        CheckBox cReal = UI.Caixa(Idioma.T("op.abrirReal"), 4, y, cfg.AbrirReal);
        cReal.CheckedChanged += delegate { cfg.AbrirReal = cReal.Checked; Guarda(); };
        p.Controls.Add(cReal);
        y += 22;

        Label avisoReal = UI.Rotulo(Idioma.T("op.abrirRealAviso"), 28, y);
        avisoReal.MaximumSize = new Size(660, 34);
        avisoReal.ForeColor = C.Texto3;
        avisoReal.Font = new Font("Segoe UI", 8f);
        p.Controls.Add(avisoReal);
        y += 34;

        CheckBox cLog = UI.Caixa(Idioma.T("op.log"), 4, y, cfg.Log);
        cLog.CheckedChanged += delegate { cfg.Log = cLog.Checked; Guarda(); };
        p.Controls.Add(cLog);
        y += 26;

        CheckBox cCompacto = UI.Caixa(Idioma.T("op.compacto"), 4, y, cfg.Compacto);
        p.Controls.Add(cCompacto);
        y += 26;

        // Recuada e presa ao modo enxuto: tirar a moldura so existe la dentro.
        CheckBox cFantasma = UI.Caixa(Idioma.T("op.fantasma"), 28, y, cfg.Fantasma);
        cFantasma.Enabled = cfg.Compacto;
        cFantasma.CheckedChanged += delegate { cfg.Fantasma = cFantasma.Checked; Guarda(); };
        p.Controls.Add(cFantasma);

        cCompacto.CheckedChanged += delegate
        {
            cfg.Compacto = cCompacto.Checked;
            cFantasma.Enabled = cCompacto.Checked;
            cFantasma.Invalidate();
            Guarda();
        };
        y += 40;

        // ---- idioma e tema ----
        p.Controls.Add(UI.Rotulo(Idioma.T("op.idioma"), 4, y + 5));
        ComboBox cmbIdioma = UI.Combo(120, y, 220);
        cmbIdioma.Items.AddRange(new object[] { Idioma.T("op.idioma.auto"), "Portugues (BR)", "English" });
        cmbIdioma.SelectedIndex = cfg.Lingua == "pt" ? 1 : (cfg.Lingua == "en" ? 2 : 0);
        p.Controls.Add(cmbIdioma);

        p.Controls.Add(UI.Rotulo(Idioma.T("op.tema"), 380, y + 5));
        ComboBox cmbTema = UI.Combo(450, y, 220);
        cmbTema.Items.AddRange(new object[] {
            Idioma.T("op.tema.sistema"), Idioma.T("op.tema.escuro"), Idioma.T("op.tema.claro") });
        cmbTema.SelectedIndex = cfg.Tema == "escuro" ? 1 : (cfg.Tema == "claro" ? 2 : 0);
        p.Controls.Add(cmbTema);

        cmbIdioma.SelectedIndexChanged += delegate
        {
            cfg.Lingua = cmbIdioma.SelectedIndex == 1 ? "pt"
                : (cmbIdioma.SelectedIndex == 2 ? "en" : "auto");
            Guarda();
            Reabrir = true;
            Close();
        };
        cmbTema.SelectedIndexChanged += delegate
        {
            cfg.Tema = cmbTema.SelectedIndex == 1 ? "escuro"
                : (cmbTema.SelectedIndex == 2 ? "claro" : "sistema");
            Guarda();
            Reabrir = true;
            Close();
        };
        y += 46;

        // ---- lugar no Windows ----
        p.Controls.Add(UI.Titulo(Idioma.T("op.windows"), 0, y));
        y += 28;

        lblEstadoWin = UI.Rotulo("", 4, y);
        p.Controls.Add(lblEstadoWin);
        y += 26;

        p.Controls.Add(UI.Botao(Idioma.T("op.registrar"), 4, y, 220, delegate
        {
            Registrar.Registra(ExeDoMotor());
            AtualizaEstadoWindows();
            UI.Informa(this, Idioma.T("op.passoPadrao"));
            Registrar.AbrePadroesWindows();
        }, true));

        p.Controls.Add(UI.Botao(Idioma.T("op.padroesWin"), 232, y, 240, delegate
        {
            Registrar.AbrePadroesWindows();
        }, false));

        p.Controls.Add(UI.Botao(Idioma.T("op.desregistrar"), 480, y, 190, delegate
        {
            Registrar.Remove();
            AtualizaEstadoWindows();
            UI.Informa(this, Idioma.T("op.desregistrado"));
        }, false));
        y += 40;

        CheckBox cEdge = UI.Caixa(Idioma.T("op.edge"), 4, y, Registrar.CapturaEdge());
        cEdge.CheckedChanged += delegate
        {
            Registrar.DefineCapturaEdge(cEdge.Checked, ExeDoMotor());
        };
        p.Controls.Add(cEdge);
        y += 34;

        p.Controls.Add(UI.Botao(Idioma.T("op.exportar"), 4, y, 220, Exporta, false));
        p.Controls.Add(UI.Botao(Idioma.T("op.importar"), 232, y, 220, Importa, false));
        y += 40;

        Label ondeCfg = UI.Rotulo(Idioma.T("sobre.config") + ": " + Amb.CaminhoIni(), 4, y);
        ondeCfg.ForeColor = C.Texto3;
        ondeCfg.Font = new Font("Consolas", 8f);
        p.Controls.Add(ondeCfg);

        AtualizaEstadoWindows();
        return p;
    }

    string ExeDoMotor()
    {
        return Path.Combine(Amb.PastaExe, Amb.Produto + ".exe");
    }

    void Exporta(object s, EventArgs e)
    {
        using (SaveFileDialog d = new SaveFileDialog())
        {
            d.Filter = Idioma.T("op.iniFiltro");
            d.FileName = "firawselector-config.ini";
            if (d.ShowDialog(this) != DialogResult.OK) return;
            try
            {
                cfg.SalvarEm(d.FileName);
                UI.Informa(this, Idioma.T("op.exportado", d.FileName));
            }
            catch (Exception ex) { UI.Aviso(this, ex.Message); }
        }
    }

    void Importa(object s, EventArgs e)
    {
        using (OpenFileDialog d = new OpenFileDialog())
        {
            d.Filter = Idioma.T("op.iniFiltro");
            if (d.ShowDialog(this) != DialogResult.OK) return;
            if (!UI.Confirma(this, Idioma.T("op.importarAviso"))) return;

            try
            {
                // Le primeiro, grava depois: arquivo torto nao pode deixar a
                // configuracao pela metade.
                Cfg novo = Cfg.LerArquivo(d.FileName);
                if (novo.Navegadores.Count == 0)
                    throw new Exception(Idioma.T("nav.perfisNenhum"));

                novo.Salvar();
                cfg = novo;
                UI.Informa(this, Idioma.T("op.importado"));
                Reabrir = true;
                Close();
            }
            catch (Exception ex)
            {
                UI.Aviso(this, Idioma.T("op.importarRuim", ex.Message));
            }
        }
    }

    void AtualizaEstadoWindows()
    {
        bool reg = Registrar.EstaRegistrado();
        bool pad = Registrar.EhPadrao();
        lblEstadoWin.Text = Amb.Produto + ": " +
            (reg ? Idioma.T("op.registrado") : Idioma.T("op.naoRegistrado")) + "  ·  " +
            (pad ? Idioma.T("op.ehPadrao") : Idioma.T("op.naoEhPadrao"));
        lblEstadoWin.ForeColor = pad ? C.Bom : (reg ? C.Aviso : C.Texto2);
        if (faixa != null) faixa.Visible = !pad;
    }

    // ================= registro =================

    Panel PaginaRegistro()
    {
        Panel p = new Panel();

        Panel topo = Faixa(DockStyle.Top, 34);
        topo.Controls.Add(UI.Titulo(Idioma.T("lg.titulo"), 0, 0));

        Panel baixo = Faixa(DockStyle.Bottom, 46);

        txtLog = new TextBox();
        txtLog.Dock = DockStyle.Fill;
        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Both;
        txtLog.WordWrap = false;
        txtLog.BackColor = C.Campo;
        txtLog.ForeColor = C.Texto2;
        txtLog.BorderStyle = BorderStyle.FixedSingle;
        txtLog.Font = new Font("Consolas", 8.5f);

        baixo.Controls.Add(UI.Botao(Idioma.T("lg.limpar"), 0, 12, 130, delegate
        {
            try { File.Delete(Amb.CaminhoLog()); }
            catch { }
            CarregaLog();
        }, false));

        baixo.Controls.Add(UI.Botao(Idioma.T("lg.abrirPasta"), 138, 12, 150, delegate
        {
            try { Directory.CreateDirectory(Amb.PastaDados); }
            catch { }
            Amb.AbreExterno(Amb.PastaDados);
        }, false));

        p.Controls.Add(txtLog);
        p.Controls.Add(baixo);
        p.Controls.Add(topo);

        return p;
    }

    void CarregaLog()
    {
        try
        {
            if (!File.Exists(Amb.CaminhoLog()))
            {
                txtLog.Text = Idioma.T("lg.vazio");
                return;
            }
            string[] linhas = File.ReadAllLines(Amb.CaminhoLog(), Encoding.UTF8);
            Array.Reverse(linhas);
            txtLog.Text = string.Join(Environment.NewLine, linhas);
        }
        catch (Exception ex) { txtLog.Text = ex.Message; }
    }

    // ================= sobre =================

    Panel PaginaSobre()
    {
        Panel p = new Panel();
        int y = 0;

        Label nome = UI.Rotulo(Amb.Produto, 0, y);
        nome.Font = new Font("Segoe UI Semibold", 20f, FontStyle.Bold);
        nome.ForeColor = C.Acento;
        p.Controls.Add(nome);
        y += 42;

        Label ver = UI.Rotulo(Idioma.T("app.tagline") + "   ·   " + Amb.Versao + "   ·   " + Amb.Marca, 0, y);
        ver.ForeColor = C.Texto2;
        p.Controls.Add(ver);
        y += 40;

        p.Controls.Add(UI.Titulo(Idioma.T("sobre.o"), 0, y));
        y += 28;

        Label texto = UI.Rotulo(Idioma.T("sobre.texto"), 0, y);
        texto.MaximumSize = new Size(660, 120);
        texto.ForeColor = C.Texto2;
        p.Controls.Add(texto);
        y += 92;

        p.Controls.Add(UI.Titulo(Idioma.T("sobre.config"), 0, y));
        y += 28;

        Label cfgPath = UI.Rotulo(Amb.CaminhoIni(), 0, y);
        cfgPath.Font = new Font("Consolas", 8.5f);
        cfgPath.ForeColor = C.Texto3;
        p.Controls.Add(cfgPath);
        y += 22;

        Label logPath = UI.Rotulo(Amb.CaminhoLog(), 0, y);
        logPath.Font = new Font("Consolas", 8.5f);
        logPath.ForeColor = C.Texto3;
        p.Controls.Add(logPath);
        y += 40;

        p.Controls.Add(UI.Botao(Idioma.T("sobre.site"), 0, y, 200, delegate
        {
            Amb.AbreExterno(Amb.Site);
        }, true));
        p.Controls.Add(UI.Botao(Idioma.T("sobre.repo"), 210, y, 200, delegate
        {
            Amb.AbreExterno(Amb.Repo);
        }, false));
        y += 44;

        Label amb = UI.Rotulo(Amb.Windows() + "   ·   .NET " + DotNetTexto(), 0, y);
        amb.ForeColor = C.Texto3;
        amb.Font = new Font("Segoe UI", 8f);
        p.Controls.Add(amb);

        return p;
    }

    static string DotNetTexto()
    {
        int r;
        return Amb.DotNet(out r);
    }

    // ================= entrada =================

    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        string aba = "navegadores";
        foreach (string a in args)
        {
            if (a.StartsWith("--aba=")) aba = a.Substring(6);
        }

        // Trocar idioma/tema fecha e reabre: e a montagem que le a paleta.
        while (true)
        {
            Cfg cfg = Cfg.Carregar();
            Studio s = new Studio(cfg, aba);
            Application.Run(s);
            if (!s.Reabrir) break;
            aba = s.AbaAoVoltar;
        }
    }
}
