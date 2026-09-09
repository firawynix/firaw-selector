using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// Gera firawselector.ico. Roda uma vez; o .ico entra no repositorio.
// O desenho e o mesmo da marca: um no que se abre em duas saidas — o link
// chegando e sendo mandado para um navegador ou para outro.
static class MkIco
{
    static readonly Color Ciano = Color.FromArgb(34, 211, 238);
    static readonly Color CianoEsc = Color.FromArgb(8, 145, 178);
    static readonly Color Escuro = Color.FromArgb(9, 16, 24);

    static GraphicsPath Arredondado(RectangleF r, float raio)
    {
        GraphicsPath p = new GraphicsPath();
        float d = raio * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    static Bitmap Desenha(int s)
    {
        Bitmap bm = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bm))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            RectangleF fundo = new RectangleF(0.5f, 0.5f, s - 1f, s - 1f);
            using (GraphicsPath p = Arredondado(fundo, s * 0.22f))
            using (LinearGradientBrush b = new LinearGradientBrush(
                new RectangleF(0, 0, s, s), Ciano, CianoEsc, LinearGradientMode.ForwardDiagonal))
                g.FillPath(b, p);

            float traco = Math.Max(1.4f, s * 0.075f);
            PointF no = new PointF(s * 0.28f, s * 0.50f);
            PointF cima = new PointF(s * 0.73f, s * 0.27f);
            PointF baixo = new PointF(s * 0.73f, s * 0.73f);

            using (Pen caneta = new Pen(Escuro, traco))
            {
                caneta.StartCap = LineCap.Round;
                caneta.EndCap = LineCap.Round;
                caneta.LineJoin = LineJoin.Round;
                g.DrawLine(caneta, no, cima);
                g.DrawLine(caneta, no, baixo);
            }

            using (Brush b = new SolidBrush(Escuro))
            {
                float rNo = s * 0.115f;
                g.FillEllipse(b, no.X - rNo, no.Y - rNo, rNo * 2, rNo * 2);
                float rP = s * 0.085f;
                g.FillEllipse(b, cima.X - rP, cima.Y - rP, rP * 2, rP * 2);
                g.FillEllipse(b, baixo.X - rP, baixo.Y - rP, rP * 2, rP * 2);
            }
        }
        return bm;
    }

    static void Main(string[] args)
    {
        string saida = args.Length > 0 ? args[0] : "firawselector.ico";
        int[] tamanhos = new int[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

        List<byte[]> pngs = new List<byte[]>();
        foreach (int s in tamanhos)
        {
            using (Bitmap bm = Desenha(s))
            using (MemoryStream ms = new MemoryStream())
            {
                bm.Save(ms, ImageFormat.Png);
                pngs.Add(ms.ToArray());
            }
        }

        using (FileStream fs = new FileStream(saida, FileMode.Create))
        using (BinaryWriter w = new BinaryWriter(fs))
        {
            w.Write((short)0);                 // reservado
            w.Write((short)1);                 // 1 = icone
            w.Write((short)tamanhos.Length);

            int offset = 6 + 16 * tamanhos.Length;
            for (int i = 0; i < tamanhos.Length; i++)
            {
                int s = tamanhos[i];
                w.Write((byte)(s >= 256 ? 0 : s));   // 0 quer dizer 256
                w.Write((byte)(s >= 256 ? 0 : s));
                w.Write((byte)0);              // cores da paleta
                w.Write((byte)0);              // reservado
                w.Write((short)1);             // planos
                w.Write((short)32);            // bits por pixel
                w.Write(pngs[i].Length);
                w.Write(offset);
                offset += pngs[i].Length;
            }
            foreach (byte[] png in pngs) w.Write(png);
        }
        Console.WriteLine("ok: " + saida + " (" + tamanhos.Length + " tamanhos)");
    }
}
