using System.Drawing;
using System.Windows.Forms;

namespace GOS;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Login -> Principal -> (Sair) -> Login ...
        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK || login.UsuarioLogado == null) return;

            using var principal = new MainForm(login.UsuarioLogado);
            Application.Run(principal);

            if (!principal.Deslogou) return;
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
        ClientSize = new Size(420, 540);
        BackColor = Tema.Cartao;
        Font = Tema.Fonte;

        _email = new TextBox { Width = 340, Font = Tema.Fonte, Margin = new Padding(0, 0, 0, 14) };
        _senha = new TextBox { Width = 340, Font = Tema.Fonte, UseSystemPasswordChar = true, Margin = new Padding(0, 0, 0, 22) };

        var entrar = Tema.Botao("Entrar", largura: 340);
        entrar.Height = 44;
        entrar.Margin = new Padding(0, 0, 0, 18);
        entrar.Click += (_, _) => Entrar();

        var dica = new Label
        {
            Text = "Acesso de demonstração:\nadmin@gos.com  /  admin123",
            AutoSize = true,
            ForeColor = Tema.TextoSuave,
            Font = new Font("Segoe UI", 9F)
        };

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
        corpo.Controls.Add(dica);

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
            Tema.Erro(ex.Message);
        }
    }
}
