using System.Drawing;
using System.Windows.Forms;

namespace GOS;

public sealed class ClientesView : UserControl, IAtualizavel
{
    private readonly DataGridView _grid = new();
    private readonly TextBox _busca = new();

    public ClientesView()
    {
        BackColor = Tema.Fundo;

        var novo = Tema.Botao("+ Novo cliente", largura: 140);
        var editar = Tema.Botao("Editar", Tema.Escuro, 100);
        var excluir = Tema.Botao("Excluir", Tema.Perigo, 100);

        _busca.Width = 320;
        _busca.Font = Tema.Fonte;
        _busca.PlaceholderText = "Buscar por nome, e-mail ou telefone...";
        _busca.Margin = new Padding(16, 6, 0, 0);

        novo.Click += (_, _) => Novo();
        editar.Click += (_, _) => Editar();
        excluir.Click += (_, _) => Excluir();
        _busca.TextChanged += (_, _) => Atualizar();

        Tema.EstilizarGrid(_grid);
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) Editar(); };

        Tema.Empilhar(this, Tema.Barra(novo, editar, excluir, _busca), _grid);
        Atualizar();
    }

    public void Atualizar()
    {
        Tema.Executar(() =>
        {
            var termo = _busca.Text.Trim();
            var lista = ClienteRepository.Listar()
                .Where(c => termo.Length == 0
                         || c.Nome.Contains(termo, StringComparison.OrdinalIgnoreCase)
                         || c.Email.Contains(termo, StringComparison.OrdinalIgnoreCase)
                         || c.Telefone.Contains(termo, StringComparison.OrdinalIgnoreCase))
                .Select(c => new { c.Id, c.Nome, c.Email, c.Telefone })
                .ToList();

            _grid.DataSource = lista;

            if (_grid.Columns.Count == 0) return;
            _grid.Columns["Id"]!.HeaderText = "Nº";
            _grid.Columns["Id"]!.FillWeight = 20;
            _grid.Columns["Email"]!.HeaderText = "E-mail";
        });
    }

    private void Novo()
    {
        using var dlg = new ClienteDialog();
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        Tema.Executar(() =>
        {
            ClienteRepository.Criar(dlg.Nome, dlg.Email, dlg.Telefone);
            Atualizar();
        });
    }

    private void Editar()
    {
        if (Tema.IdSelecionado(_grid, "um cliente") is not int id) return;

        Tema.Executar(() =>
        {
            var cliente = ClienteRepository.Listar().FirstOrDefault(c => c.Id == id);
            if (cliente == null) { Tema.Erro("Cliente não encontrado."); return; }

            using var dlg = new ClienteDialog(cliente);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            ClienteRepository.Atualizar(new Cliente(id, dlg.Nome, dlg.Email, dlg.Telefone));
            Atualizar();
        });
    }

    private void Excluir()
    {
        if (Tema.IdSelecionado(_grid, "um cliente") is not int id) return;
        if (!Tema.Confirmar($"Excluir o cliente #{id}?")) return;

        Tema.Executar(() =>
        {
            ClienteRepository.Excluir(id);
            Atualizar();
        });
    }
}
