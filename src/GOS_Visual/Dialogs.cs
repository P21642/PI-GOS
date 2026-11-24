using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace GOS;

/// <summary>Base dos formulários pop-up: monta os campos de cima para baixo.</summary>
public abstract class DialogoBase : Form
{
    protected FlowLayoutPanel Corpo { get; }

    protected DialogoBase(string titulo)
    {
        Text = titulo;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        BackColor = Tema.Fundo;
        Font = Tema.Fonte;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        Corpo = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(24),
            Location = new Point(0, 0)
        };
        Controls.Add(Corpo);
    }

    protected TextBox Campo(string rotulo, string valor = "", bool senha = false, bool multilinha = false)
    {
        Corpo.Controls.Add(Tema.Rotulo(rotulo, true));
        var t = new TextBox
        {
            Width = 360,
            Font = Tema.Fonte,
            Text = valor,
            MaxLength = 255,
            UseSystemPasswordChar = senha,
            Margin = new Padding(0, 0, 0, 12)
        };
        if (multilinha)
        {
            t.Multiline = true;
            t.Height = 90;
            t.ScrollBars = ScrollBars.Vertical;
        }
        Corpo.Controls.Add(t);
        return t;
    }

    protected ComboBox Combo(string rotulo, IList itens, string campoExibicao = "")
    {
        Corpo.Controls.Add(Tema.Rotulo(rotulo, true));
        var c = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 360,
            Font = Tema.Fonte,
            Margin = new Padding(0, 0, 0, 12),
            DisplayMember = campoExibicao,
            DataSource = itens
        };
        Corpo.Controls.Add(c);
        return c;
    }

    protected void AdicionarBotoes(string textoSalvar = "Salvar")
    {
        var salvar = Tema.Botao(textoSalvar, largura: 140);
        var cancelar = Tema.Botao("Cancelar", Tema.TextoSuave, 110);
        cancelar.DialogResult = DialogResult.Cancel;
        salvar.Click += (_, _) =>
        {
            if (Validar()) DialogResult = DialogResult.OK;
        };

        var linha = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 8, 0, 0) };
        linha.Controls.Add(salvar);
        linha.Controls.Add(cancelar);
        Corpo.Controls.Add(linha);

        AcceptButton = salvar;
        CancelButton = cancelar;
    }

    protected bool Falha(string mensagem, Control campo)
    {
        Tema.Erro(mensagem);
        campo.Focus();
        return false;
    }

    protected abstract bool Validar();
}

// ============================ CLIENTE ============================
public sealed class ClienteDialog : DialogoBase
{
    private readonly TextBox _nome;
    private readonly TextBox _email;
    private readonly TextBox _telefone;

    public string Nome => _nome.Text.Trim();
    public string Email => _email.Text.Trim();
    public string Telefone => _telefone.Text.Trim();

    public ClienteDialog(Cliente? cliente = null)
        : base(cliente == null ? "Novo cliente" : "Editar cliente")
    {
        _nome = Campo("Nome", cliente?.Nome ?? "");
        _email = Campo("E-mail", cliente?.Email ?? "");
        _telefone = Campo("Telefone", cliente?.Telefone ?? "");
        AdicionarBotoes();
    }

    protected override bool Validar()
    {
        if (Nome.Length == 0) return Falha("Informe o nome.", _nome);
        if (!Tema.EmailValido(Email)) return Falha("Informe um e-mail válido.", _email);
        if (Telefone.Length == 0) return Falha("Informe o telefone.", _telefone);
        return true;
    }
}

// ============================ USUÁRIO ============================
public sealed class UsuarioDialog : DialogoBase
{
    private readonly TextBox _nome;
    private readonly TextBox _email;
    private readonly TextBox _senha;
    private readonly TextBox _confirma;
    private readonly ComboBox _cargo;

    public string Nome => _nome.Text.Trim();
    public string Email => _email.Text.Trim();
    public string Senha => _senha.Text;
    public string CargoSelecionado => (string)_cargo.SelectedItem!;

    public UsuarioDialog() : base("Novo usuário")
    {
        _nome = Campo("Nome");
        _email = Campo("E-mail");
        _senha = Campo("Senha (mínimo 6 caracteres)", senha: true);
        _confirma = Campo("Confirmar senha", senha: true);
        _cargo = Combo("Cargo", new[] { Cargo.Padrao, Cargo.Admin });
        AdicionarBotoes();
    }

    protected override bool Validar()
    {
        if (Nome.Length == 0) return Falha("Informe o nome.", _nome);
        if (!Tema.EmailValido(Email)) return Falha("Informe um e-mail válido.", _email);
        if (Senha.Length < 6) return Falha("A senha deve ter pelo menos 6 caracteres.", _senha);
        if (Senha != _confirma.Text) return Falha("As senhas não conferem.", _confirma);
        return true;
    }
}

// ======================== NOVA ORDEM DE SERVIÇO ========================
public sealed class OrdemDialog : DialogoBase
{
    private readonly ComboBox _cliente;
    private readonly ComboBox _servico;
    private readonly ComboBox _responsavel;
    private readonly TextBox _descricao;

    public int ClienteId => ((Cliente)_cliente.SelectedItem!).Id;
    public int ServicoId => ((Servico)_servico.SelectedItem!).Id;
    public int ResponsavelId => ((Usuario)_responsavel.SelectedItem!).Id;
    public string Descricao => _descricao.Text.Trim();

    public OrdemDialog(List<Cliente> clientes, List<Servico> servicos, List<Usuario> usuarios, int responsavelPadraoId)
        : base("Nova ordem de serviço")
    {
        _cliente = Combo("Cliente", clientes, nameof(Cliente.Nome));
        _servico = Combo("Tipo de serviço", servicos, nameof(Servico.Tipo));
        _responsavel = Combo("Responsável", usuarios, nameof(Usuario.Nome));
        _responsavel.SelectedItem = usuarios.FirstOrDefault(u => u.Id == responsavelPadraoId);
        _descricao = Campo("Descrição do serviço", multilinha: true);
        AdicionarBotoes("Abrir ordem");
    }

    protected override bool Validar()
    {
        if (_cliente.SelectedItem == null) return Falha("Selecione um cliente.", _cliente);
        if (_servico.SelectedItem == null) return Falha("Selecione o tipo de serviço.", _servico);
        if (_responsavel.SelectedItem == null) return Falha("Selecione o responsável.", _responsavel);
        if (Descricao.Length == 0) return Falha("Informe a descrição do serviço.", _descricao);
        return true;
    }
}

// ========================== ALTERAR STATUS ==========================
public sealed class StatusDialog : DialogoBase
{
    private readonly ComboBox _status;

    public string NovoStatus => (string)_status.SelectedItem!;

    public StatusDialog(OrdemServico os) : base($"Alterar status - Ordem #{os.Id}")
    {
        Corpo.Controls.Add(Tema.Rotulo($"Cliente: {os.ClienteNome}"));
        Corpo.Controls.Add(Tema.Rotulo($"Status atual: {os.Status}", true));
        Corpo.Controls.Add(new Label { Height = 8 });

        _status = Combo("Novo status", StatusOS.Todos);
        _status.SelectedItem = os.Status;
        AdicionarBotoes("Atualizar");
    }

    protected override bool Validar() => true;
}
