using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Net.Mail;
using System.Windows.Forms;

namespace GOS;

public static class Tema
{
    // ---------- Cores (amarelo e branco) ----------
    // Para ajustar o tom exato do amarelo da marca, mude só o valor de Primaria.
    public static readonly Color Primaria = Color.FromArgb(255, 193, 7);    // amarelo
    public static readonly Color Fundo = Color.FromArgb(255, 251, 235);     // branco levemente amarelado
    public static readonly Color Cartao = Color.White;
    public static readonly Color Texto = Color.FromArgb(33, 37, 41);
    public static readonly Color TextoSuave = Color.FromArgb(108, 117, 125);
    public static readonly Color Perigo = Color.FromArgb(220, 38, 38);
    public static readonly Color Sucesso = Color.FromArgb(22, 163, 74);
    public static readonly Color Alerta = Color.FromArgb(160, 105, 0);      // dourado escuro
    public static readonly Color Azul = Color.FromArgb(37, 99, 235);        // só no status "em andamento"
    public static readonly Color Selecao = Color.FromArgb(255, 243, 205);   // linha selecionada na tabela

    // ---------- Fontes ----------
    public static readonly Font Fonte = new("Segoe UI", 10F);
    public static readonly Font FonteNegrito = new("Segoe UI", 10F, FontStyle.Bold);
    public static readonly Font FonteTitulo = new("Segoe UI", 16F, FontStyle.Bold);
    public static readonly Font FonteNumero = new("Segoe UI", 22F, FontStyle.Bold);

    // ---------- Componentes ----------
    public static Button Botao(string texto, Color? cor = null, int largura = 130)
    {
        var fundo = cor ?? Primaria;
        var b = new Button
        {
            Text = texto,
            Width = largura,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = fundo,
            ForeColor = ContrasteTexto(fundo),
            Font = FonteNegrito,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0),
            UseVisualStyleBackColor = false
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(fundo, 0.1f);
        return b;
    }

    /// <summary>Texto escuro sobre fundos claros (amarelo, branco) e branco sobre fundos escuros.</summary>
    private static Color ContrasteTexto(Color fundo)
    {
        double luz = 0.299 * fundo.R + 0.587 * fundo.G + 0.114 * fundo.B;
        return luz > 150 ? Texto : Color.White;
    }

    public static Label Rotulo(string texto, bool suave = false) => new()
    {
        Text = texto,
        AutoSize = true,
        Font = Fonte,
        ForeColor = suave ? TextoSuave : Texto,
        Margin = new Padding(0, 0, 0, 4)
    };

    public static FlowLayoutPanel Barra(params Control[] itens)
    {
        var barra = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 8, 0, 12)
        };
        barra.Controls.AddRange(itens);
        return barra;
    }

    /// <summary>Empilha os controles verticalmente; o último ocupa o espaço restante.</summary>
    public static void Empilhar(Control host, params Control[] controles)
    {
        var tabela = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = controles.Length,
            Padding = new Padding(16),
            BackColor = Fundo
        };
        tabela.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (int i = 0; i < controles.Length; i++)
        {
            bool ultimo = i == controles.Length - 1;
            tabela.RowStyles.Add(ultimo ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
            controles[i].Dock = DockStyle.Fill;
            tabela.Controls.Add(controles[i], 0, i);
        }
        host.Controls.Add(tabela);
    }

    public static void EstilizarGrid(DataGridView g)
    {
        g.BackgroundColor = Cartao;
        g.BorderStyle = BorderStyle.None;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.ReadOnly = true;
        g.RowHeadersVisible = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.MultiSelect = false;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        g.GridColor = Color.FromArgb(229, 231, 235);
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;

        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.ColumnHeadersHeight = 38;
        g.ColumnHeadersDefaultCellStyle.BackColor = Primaria;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Texto;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primaria;
        g.ColumnHeadersDefaultCellStyle.Font = FonteNegrito;

        g.RowTemplate.Height = 34;
        g.DefaultCellStyle.Font = Fonte;
        g.DefaultCellStyle.ForeColor = Texto;
        g.DefaultCellStyle.SelectionBackColor = Selecao;
        g.DefaultCellStyle.SelectionForeColor = Texto;
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(255, 253, 245);
    }

    /// <summary>Retorna o Id da linha selecionada (coluna "Id") ou null, avisando o usuário.</summary>
    public static int? IdSelecionado(DataGridView g, string item)
    {
        if (g.CurrentRow == null)
        {
            Info($"Selecione {item} na lista.");
            return null;
        }
        return Convert.ToInt32(g.CurrentRow.Cells["Id"].Value);
    }

    // ---------- Mensagens ----------
    private const string TituloMensagem = "HiveSec - Ordens de Serviço";

    public static void Info(string msg) =>
        MessageBox.Show(msg, TituloMensagem, MessageBoxButtons.OK, MessageBoxIcon.Information);

    public static void Erro(string msg) =>
        MessageBox.Show(msg, TituloMensagem, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    public static bool Confirmar(string msg) =>
        MessageBox.Show(msg, TituloMensagem, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    /// <summary>Executa a ação mostrando qualquer erro numa mensagem (útil também com o banco depois).</summary>
    public static void Executar(Action acao)
    {
        try { acao(); }
        catch (Exception ex) { Erro(Erros.Traduzir(ex)); }
    }

    public static bool EmailValido(string s) => s.Contains('@') && MailAddress.TryCreate(s, out _);
}

/// <summary>
/// Logo da HiveSec. Se existir logo.png (ou logo.jpg) ao lado do programa, usa esse arquivo;
/// caso contrário desenha uma logo provisória (hexágono + texto).
/// </summary>
public sealed class LogoControl : Control
{
    private const int Margem = 6;
    private static Image? _cache;
    private static bool _carregou;

    private readonly Image? _imagem;
    private readonly bool _sobreAmarelo;

    /// <param name="altura">Altura da logo em pixels.</param>
    /// <param name="sobreAmarelo">True quando a logo fica sobre uma faixa amarela.</param>
    public LogoControl(int altura, bool sobreAmarelo)
    {
        _imagem = CarregarImagem();
        _sobreAmarelo = sobreAmarelo;

        SetStyle(ControlStyles.SupportsTransparentBackColor
               | ControlStyles.OptimizedDoubleBuffer
               | ControlStyles.AllPaintingInWmPaint
               | ControlStyles.UserPaint, true);
        BackColor = Color.Transparent;

        Height = altura;
        if (_imagem != null)
        {
            // Sobre o amarelo, a imagem ganha uma "etiqueta" branca atrás (funciona com JPG de fundo branco)
            int borda = sobreAmarelo ? 2 * Margem : 0;
            double proporcao = (double)_imagem.Width / _imagem.Height;
            Width = (int)Math.Round((altura - borda) * proporcao) + borda;
        }
        else
        {
            Width = (int)(altura * 3.3);
        }
    }

    private static Image? CarregarImagem()
    {
        if (_carregou) return _cache;
        _carregou = true;

        foreach (var nome in new[] { "logo.png", "logo.jpg", "logo.jpeg" })
        {
            var caminho = Path.Combine(AppContext.BaseDirectory, nome);
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
                // arquivo inválido: usa a logo provisória
            }
        }
        return _cache;
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
            if (_sobreAmarelo)
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
        var corHex = _sobreAmarelo ? Tema.Texto : Tema.Primaria;
        var corMiolo = _sobreAmarelo ? Tema.Primaria : Tema.Texto;

        using (var preenchimento = new SolidBrush(corHex))
            g.FillPolygon(preenchimento, Hexagono(new RectangleF(0, 0, h, h)));

        using (var caneta = new Pen(corMiolo, Math.Max(2f, h * 0.07f)))
            g.DrawPolygon(caneta, Hexagono(new RectangleF(h * 0.22f, h * 0.22f, h * 0.56f, h * 0.56f)));

        using var fonte = new Font("Segoe UI", h * 0.42f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var texto = new SolidBrush(Tema.Texto);
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
