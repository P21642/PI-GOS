using System.Drawing;
using System.Windows.Forms;

namespace GOS;

// ============================ USUÁRIOS (admin) ============================
public sealed class UsuariosView : UserControl, IAtualizavel
{
    private readonly Usuario _logado;
    private readonly DataGridView _grid = new();

    public UsuariosView(Usuario logado)
    {
        _logado = logado;
        BackColor = Tema.Fundo;

        var novo = Tema.Botao("+ Novo usuário", largura: 150);
        var excluir = Tema.Botao("Excluir", Tema.Perigo, 100);
        novo.Click += (_, _) => Novo();
        excluir.Click += (_, _) => Excluir();

        Tema.EstilizarGrid(_grid);

        Tema.Empilhar(this, Tema.Barra(novo, excluir), _grid);
        Atualizar();
    }

    public void Atualizar()
    {
        Tema.Executar(() =>
        {
            _grid.DataSource = UsuarioRepository.Listar()
                .Select(u => new { u.Id, u.Nome, u.Email, u.Cargo })
                .ToList();

            if (_grid.Columns.Count == 0) return;
            _grid.Columns["Id"]!.HeaderText = "Nº";
            _grid.Columns["Id"]!.FillWeight = 20;
            _grid.Columns["Email"]!.HeaderText = "E-mail";
        });
    }

    private void Novo()
    {
        using var dlg = new UsuarioDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        Tema.Executar(() =>
        {
            UsuarioRepository.Criar(dlg.Nome, dlg.Email, dlg.Senha, dlg.CargoSelecionado);
            Atualizar();
        });
    }

    private void Excluir()
    {
        if (Tema.IdSelecionado(_grid, "um usuário") is not int id) return;
        if (id == _logado.Id)
        {
            Tema.Erro("Você não pode excluir o seu próprio usuário.");
            return;
        }
        if (!Tema.Confirmar($"Excluir o usuário #{id}?")) return;

        Tema.Executar(() =>
        {
            UsuarioRepository.Excluir(id);
            Atualizar();
        });
    }
}

// ============================ JANELA PRINCIPAL ============================
public sealed class MainForm : Form
{
    private readonly Usuario _logado;
    private readonly TabControl _abas = new();

    public bool Deslogou { get; private set; }

    public MainForm(Usuario logado)
    {
        _logado = logado;

        Text = "HiveSec - Gestão de Ordens de Serviço";
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1100, 700);
        MinimumSize = new Size(900, 560);
        BackColor = Tema.Fundo;
        Font = Tema.Fonte;

        // Abas
        _abas.Dock = DockStyle.Fill;
        _abas.Font = Tema.Fonte;
        _abas.Padding = new Point(20, 6);

        AdicionarAba("Ordens de Serviço", new OrdensView(_logado));
        AdicionarAba("Clientes", new ClientesView());
        if (_logado.Cargo == Cargo.Admin)
            AdicionarAba("Usuários", new UsuariosView(_logado));

        // Ao trocar de aba, recarrega a lista
        _abas.SelectedIndexChanged += (_, _) =>
        {
            if (_abas.SelectedTab?.Controls.Count > 0 && _abas.SelectedTab.Controls[0] is IAtualizavel tela)
                tela.Atualizar();
        };

        // O controle "Fill" é adicionado primeiro, depois o "Top"
        Controls.Add(_abas);
        Controls.Add(CriarCabecalho());
    }

    private void AdicionarAba(string titulo, Control tela)
    {
        tela.Dock = DockStyle.Fill;
        var pagina = new TabPage(titulo) { BackColor = Tema.Fundo };
        pagina.Controls.Add(tela);
        _abas.TabPages.Add(pagina);
    }

    private Control CriarCabecalho()
    {
        var cabecalho = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 64,
            BackColor = Tema.Primaria,
            ColumnCount = 3,
            RowCount = 1,
            Padding = new Padding(20, 0, 20, 0)
        };
        cabecalho.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cabecalho.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cabecalho.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        cabecalho.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var logo = new LogoControl(46, sobreAmarelo: true) { Margin = new Padding(0, 9, 0, 0) };
        var subtitulo = new Label
        {
            Text = "Gestão de Ordens de Serviço",
            AutoSize = true,
            ForeColor = Tema.Texto,
            Font = Tema.FonteNegrito,
            Margin = new Padding(16, 22, 0, 0)
        };
        var titulo = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Anchor = AnchorStyles.Left
        };
        titulo.Controls.Add(logo);
        titulo.Controls.Add(subtitulo);

        var usuario = new Label
        {
            Text = $"{_logado.Nome}  ({_logado.Cargo})",
            AutoSize = true,
            ForeColor = Tema.Texto,
            Font = Tema.Fonte,
            Margin = new Padding(0, 8, 16, 0)
        };

        var sair = Tema.Botao("Sair", Color.White, 90);
        sair.Margin = new Padding(0);
        sair.Click += (_, _) =>
        {
            if (!Tema.Confirmar("Deseja sair da sua conta?")) return;
            Deslogou = true;
            Close();
        };

        var direita = new FlowLayoutPanel
        {
            AutoSize = true,
            WrapContents = false,
            Anchor = AnchorStyles.Right
        };
        direita.Controls.Add(usuario);
        direita.Controls.Add(sair);

        cabecalho.Controls.Add(titulo, 0, 0);
        cabecalho.Controls.Add(direita, 2, 0);
        return cabecalho;
    }
}
