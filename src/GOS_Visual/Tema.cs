using System.Drawing;
using System.Net.Mail;
using System.Windows.Forms;

namespace GOS;

public static class Tema
{
    // =====================================================================
    //  PALETA DA MARCA - para usar as cores oficiais da HiveSec, basta
    //  trocar os valores RGB de Escuro e Primaria (o programa inteiro usa estas cores).
    // =====================================================================
    public static readonly Color Escuro = Color.FromArgb(30, 30, 36);       // cabeçalhos / grafite
    public static readonly Color Primaria = Color.FromArgb(245, 168, 0);    // destaque (âmbar / mel)
    public static readonly Color Selecao = Color.FromArgb(255, 243, 205);   // linha selecionada na tabela

    // ---------- Neutras e de estado ----------
    public static readonly Color Fundo = Color.FromArgb(244, 244, 242);
    public static readonly Color Cartao = Color.White;
    public static readonly Color Texto = Color.FromArgb(30, 30, 36);
    public static readonly Color TextoSuave = Color.FromArgb(113, 113, 122);
    public static readonly Color Perigo = Color.FromArgb(220, 38, 38);
    public static readonly Color Sucesso = Color.FromArgb(22, 163, 74);
    public static readonly Color Alerta = Color.FromArgb(180, 83, 9);
    public static readonly Color Azul = Color.FromArgb(37, 99, 235);        // status "em andamento"

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

    /// <summary>Texto escuro sobre fundos claros (ex.: âmbar) e branco sobre fundos escuros.</summary>
    private static Color ContrasteTexto(Color fundo)
    {
        double luz = 0.299 * fundo.R + 0.587 * fundo.G + 0.114 * fundo.B;
        return luz > 150 ? Escuro : Color.White;
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
        g.ColumnHeadersDefaultCellStyle.BackColor = Escuro;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Escuro;
        g.ColumnHeadersDefaultCellStyle.Font = FonteNegrito;

        g.RowTemplate.Height = 34;
        g.DefaultCellStyle.Font = Fonte;
        g.DefaultCellStyle.ForeColor = Texto;
        g.DefaultCellStyle.SelectionBackColor = Selecao;
        g.DefaultCellStyle.SelectionForeColor = Texto;
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(249, 250, 251);
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
