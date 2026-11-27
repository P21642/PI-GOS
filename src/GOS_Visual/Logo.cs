using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace GOS;

/// <summary>
/// Carrega a logo oficial da pasta Assets (logo.png, logo.jpg ou logo.jpeg).
/// Se nenhum arquivo existir, o LogoControl desenha uma logo provisória.
/// </summary>
public static class Logo
{
    private static Image? _cache;
    private static bool _carregou;

    public static Image? Carregar()
    {
        if (_carregou) return _cache;
        _carregou = true;

        foreach (var nome in new[] { "logo.png", "logo.jpg", "logo.jpeg" })
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, "Assets", nome);
            if (!File.Exists(caminho)) continue;

            try
            {
                using var arquivo = new FileStream(caminho, FileMode.Open, FileAccess.Read);
                using var original = Image.FromStream(arquivo);
                _cache = new Bitmap(original); // cópia em memória: não trava o arquivo
                break;
            }
            catch
            {
                // arquivo inválido: segue para a logo provisória
            }
        }
        return _cache;
    }
}

public sealed class LogoControl : Control
{
    private const int Margem = 6;
    private readonly Image? _imagem;
    private readonly bool _fundoEscuro;

    /// <param name="altura">Altura total da logo em pixels.</param>
    /// <param name="fundoEscuro">True quando a logo fica sobre o cabeçalho escuro.</param>
    public LogoControl(int altura, bool fundoEscuro)
    {
        _imagem = Logo.Carregar();
        _fundoEscuro = fundoEscuro;

        SetStyle(ControlStyles.SupportsTransparentBackColor
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.UserPaint, true);
        BackColor = Color.Transparent;

        Height = altura;
        if (_imagem != null)
        {
            // Com fundo escuro, a imagem ganha uma "etiqueta" branca atrás (funciona com JPG de fundo branco)
            int borda = fundoEscuro ? 2 * Margem : 0;
            double proporcao = (double)_imagem.Width / _imagem.Height;
            Width = (int)Math.Round((altura - borda) * proporcao) + borda;
        }
        else
        {
            Width = (int)(altura * 3.3);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        // ---- Logo oficial (arquivo) ----
        if (_imagem != null)
        {
            var area = new Rectangle(0, 0, Width, Height);
            if (_fundoEscuro)
            {
                using var caminho = Arredondado(area, 8);
                using var branco = new SolidBrush(Color.White);
                g.FillPath(branco, caminho);
                area.Inflate(-Margem, -Margem);
            }
            g.DrawImage(_imagem, area);
            return;
        }

        // ---- Logo provisória: hexágono + texto ----
        float h = Height;
        using (var mel = new SolidBrush(Tema.Primaria))
            g.FillPolygon(mel, Hexagono(new RectangleF(0, 0, h, h)));

        using (var caneta = new Pen(Tema.Escuro, Math.Max(2f, h * 0.07f)))
            g.DrawPolygon(caneta, Hexagono(new RectangleF(h * 0.22f, h * 0.22f, h * 0.56f, h * 0.56f)));

        using var fonte = new Font("Segoe UI", h * 0.42f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var texto = new SolidBrush(_fundoEscuro ? Color.White : Tema.Escuro);
        using var formato = new StringFormat { LineAlignment = StringAlignment.Center };
        g.DrawString("HIVESEC", fonte, texto, new RectangleF(h * 1.05f, 0, Width - h * 1.05f, h), formato);
    }

    /// <summary>Hexágono com ponta para cima, inscrito no retângulo.</summary>
    private static PointF[] Hexagono(RectangleF r)
    {
        float cx = r.X + r.Width / 2f;
        float cy = r.Y + r.Height / 2f;
        float raio = Math.Min(r.Width, r.Height) / 2f;

        var pontos = new PointF[6];
        for (int i = 0; i < 6; i++)
        {
            double angulo = Math.PI / 180.0 * (60 * i - 30);
            pontos[i] = new PointF(cx + raio * (float)Math.Cos(angulo), cy + raio * (float)Math.Sin(angulo));
        }
        return pontos;
    }

    private static GraphicsPath Arredondado(Rectangle r, int raio)
    {
        int d = raio * 2;
        var p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d - 1, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d - 1, r.Bottom - d - 1, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d - 1, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
