using System.Drawing;
using System.Windows.Forms;

namespace GOS;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // Erro inesperado numa tela: mostra mensagem segura em vez de derrubar o programa.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Tema.Erro(Erros.Traduzir(e.Exception));

        ApplicationConfiguration.Initialize();

        if (!Iniciar()) return;

        // Login -> Principal -> (Sair) -> Login ...
        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK || login.UsuarioLogado == null) return;

            // A partir daqui toda conexão com o banco informa quem está logado (auditoria).
            Database.UsuarioLogadoId = login.UsuarioLogado.Id;

            using var principal = new MainForm(login.UsuarioLogado);
            Application.Run(principal);

            Database.UsuarioLogadoId = null;
            if (!principal.Deslogou) return;
        }
    }

    /// <summary>Confere conexão com o MySQL, configuração de criptografia e cria o primeiro administrador.</summary>
    private static bool Iniciar()
    {
        if (!Database.TestarConexao(out var erro))
        {
            Tema.Erro("Não foi possível conectar ao MySQL:\n" + erro +
                "\n\nVerifique GOS_DB_USER e GOS_DB_PASSWORD (e GOS_DB_HOST/PORT/NAME, se usar), " +
                "se o MySQL está rodando e se o banco 'gos' foi criado (database/database.sql).");
            return false;
        }

        if (!Cripto.ChavesOk(out var erroChaves))
        {
            Tema.Erro("Configuração de criptografia inválida:\n" + erroChaves + "\n\nVeja a seção 4 do README e o Cripto.cs.");
            return false;
        }

        try
        {
            ServicoRepository.GarantirPadrao();
            return UsuarioRepository.Contar() > 0 || CadastrarPrimeiroAdmin();
        }
        catch (Exception ex)
        {
            Tema.Erro(Erros.Traduzir(ex));
            return false;
        }
    }

    // Primeira execução: ainda não existe nenhum usuário, então o primeiro é sempre administrador.
    private static bool CadastrarPrimeiroAdmin()
    {
        Tema.Info("Primeiro acesso: cadastre o administrador do sistema.\n(O cargo escolhido na tela é ignorado: o primeiro usuário é sempre administrador.)");

        while (true)
        {
            using var dlg = new UsuarioDialog();
            if (dlg.ShowDialog() != DialogResult.OK) return false;

            try
            {
                UsuarioRepository.Criar(dlg.Nome, dlg.Email, dlg.Senha, Cargo.Admin);
                return true;
            }
            catch (Exception ex)
            {
                Tema.Erro(Erros.Traduzir(ex));
            }
        }
    }
}

public sealed class LoginForm : Form
{
    private readonly TextBox _email;
    private readonly TextBox _senha;

    public Usuario? UsuarioLogado { get; private set; }

    public LoginForm()
    {
        Text = "HiveSec - Login";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(400, 520);
        BackColor = Tema.Cartao;
        Font = Tema.Fonte;

        _email = new TextBox { Width = 320, Font = Tema.Fonte, Margin = new Padding(0, 0, 0, 14) };
        _senha = new TextBox { Width = 320, Font = Tema.Fonte, UseSystemPasswordChar = true, Margin = new Padding(0, 0, 0, 22) };

        var entrar = Tema.Botao("Entrar", largura: 320);
        entrar.Height = 42;
        entrar.Margin = new Padding(0, 0, 0, 18);
        entrar.Click += (_, _) => Entrar();

        var corpo = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(40, 28, 40, 0)
        };
        corpo.Controls.Add(Tema.Rotulo("E-mail", true));
        corpo.Controls.Add(_email);
        corpo.Controls.Add(Tema.Rotulo("Senha", true));
        corpo.Controls.Add(_senha);
        corpo.Controls.Add(entrar);

        // ---- Faixa superior com a logo ----
        var topo = new Panel { Dock = DockStyle.Top, Height = 190, BackColor = Tema.Escuro };
        var logo = new LogoControl(72, fundoEscuro: true);
        var subtitulo = new Label
        {
            Text = "Gestão de Ordens de Serviço",
            AutoSize = true,
            ForeColor = Color.FromArgb(212, 212, 216),
            Font = Tema.Fonte
        };
        var faixa = new Panel { Dock = DockStyle.Bottom, Height = 5, BackColor = Tema.Primaria };

        topo.Controls.Add(faixa);
        topo.Controls.Add(logo);
        topo.Controls.Add(subtitulo);

        void Centralizar()
        {
            logo.Location = new Point((topo.Width - logo.Width) / 2, 38);
            subtitulo.Location = new Point((topo.Width - subtitulo.Width) / 2, 38 + logo.Height + 18);
        }
        topo.Resize += (_, _) => Centralizar();
        Centralizar();

        var rodape = new Label
        {
            Text = "HiveSec Consultoria",
            Dock = DockStyle.Bottom,
            Height = 36,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Tema.TextoSuave,
            Font = new Font("Segoe UI", 8.5F)
        };

        // Ordem de docking: "Fill" primeiro, depois "Bottom" e "Top"
        Controls.Add(corpo);
        Controls.Add(rodape);
        Controls.Add(topo);

        AcceptButton = entrar;
    }

    private void Entrar()
    {
        try
        {
            var usuario = UsuarioRepository.Autenticar(_email.Text.Trim(), _senha.Text);
            if (usuario == null)
            {
                Tema.Erro("E-mail ou senha inválidos.");
                _senha.Clear();
                _senha.Focus();
                return;
            }

            UsuarioLogado = usuario;
            DialogResult = DialogResult.OK;
        }
        catch (Exception ex)
        {
            Tema.Erro(Erros.Traduzir(ex));
        }
    }
}
