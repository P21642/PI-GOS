using System.Drawing;
using System.Windows.Forms;

namespace GOS;

public interface IAtualizavel
{
    void Atualizar();
}

public sealed class OrdensView : UserControl, IAtualizavel
{
    private readonly Usuario _logado;
    private readonly DataGridView _grid = new();
    private readonly ComboBox _filtro = new();
    private readonly Label _qtdAbertas = CriarNumero();
    private readonly Label _qtdAndamento = CriarNumero();
    private readonly Label _qtdConcluidas = CriarNumero();
    private bool _pronto; // só atualiza a tela depois que ela estiver totalmente montada

    public OrdensView(Usuario logado)
    {
        _logado = logado;
        BackColor = Tema.Fundo;

        // ---- Cartões de resumo ----
        var cartoes = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0, 0, 0, 4) };
        cartoes.Controls.Add(Cartao("Abertas", Tema.Alerta, _qtdAbertas));
        cartoes.Controls.Add(Cartao("Em andamento", Tema.Azul, _qtdAndamento));
        cartoes.Controls.Add(Cartao("Concluídas", Tema.Sucesso, _qtdConcluidas));

        // ---- Barra de ferramentas ----
        var novo = Tema.Botao("+ Nova ordem", largura: 140);
        var alterar = Tema.Botao("Alterar status", Tema.Escuro, 140);
        var detalhes = Tema.Botao("Detalhes", Tema.TextoSuave, 110);
        var excluir = Tema.Botao("Excluir", Tema.Perigo, 100);
        excluir.Visible = logado.Cargo == Cargo.Admin;

        var lblFiltro = Tema.Rotulo("Filtrar por status:", true);
        lblFiltro.Margin = new Padding(16, 9, 6, 0);

        _filtro.DropDownStyle = ComboBoxStyle.DropDownList;
        _filtro.Width = 170;
        _filtro.Margin = new Padding(0, 5, 0, 0);
        _filtro.Items.Add("Todos os status");
        _filtro.Items.AddRange(StatusOS.Todos);
        _filtro.SelectedIndex = 0;

        novo.Click += (_, _) => NovaOrdem();
        alterar.Click += (_, _) => AlterarStatus();
        detalhes.Click += (_, _) => MostrarDetalhes();
        excluir.Click += (_, _) => Excluir();
        _filtro.SelectedIndexChanged += (_, _) => Atualizar();

        var barra = Tema.Barra(novo, alterar, detalhes, excluir, lblFiltro, _filtro);

        // ---- Tabela ----
        Tema.EstilizarGrid(_grid);
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) MostrarDetalhes(); };
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Columns[e.ColumnIndex].Name == "Status" && e.Value is string st)
            {
                e.CellStyle!.ForeColor = st switch
                {
                    StatusOS.Aberto => Tema.Alerta,
                    StatusOS.EmAndamento => Tema.Azul,
                    _ => Tema.Sucesso
                };
                e.CellStyle.Font = Tema.FonteNegrito;
            }
        };

        Tema.Empilhar(this, cartoes, barra, _grid);

        _pronto = true;
        Atualizar();
    }

    public void Atualizar()
    {
        if (!_pronto) return;

        Tema.Executar(() =>
        {
            var todas = OrdemServicoRepository.Listar();
            _qtdAbertas.Text = todas.Count(o => o.Status == StatusOS.Aberto).ToString();
            _qtdAndamento.Text = todas.Count(o => o.Status == StatusOS.EmAndamento).ToString();
            _qtdConcluidas.Text = todas.Count(o => o.Status == StatusOS.Concluido).ToString();

            string? status = _filtro.SelectedIndex > 0 ? StatusOS.Todos[_filtro.SelectedIndex - 1] : null;
            var lista = status == null ? todas : todas.Where(o => o.Status == status).ToList();

            _grid.DataSource = lista.Select(o => new
            {
                o.Id,
                o.Status,
                Cliente = o.ClienteNome,
                Servico = o.TipoServico,
                Responsavel = o.ResponsavelNome,
                Abertura = o.DataAbertura.ToString("dd/MM/yyyy HH:mm"),
                Fechamento = o.DataFechamento?.ToString("dd/MM/yyyy HH:mm") ?? "-",
                Descricao = o.Descricao
            }).ToList();

            if (_grid.Columns.Count == 0) return;
            _grid.Columns["Id"]!.HeaderText = "Nº";
            _grid.Columns["Id"]!.FillWeight = 20;
            _grid.Columns["Status"]!.FillWeight = 45;
            _grid.Columns["Servico"]!.HeaderText = "Serviço";
            _grid.Columns["Servico"]!.FillWeight = 45;
            _grid.Columns["Responsavel"]!.HeaderText = "Responsável";
            _grid.Columns["Descricao"]!.HeaderText = "Descrição";
            _grid.Columns["Descricao"]!.FillWeight = 130;
        });
    }

    // ============================ Ações ============================
    private void NovaOrdem()
    {
        Tema.Executar(() =>
        {
            var clientes = ClienteRepository.Listar();
            if (clientes.Count == 0)
            {
                Tema.Info("Cadastre um cliente antes de abrir uma ordem de serviço.");
                return;
            }

            using var dlg = new OrdemDialog(clientes, ServicoRepository.Listar(), UsuarioRepository.Listar(), _logado.Id);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            int id = OrdemServicoRepository.Abrir(dlg.Descricao, dlg.ClienteId, dlg.ResponsavelId, dlg.ServicoId);
            Atualizar();
            Tema.Info($"Ordem de serviço #{id} aberta com sucesso.");
        });
    }

    private void AlterarStatus()
    {
        if (Tema.IdSelecionado(_grid, "uma ordem") is not int id) return;

        Tema.Executar(() =>
        {
            var os = OrdemServicoRepository.BuscarPorId(id);
            if (os == null) { Tema.Erro("Ordem não encontrada."); return; }

            using var dlg = new StatusDialog(os);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            OrdemServicoRepository.AtualizarStatus(id, dlg.NovoStatus);
            Atualizar();
        });
    }

    private void MostrarDetalhes()
    {
        if (Tema.IdSelecionado(_grid, "uma ordem") is not int id) return;

        Tema.Executar(() =>
        {
            var os = OrdemServicoRepository.BuscarPorId(id);
            if (os == null) { Tema.Erro("Ordem não encontrada."); return; }

            var fechamento = os.DataFechamento?.ToString("dd/MM/yyyy HH:mm") ?? "-";
            MessageBox.Show(
                $"Cliente: {os.ClienteNome}\n" +
                $"Serviço: {os.TipoServico}\n" +
                $"Responsável: {os.ResponsavelNome}\n" +
                $"Status: {os.Status}\n" +
                $"Abertura: {os.DataAbertura:dd/MM/yyyy HH:mm}\n" +
                $"Fechamento: {fechamento}\n\n" +
                $"Descrição:\n{os.Descricao}",
                $"Ordem de serviço #{os.Id}",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
    }

    private void Excluir()
    {
        if (Tema.IdSelecionado(_grid, "uma ordem") is not int id) return;
        if (!Tema.Confirmar($"Excluir a ordem #{id}? Essa ação não pode ser desfeita.")) return;

        Tema.Executar(() =>
        {
            OrdemServicoRepository.Excluir(id);
            Atualizar();
        });
    }

    // ============================ Visual ============================
    private static Label CriarNumero() => new()
    {
        Text = "0",
        AutoSize = true,
        Font = Tema.FonteNumero,
        ForeColor = Tema.Texto,
        Location = new Point(20, 6)
    };

    private static Panel Cartao(string titulo, Color cor, Label valor)
    {
        var painel = new Panel
        {
            Width = 210,
            Height = 84,
            BackColor = Tema.Cartao,
            Margin = new Padding(0, 0, 14, 12)
        };
        var barra = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = cor };
        var rotulo = new Label
        {
            Text = titulo,
            AutoSize = true,
            Font = Tema.Fonte,
            ForeColor = Tema.TextoSuave,
            Location = new Point(22, 54)
        };
        painel.Controls.Add(barra);
        painel.Controls.Add(valor);
        painel.Controls.Add(rotulo);
        return painel;
    }
}
