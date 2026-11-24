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
        Text = "G.O.S. - Login";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        ClientSize = new Size(400, 460);
        BackColor = Tema.Cartao;
        Font = Tema.Fonte;

        _email = new TextBox { Width = 320, Font = Tema.Fonte, Margin = new Padding(0, 0, 0, 14) };
        _senha = new TextBox { Width = 320, Font = Tema.Fonte, UseSystemPasswordChar = true, Margin = new Padding(0, 0, 0, 22) };

        var entrar = Tema.Botao("Entrar", largura: 320);
        entrar.Height = 42;
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

        var topo = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Tema.Primaria };
        var subtitulo = new Label
        {
            Text = "Gestão de Ordens de Serviço",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = Tema.Fonte,
            TextAlign = ContentAlignment.TopCenter
        };
        var titulo = new Label
        {
            Text = "G.O.S.",
            Dock = DockStyle.Top,
            Height = 90,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 32F, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomCenter
        };
        topo.Controls.Add(subtitulo);
        topo.Controls.Add(titulo);

        // O controle "Fill" é adicionado primeiro, depois o "Top"
        Controls.Add(corpo);
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
