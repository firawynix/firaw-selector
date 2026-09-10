using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

// ---------------------------------------------------------------------------
// Uma linha da janela de escolha: icone, nome, atalho de teclado.
// ---------------------------------------------------------------------------
class ItemNavegador : Panel
{
    public readonly Navegador Nav;
    readonly int numero;
    readonly bool ehPadrao;
    readonly bool compacto;
    bool dentro;

    Cores C { get { return Cores.Atual; } }

    public ItemNavegador(Navegador n, int numeroAtalho, bool padrao, bool enxuto)
    {
        Nav = n;
        numero = numeroAtalho;
        ehPadrao = padrao;
        compacto = enxuto;

        DoubleBuffered = true;
        Height = compacto ? 64 : 52;
        Cursor = Cursors.Hand;
        BackColor = C.Painel;

        MouseEnter += delegate { dentro = true; Invalidate(); };
        MouseLeave += delegate { dentro = false; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using (Brush fundo = new SolidBrush(dentro ? C.Campo : C.Painel))
            g.FillRectangle(fundo, 0, 0, Width, Height);

        using (Pen p = new Pen(dentro ? C.Acento : C.Linha))
            g.DrawRectangle(p, 0, 0, Width - 1, Height - 1);

        if (ehPadrao)
        {
            using (Brush b = new SolidBrush(C.Acento))
                g.FillRectangle(b, 0, 1, 3, Height - 2);
        }

        // No modo enxuto o icone e a informacao principal: sai a 40, no lugar
        // dos 24 do modo normal, e a linha inteira gira em torno dele.
        int lado = compacto ? 40 : 24;
        Icon ic = Nav.Icone(compacto ? 48 : 32);
        if (ic != null) g.DrawIcon(ic, new Rectangle(compacto ? 18 : 15, (Height - lado) / 2, lado, lado));

        int textoX = compacto ? 74 : 52;
        using (Brush tinta = new SolidBrush(C.Texto))
        using (Font f = new Font("Segoe UI Semibold", compacto ? 12f : 10f, FontStyle.Bold))
        using (StringFormat sf = new StringFormat())
        {
            sf.LineAlignment = StringAlignment.Center;
            sf.Trimming = StringTrimming.EllipsisCharacter;
            sf.FormatFlags = StringFormatFlags.NoWrap;
            Rectangle r = compacto
                ? new Rectangle(textoX, 0, Width - textoX - 54, Height)
                : new Rectangle(textoX, Nav.Args.Length > 0 ? 4 : 0,
                    Width - textoX - 46, Nav.Args.Length > 0 ? 24 : Height);
            g.DrawString(Nav.Nome, f, tinta, r, sf);
        }

        if (!compacto && Nav.Args.Length > 0)
        {
            using (Brush tinta = new SolidBrush(C.Texto3))
            using (Font f = new Font("Segoe UI", 7.8f))
            using (StringFormat sf = new StringFormat())
            {
                sf.Trimming = StringTrimming.EllipsisCharacter;
                sf.FormatFlags = StringFormatFlags.NoWrap;
                g.DrawString(Nav.Args, f, tinta,
                    new Rectangle(52, 28, Width - 100, 16), sf);
            }
        }

        if (numero <= 9)
        {
            int c = compacto ? 28 : 22;
            Rectangle cx = new Rectangle(Width - c - 18, (Height - c) / 2, c, c);
            using (Brush b = new SolidBrush(C.Campo))
                g.FillRectangle(b, cx);
            using (Pen p = new Pen(C.Linha))
                g.DrawRectangle(p, cx);
            using (Brush tinta = new SolidBrush(C.Texto2))
            using (Font f = new Font("Consolas", compacto ? 11f : 9f))
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;
                g.DrawString(numero.ToString(), f, tinta, cx, sf);
            }
        }
    }
}

// ---------------------------------------------------------------------------
// A janela de escolha. E ela que aparece quando nenhuma regra decide sozinha.
// ---------------------------------------------------------------------------
class Escolha : JanelaFiraw
{
    readonly Cfg cfg;
    string url;
    readonly string host;

    Label lblUrl, lblConta;
    CheckBox chkLembrar, chkPrivado;
    Timer relogio;
    int restante;

    // Modo fantasma: os navegadores ficam todos a mostra o tempo todo; o que
    // some e a MOLDURA — barra de titulo, botoes e o fundo por tras das linhas.
    // Tudo isso volta quando o ponteiro chega, junto com o rodape.
    Timer vigia;
    bool revelada = true;
    bool fantasma;
    Panel rodape;
    string tituloBase;

    /// <summary>
    /// Cor que o Windows recorta da janela. Off-magenta de proposito: e uma cor
    /// que nao aparece em icone de navegador nenhum, e um pixel igual a ela
    /// viraria um furo na janela.
    /// </summary>
    static readonly Color Chave = Color.FromArgb(255, 0, 254);

    /// <summary>Sai da janela de escolha e volta com o modo cheio.</summary>
    public bool Reabrir;

    public Navegador Resultado;
    public bool Privativo;
    public bool Lembrar;
    public string Url { get { return url; } }

    public Escolha(Cfg c, string endereco)
        : base(Idioma.T("esc.titulo"), false, false)
    {
        cfg = c;
        url = endereco;
        host = Motor.Host(endereco);

        ShowInTaskbar = true;
        TopMost = true;
        StartPosition = FormStartPosition.CenterScreen;
        Corpo.BackColor = C.Fundo;

        Monta();

        FormClosing += delegate
        {
            if (vigia != null) { vigia.Stop(); vigia = null; }
            ParaContagem();
        };

        KeyDown += Teclado;
        MouseMove += delegate { ParaContagem(); };
    }

    void Monta()
    {
        List<Navegador> lista = cfg.Visiveis();
        bool enxuto = cfg.Compacto;
        fantasma = enxuto && cfg.Fantasma;
        int largura = enxuto ? 400 : 520;
        int y = enxuto ? 12 : 14;

        // No modo enxuto so ficam icone, nome e numero. O endereco, as caixas e
        // os botoes saem — o teclado continua fazendo tudo (1-9, Enter, Esc,
        // Ctrl para privativo).
        if (!enxuto)
        {
            lblUrl = new Label();
            lblUrl.Location = new Point(16, y);
            lblUrl.Size = new Size(largura - 32, 20);
            lblUrl.ForeColor = C.AcentoTexto;
            lblUrl.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            lblUrl.Text = string.IsNullOrEmpty(host) ? url : host;
            Corpo.Controls.Add(lblUrl);
            y += 21;

            Label caminho = new Label();
            caminho.Location = new Point(16, y);
            caminho.Size = new Size(largura - 32, 17);
            caminho.ForeColor = C.Texto3;
            caminho.Font = new Font("Consolas", 8.25f);
            caminho.Text = Encurta(url, 92);
            Corpo.Controls.Add(caminho);
            y += 26;
        }

        if (lista.Count == 0)
        {
            Label vazio = new Label();
            vazio.Location = new Point(16, y);
            vazio.Size = new Size(largura - 32, 40);
            vazio.ForeColor = C.Ruim;
            vazio.Text = Idioma.T("esc.vazio");
            Corpo.Controls.Add(vazio);
            y += 48;
        }

        Navegador padrao = cfg.Padrao;
        for (int i = 0; i < lista.Count; i++)
        {
            Navegador n = lista[i];
            ItemNavegador item = new ItemNavegador(n, i + 1, padrao != null && n.Id == padrao.Id, enxuto);
            item.Location = new Point(16, y);
            item.Width = largura - 32;
            item.Click += delegate(object s, EventArgs e)
            {
                Aceita(((ItemNavegador)s).Nav, Control.ModifierKeys == Keys.Control);
            };
            Corpo.Controls.Add(item);
            y += item.Height + 4;
        }

        if (enxuto)
        {
            y += 4;

            // Rodape: e por aqui que se chega nas Configuracoes e se volta ao
            // modo cheio sem ter de abrir o Studio no meio da escolha.
            rodape = new Panel();
            rodape.Location = new Point(16, y);
            rodape.Size = new Size(largura - 32, 30);
            rodape.BackColor = C.Fundo;

            Button bCfg = UI.Botao(Idioma.T("esc.configurar"), 0, 0, 130, delegate
            {
                AbreStudioDaEscolha();
            }, false);
            rodape.Controls.Add(bCfg);

            Button bCheio = UI.Botao(Idioma.T("esc.modoCompleto"), 138, 0, 130, delegate
            {
                cfg.Compacto = false;
                try { cfg.Salvar(); }
                catch { }
                Reabrir = true;
                Close();
            }, false);
            rodape.Controls.Add(bCheio);

            lblConta = new Label();
            lblConta.Location = new Point(276, 5);
            lblConta.Size = new Size(rodape.Width - 276, 20);
            lblConta.ForeColor = C.Texto3;
            lblConta.TextAlign = ContentAlignment.MiddleRight;
            rodape.Controls.Add(lblConta);

            Corpo.Controls.Add(rodape);
            y += 38;

            ClientSize = new Size(largura, Barra.Height + y);

            if (fantasma)
            {
                // A cor-chave vira buraco na janela: sobra so o que as linhas
                // pintam por conta propria.
                TransparencyKey = Chave;
                Revela(false);
                LigaVigia();
            }
            LigaContagem();
            return;
        }

        y += 4;
        chkPrivado = UI.Caixa(Idioma.T("esc.privativo"), 16, y, false);
        Corpo.Controls.Add(chkPrivado);

        chkLembrar = UI.Caixa(Idioma.T("esc.lembrar", string.IsNullOrEmpty(host) ? "?" : host), 180, y, false);
        chkLembrar.Enabled = cfg.Lembrar && !string.IsNullOrEmpty(host);
        // Dominio comprido estourava a largura da janela e o texto era cortado
        // no meio de uma letra; preso ao espaco que sobra, ele termina em "...".
        chkLembrar.Width = Math.Min(chkLembrar.Width, largura - 16 - 180);
        Corpo.Controls.Add(chkLembrar);
        y += 30;

        Corpo.Controls.Add(UI.Botao(Idioma.T("esc.copiar"), 16, y, 110, delegate
        {
            try { Clipboard.SetText(url); } catch { }
            lblConta.ForeColor = C.Bom;
            lblConta.Text = Idioma.T("esc.copiado");
            ParaContagem();
        }, false));

        Corpo.Controls.Add(UI.Botao(Idioma.T("esc.editar"), 132, y, 90, delegate { Edita(); }, false));

        Corpo.Controls.Add(UI.Botao(Idioma.T("btn.cancelar"), largura - 32 - 100, y, 100,
            delegate { Resultado = null; Close(); }, false));

        lblConta = new Label();
        lblConta.Location = new Point(232, y + 6);
        lblConta.Size = new Size(150, 20);
        lblConta.ForeColor = C.Texto3;
        lblConta.TextAlign = ContentAlignment.MiddleCenter;
        Corpo.Controls.Add(lblConta);
        y += 38;

        Label dica = new Label();
        dica.Location = new Point(16, y);
        dica.Size = new Size(largura - 130, 18);
        dica.ForeColor = C.Texto3;
        dica.Font = new Font("Segoe UI", 7.9f);
        dica.Text = Idioma.T("esc.dica");
        Corpo.Controls.Add(dica);

        // Atalho para o Studio sem sair da escolha.
        Label cfgLink = new Label();
        cfgLink.Location = new Point(largura - 112, y - 1);
        cfgLink.Size = new Size(96, 18);
        cfgLink.TextAlign = ContentAlignment.MiddleRight;
        cfgLink.ForeColor = C.AcentoTexto;
        cfgLink.Font = new Font("Segoe UI", 7.9f, FontStyle.Underline);
        cfgLink.Cursor = Cursors.Hand;
        cfgLink.Text = Idioma.T("esc.configurar");
        cfgLink.Click += delegate { AbreStudioDaEscolha(); };
        Corpo.Controls.Add(cfgLink);
        y += 26;

        ClientSize = new Size(largura, Barra.Height + y);
        LigaContagem();
    }

    /// <summary>
    /// Vigia o ponteiro. MouseEnter/MouseLeave da janela nao servem aqui: os
    /// filhos cobrem o corpo inteiro, entao a janela nunca recebe o evento — e
    /// sair de um filho para outro dispararia "saiu" a toda hora.
    /// </summary>
    void LigaVigia()
    {
        vigia = new Timer();
        vigia.Interval = 120;
        vigia.Tick += delegate
        {
            bool dentro;
            try { dentro = Bounds.Contains(Cursor.Position); }
            catch { return; }
            if (dentro != revelada) Revela(dentro);
        };
        vigia.Start();
    }

    /// <summary>
    /// Liga e desliga a moldura. O tamanho da janela nao muda: as linhas ficam
    /// exatamente onde estavam, so o que esta atras delas e que aparece ou some.
    /// </summary>
    void Revela(bool mostrar)
    {
        revelada = mostrar;
        Color fundo = mostrar ? C.Fundo : Chave;

        BackColor = mostrar ? C.Linha : Chave;   // a moldura de 1px da janela
        Corpo.BackColor = fundo;
        Barra.BackColor = mostrar ? C.Painel : Chave;
        foreach (Control c in Barra.Controls) c.Visible = mostrar;
        Barra.Invalidate();

        if (rodape != null)
        {
            rodape.BackColor = fundo;
            foreach (Control c in rodape.Controls) c.Visible = mostrar;
        }
        if (!mostrar) MostraContagem(restante > 0 ? Idioma.T("esc.auto", restante) : "");
    }

    /// <summary>Escondida a moldura nao ha rodape: a contagem vai para o titulo.</summary>
    void MostraContagem(string txt)
    {
        if (tituloBase == null) tituloBase = Titulo;

        if (lblConta != null && lblConta.Visible)
        {
            lblConta.Text = txt;
            Titulo = tituloBase;
            return;
        }
        Titulo = txt.Length > 0 ? tituloBase + "  —  " + txt : tituloBase;
    }

    void AbreStudioDaEscolha()
    {
        string studio = Path.Combine(Amb.PastaExe, Amb.Produto + " Studio.exe");
        try
        {
            if (File.Exists(studio)) System.Diagnostics.Process.Start(studio);
        }
        catch { }
    }

    void LigaContagem()
    {
        if (cfg.Tempo <= 0 || cfg.Padrao == null) return;

        restante = cfg.Tempo;
        relogio = new Timer();
        relogio.Interval = 1000;
        relogio.Tick += delegate
        {
            restante--;
            if (restante <= 0)
            {
                ParaContagem();
                Aceita(cfg.Padrao, false);
                return;
            }
            MostraContagem(Idioma.T("esc.auto", restante));
        };
        MostraContagem(Idioma.T("esc.auto", restante));
        relogio.Start();
    }

    static string Encurta(string s, int max)
    {
        if (s == null) return "";
        if (s.Length <= max) return s;
        return s.Substring(0, max - 3) + "...";
    }

    void ParaContagem()
    {
        if (relogio != null)
        {
            relogio.Stop();
            relogio = null;
            restante = 0;
            if (lblConta != null && lblConta.ForeColor != C.Bom) lblConta.Text = "";
            if (tituloBase != null) Titulo = tituloBase;
        }
    }

    void Edita()
    {
        ParaContagem();
        using (EditorDeLink d = new EditorDeLink(url))
        {
            if (d.ShowDialog(this) == DialogResult.OK && d.Texto.Trim().Length > 0)
            {
                url = d.Texto.Trim();
                if (lblUrl != null) lblUrl.Text = Motor.Host(url);
            }
        }
    }

    void Aceita(Navegador n, bool ctrl)
    {
        if (n == null) return;
        ParaContagem();
        Resultado = n;
        // No modo enxuto as caixas nem existem; o Ctrl continua valendo.
        Privativo = ctrl || (chkPrivado != null && chkPrivado.Checked);
        Lembrar = chkLembrar != null && chkLembrar.Checked && chkLembrar.Enabled;
        Close();
    }

    void Teclado(object s, KeyEventArgs e)
    {
        List<Navegador> lista = cfg.Visiveis();

        if (e.KeyCode == Keys.Escape) { Resultado = null; Close(); return; }
        if (e.KeyCode == Keys.Enter) { Aceita(cfg.Padrao, e.Control); return; }

        int n = -1;
        if (e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D9) n = e.KeyCode - Keys.D1;
        if (e.KeyCode >= Keys.NumPad1 && e.KeyCode <= Keys.NumPad9) n = e.KeyCode - Keys.NumPad1;
        if (n >= 0 && n < lista.Count) { Aceita(lista[n], e.Control); return; }

        ParaContagem();
    }
}

// ---------------------------------------------------------------------------
// Caixa para mexer no endereco antes de abrir.
// ---------------------------------------------------------------------------
class EditorDeLink : JanelaFiraw
{
    TextBox campo;
    public string Texto { get { return campo.Text; } }

    public EditorDeLink(string url)
        : base(Idioma.T("esc.editarTit"), false, false)
    {
        ClientSize = new Size(560, Barra.Height + 92);
        ShowInTaskbar = false;
        TopMost = true;

        campo = UI.Campo(16, 16, 528);
        campo.Text = url;
        campo.Font = new Font("Consolas", 9f);
        Corpo.Controls.Add(campo);

        Button ok = UI.Botao(Idioma.T("btn.ok"), 344, 52, 90, null, true);
        ok.DialogResult = DialogResult.OK;
        Corpo.Controls.Add(ok);

        Button cancelar = UI.Botao(Idioma.T("btn.cancelar"), 444, 52, 100, null, false);
        cancelar.DialogResult = DialogResult.Cancel;
        Corpo.Controls.Add(cancelar);

        AcceptButton = ok;
        CancelButton = cancelar;
        Shown += delegate { campo.SelectAll(); campo.Focus(); };
    }
}

// ---------------------------------------------------------------------------
// O motor. E este .exe que o Windows chama quando alguem abre um link.
// ---------------------------------------------------------------------------
static class Programa
{
    [STAThread]
    static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        List<string> enderecos = new List<string>();
        bool registrar = false, remover = false, config = false;
        bool pegarEdge = false, soltarEdge = false, silencioso = false;
        // null = segue o config; true/false = manda nesta chamada so.
        bool? enxutoAgora = null;

        foreach (string a in args)
        {
            string f = a.StartsWith("/") ? "--" + a.Substring(1) : a;
            switch (f.ToLowerInvariant())
            {
                case "--register": registrar = true; break;
                case "--unregister": remover = true; break;
                case "--settings":
                case "--config": config = true; break;
                case "--capture-edge": pegarEdge = true; break;
                case "--release-edge": soltarEdge = true; break;
                case "--silent": silencioso = true; break;
                case "--slim":
                case "--enxuto": enxutoAgora = true; break;
                case "--full":
                case "--completo": enxutoAgora = false; break;
                default:
                    if (!a.StartsWith("-")) enderecos.Add(a);
                    break;
            }
        }

        string eu = Application.ExecutablePath;

        if (registrar)
        {
            Registrar.Registra(eu);
            if (!silencioso)
            {
                Cfg.Carregar();
                MessageBox.Show(Idioma.T("op.passoPadrao"), Amb.Produto,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Registrar.AbrePadroesWindows();
            }
            return 0;
        }
        if (remover) { Registrar.Remove(); return 0; }
        if (pegarEdge) { Registrar.DefineCapturaEdge(true, eu); return 0; }
        if (soltarEdge) { Registrar.DefineCapturaEdge(false, null); return 0; }

        Cfg cfg = Cfg.Carregar();

        // Sem gravar: um atalho pode forcar o modo cheio sem mudar a preferencia.
        if (enxutoAgora.HasValue) cfg.Compacto = enxutoAgora.Value;

        // Sem endereco na linha de comando: quem chamou foi o menu Iniciar.
        // O util nesse caso e abrir o configurador, nao uma janela vazia.
        if (config || enderecos.Count == 0)
        {
            AbreStudio();
            return 0;
        }

        Navegador ultimoEscolhido = null;
        bool ultimoPrivado = false;

        foreach (string bruto in enderecos)
        {
            string url = Motor.Normaliza(bruto);
            if (url.Length == 0) continue;

            // Desembrulhar ANTES de limpar: os parametros de rastreio que
            // interessam sao os do endereco de verdade, nao os do embrulho.
            if (cfg.AbrirReal) url = Motor.Desembrulha(url);
            if (cfg.LimparRastreio) url = Motor.LimpaRastreio(url);
            if (cfg.ExpandirCurtas) url = Motor.Expande(url);

            bool privado;
            string motivo;
            Navegador n = Motor.Escolhe(cfg, url, Amb.ShiftPressionado(), out privado, out motivo);

            if (n == null && ultimoEscolhido != null)
            {
                // Varios links de uma vez: perguntar por cada um seria maltrato.
                n = ultimoEscolhido;
                privado = ultimoPrivado;
                motivo = Idioma.T("por.escolhido");
            }

            if (n == null)
            {
                Escolha janela;
                // "Modo completo" no rodape fecha e reabre a janela ali mesmo,
                // ja com a preferencia nova gravada.
                while (true)
                {
                    janela = new Escolha(cfg, url);
                    Application.Run(janela);
                    if (!janela.Reabrir) break;
                }

                if (janela.Resultado == null)
                {
                    if (cfg.Log) Amb.Registra(url + "\t(cancelado)\t" + motivo);
                    continue;
                }

                n = janela.Resultado;
                privado = janela.Privativo;
                url = janela.Url;
                motivo = Idioma.T("por.escolhido");

                if (janela.Lembrar)
                {
                    string host = Motor.Host(url);
                    if (host.Length > 0)
                    {
                        cfg.Lembrados[host] = n.Id;
                        try { cfg.Salvar(); }
                        catch { }
                    }
                }
                ultimoEscolhido = n;
                ultimoPrivado = privado;
            }

            try
            {
                n.Abrir(url, privado);
                if (cfg.Log) Amb.Registra(url + "\t" + n.Nome + "\t" + motivo);
            }
            catch (Exception ex)
            {
                Amb.Registra(url + "\tERRO\t" + ex.Message);
                MessageBox.Show(Idioma.T("inst.falhou", ex.Message), Amb.Produto,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
        return 0;
    }

    static void AbreStudio()
    {
        string studio = Path.Combine(Amb.PastaExe, Amb.Produto + " Studio.exe");
        if (File.Exists(studio))
        {
            try { System.Diagnostics.Process.Start(studio); return; }
            catch { }
        }
        MessageBox.Show(
            Amb.Produto + " " + Amb.Versao + "\n\n" +
            Idioma.T("sobre.texto") + "\n\n" + Amb.CaminhoIni(),
            Amb.Produto, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
