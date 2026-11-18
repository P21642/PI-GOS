using System.Net.Mail;
using System.Text;
using MySqlConnector;

namespace GOS;

internal static class Program
{
    private static Usuario _logado = null!;
    private static bool EhAdmin => _logado.Cargo == Cargo.Admin;

    private static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "G.O.S. - Gestão de Ordens de Serviço";

        if (!Database.TestarConexao(out var erro))
        {
            Erro("Não foi possível conectar ao MySQL:\n" + erro);
            Console.WriteLine("\nVerifique usuário/senha em Database.cs e se o banco 'gos' foi criado (database.sql).");
            Pausar();
            return;
        }

        Executar(ServicoRepository.GarantirPadrao);

        // Primeira execução: ainda não existe nenhum usuário
        if (Consultar(() => UsuarioRepository.Contar()) == 0)
        {
            Titulo("Primeiro acesso - cadastro do administrador");
            CadastrarUsuario(Cargo.Admin);
        }

        if (!Login()) return;
        MenuPrincipal();
    }

    // ============================== LOGIN ==============================
    private static bool Login()
    {
        for (int tentativa = 1; tentativa <= 3; tentativa++)
        {
            Titulo("Login");
            var email = LerTexto("E-mail: ");
            var senha = LerSenha("Senha: ");

            var usuario = Consultar(() => UsuarioRepository.Autenticar(email, senha));
            if (usuario != null)
            {
                _logado = usuario;
                return true;
            }

            Erro("E-mail ou senha inválidos.");
            Pausar();
        }
        return false;
    }

    // ============================== MENUS ==============================
    private static void MenuPrincipal()
    {
        while (true)
        {
            Titulo($"Menu principal - {_logado.Nome} ({_logado.Cargo})");
            Console.WriteLine("1) Ordens de serviço");
            Console.WriteLine("2) Clientes");
            if (EhAdmin) Console.WriteLine("3) Usuários");
            Console.WriteLine("0) Sair");

            switch (LerOpcao())
            {
                case "1": MenuOrdens(); break;
                case "2": MenuClientes(); break;
                case "3" when EhAdmin: MenuUsuarios(); break;
                case "0": return;
                default: Erro("Opção inválida."); Pausar(); break;
            }
        }
    }

    private static void MenuOrdens()
    {
        while (true)
        {
            Titulo("Ordens de serviço");
            Console.WriteLine("1) Abrir nova ordem");
            Console.WriteLine("2) Listar todas");
            Console.WriteLine("3) Listar por status");
            Console.WriteLine("4) Ver detalhes");
            Console.WriteLine("5) Atualizar status");
            if (EhAdmin) Console.WriteLine("6) Excluir ordem");
            Console.WriteLine("0) Voltar");

            switch (LerOpcao())
            {
                case "1": AbrirOrdem(); break;
                case "2": ListarOrdens(null); break;
                case "3": ListarOrdensPorStatus(); break;
                case "4": VerDetalhes(); break;
                case "5": AtualizarStatus(); break;
                case "6" when EhAdmin: ExcluirOrdem(); break;
                case "0": return;
                default: Erro("Opção inválida."); Pausar(); break;
            }
        }
    }

    private static void MenuClientes()
    {
        while (true)
        {
            Titulo("Clientes");
            Console.WriteLine("1) Cadastrar");
            Console.WriteLine("2) Listar");
            Console.WriteLine("3) Editar");
            Console.WriteLine("4) Excluir");
            Console.WriteLine("0) Voltar");

            switch (LerOpcao())
            {
                case "1": CadastrarCliente(); break;
                case "2": ListarClientes(); break;
                case "3": EditarCliente(); break;
                case "4": ExcluirCliente(); break;
                case "0": return;
                default: Erro("Opção inválida."); Pausar(); break;
            }
        }
    }

    private static void MenuUsuarios()
    {
        while (true)
        {
            Titulo("Usuários");
            Console.WriteLine("1) Cadastrar");
            Console.WriteLine("2) Listar");
            Console.WriteLine("3) Excluir");
            Console.WriteLine("0) Voltar");

            switch (LerOpcao())
            {
                case "1":
                    Titulo("Novo usuário");
                    Console.WriteLine("Cargo: 1) padrão  2) admin");
                    CadastrarUsuario(LerOpcao() == "2" ? Cargo.Admin : Cargo.Padrao);
                    break;
                case "2": ListarUsuarios(); break;
                case "3": ExcluirUsuario(); break;
                case "0": return;
                default: Erro("Opção inválida."); Pausar(); break;
            }
        }
    }

    // ======================== ORDENS DE SERVIÇO ========================
    private static void AbrirOrdem()
    {
        Titulo("Abrir ordem de serviço");

        var clientes = Consultar(() => ClienteRepository.Listar());
        var servicos = Consultar(() => ServicoRepository.Listar());
        var usuarios = Consultar(() => UsuarioRepository.Listar());
        if (clientes == null || servicos == null || usuarios == null) { Pausar(); return; }

        if (clientes.Count == 0)
        {
            Erro("Nenhum cliente cadastrado. Cadastre um cliente primeiro.");
            Pausar();
            return;
        }

        Console.WriteLine("Clientes:");
        foreach (var c in clientes) Console.WriteLine($"  {c.Id} - {c.Nome}");
        int clienteId = LerIdDaLista("ID do cliente: ", clientes.Select(c => c.Id));

        Console.WriteLine("\nTipos de serviço:");
        foreach (var s in servicos) Console.WriteLine($"  {s.Id} - {s.Tipo}");
        int servicoId = LerIdDaLista("ID do tipo de serviço: ", servicos.Select(s => s.Id));

        Console.WriteLine("\nResponsáveis:");
        foreach (var u in usuarios) Console.WriteLine($"  {u.Id} - {u.Nome}");
        int responsavelId = LerIdDaLista(
            $"ID do responsável (Enter = {_logado.Id}, você): ",
            usuarios.Select(u => u.Id),
            padrao: _logado.Id);

        var descricao = LerTexto("\nDescrição do serviço: ");

        Executar(() =>
        {
            int id = OrdemServicoRepository.Abrir(descricao, clienteId, responsavelId, servicoId);
            Sucesso($"Ordem de serviço #{id} aberta com sucesso.");
        });
        Pausar();
    }

    private static void ListarOrdens(string? status)
    {
        Titulo(status == null ? "Todas as ordens de serviço" : $"Ordens com status '{status}'");
        var lista = Consultar(() => OrdemServicoRepository.Listar(status));
        if (lista != null) ImprimirOrdens(lista);
        Pausar();
    }

    private static void ListarOrdensPorStatus()
    {
        Titulo("Filtrar por status");
        var status = EscolherStatus();
        if (status != null) ListarOrdens(status);
    }

    private static void VerDetalhes()
    {
        Titulo("Detalhes da ordem de serviço");
        int id = LerInt("ID da ordem: ");
        var os = Consultar(() => OrdemServicoRepository.BuscarPorId(id));

        if (os == null) Erro("Ordem não encontrada.");
        else
        {
            Console.WriteLine($"\nOrdem #{os.Id}");
            Console.WriteLine($"Status:       {os.Status}");
            Console.WriteLine($"Cliente:      {os.ClienteNome} (ID {os.ClienteId})");
            Console.WriteLine($"Serviço:      {os.TipoServico}");
            Console.WriteLine($"Responsável:  {os.ResponsavelNome}");
            Console.WriteLine($"Abertura:     {os.DataAbertura:dd/MM/yyyy HH:mm}");
            Console.WriteLine($"Fechamento:   {(os.DataFechamento?.ToString("dd/MM/yyyy HH:mm") ?? "-")}");
            Console.WriteLine($"Descrição:    {os.Descricao}");
        }
        Pausar();
    }

    private static void AtualizarStatus()
    {
        Titulo("Atualizar status");
        int id = LerInt("ID da ordem: ");
        var os = Consultar(() => OrdemServicoRepository.BuscarPorId(id));
        if (os == null) { Erro("Ordem não encontrada."); Pausar(); return; }

        Console.WriteLine($"Status atual: {os.Status}\n");
        var novo = EscolherStatus();
        if (novo == null) return;

        Executar(() =>
        {
            OrdemServicoRepository.AtualizarStatus(id, novo);
            Sucesso($"Ordem #{id} agora está '{novo}'.");
        });
        Pausar();
    }

    private static void ExcluirOrdem()
    {
        Titulo("Excluir ordem de serviço");
        int id = LerInt("ID da ordem: ");
        if (!Confirmar($"Excluir a ordem #{id}?")) return;

        Executar(() =>
        {
            if (OrdemServicoRepository.Excluir(id)) Sucesso("Ordem excluída.");
            else Erro("Ordem não encontrada.");
        });
        Pausar();
    }

    private static string? EscolherStatus()
    {
        for (int i = 0; i < StatusOS.Todos.Length; i++)
            Console.WriteLine($"{i + 1}) {StatusOS.Todos[i]}");
        Console.WriteLine("0) Cancelar");

        var op = LerOpcao();
        if (int.TryParse(op, out var n) && n >= 1 && n <= StatusOS.Todos.Length)
            return StatusOS.Todos[n - 1];

        if (op != "0") { Erro("Opção inválida."); Pausar(); }
        return null;
    }

    private static void ImprimirOrdens(List<OrdemServico> lista)
    {
        if (lista.Count == 0) { Console.WriteLine("Nenhuma ordem encontrada."); return; }

        Console.WriteLine(Col("ID", 6) + Col("Status", 15) + Col("Cliente", 24) +
                          Col("Serviço", 14) + Col("Responsável", 20) + "Abertura");
        Console.WriteLine(new string('-', 95));
        foreach (var o in lista)
        {
            Console.WriteLine(Col(o.Id.ToString(), 6) + Col(o.Status, 15) + Col(o.ClienteNome, 24) +
                              Col(o.TipoServico, 14) + Col(o.ResponsavelNome, 20) +
                              o.DataAbertura.ToString("dd/MM/yyyy HH:mm"));
        }
    }

    // ============================ CLIENTES =============================
    private static void CadastrarCliente()
    {
        Titulo("Novo cliente");
        var nome = LerTexto("Nome: ");
        var email = LerEmail("E-mail: ");
        var telefone = LerTexto("Telefone: ");

        Executar(() =>
        {
            ClienteRepository.Criar(nome, email, telefone);
            Sucesso("Cliente cadastrado.");
        });
        Pausar();
    }

    private static void ListarClientes()
    {
        Titulo("Clientes");
        var lista = Consultar(() => ClienteRepository.Listar());
        if (lista != null)
        {
            if (lista.Count == 0) Console.WriteLine("Nenhum cliente cadastrado.");
            else
            {
                Console.WriteLine(Col("ID", 6) + Col("Nome", 28) + Col("E-mail", 32) + "Telefone");
                Console.WriteLine(new string('-', 85));
                foreach (var c in lista)
                    Console.WriteLine(Col(c.Id.ToString(), 6) + Col(c.Nome, 28) + Col(c.Email, 32) + c.Telefone);
            }
        }
        Pausar();
    }

    private static void EditarCliente()
    {
        Titulo("Editar cliente");
        int id = LerInt("ID do cliente: ");
        var cliente = Consultar(() => ClienteRepository.Listar())?.FirstOrDefault(c => c.Id == id);
        if (cliente == null) { Erro("Cliente não encontrado."); Pausar(); return; }

        Console.WriteLine("(Enter mantém o valor atual)\n");
        var nome = LerTexto($"Nome [{cliente.Nome}]: ", obrigatorio: false);
        var email = LerEmail($"E-mail [{cliente.Email}]: ", cliente.Email);
        var telefone = LerTexto($"Telefone [{cliente.Telefone}]: ", obrigatorio: false);

        var atualizado = cliente with
        {
            Nome = nome.Length > 0 ? nome : cliente.Nome,
            Email = email,
            Telefone = telefone.Length > 0 ? telefone : cliente.Telefone
        };

        Executar(() =>
        {
            ClienteRepository.Atualizar(atualizado);
            Sucesso("Cliente atualizado.");
        });
        Pausar();
    }

    private static void ExcluirCliente()
    {
        Titulo("Excluir cliente");
        int id = LerInt("ID do cliente: ");
        if (!Confirmar($"Excluir o cliente #{id}?")) return;

        Executar(() =>
        {
            if (ClienteRepository.Excluir(id)) Sucesso("Cliente excluído.");
            else Erro("Cliente não encontrado.");
        });
        Pausar();
    }

    // ============================ USUÁRIOS =============================
    private static void CadastrarUsuario(string cargo)
    {
        var nome = LerTexto("Nome: ");
        var email = LerEmail("E-mail: ");

        string senha;
        while (true)
        {
            senha = LerSenha("Senha (mín. 6 caracteres): ");
            if (senha.Length < 6) { Erro("Senha muito curta."); continue; }
            if (senha != LerSenha("Confirme a senha: ")) { Erro("As senhas não conferem."); continue; }
            break;
        }

        Executar(() =>
        {
            UsuarioRepository.Criar(nome, email, senha, cargo);
            Sucesso($"Usuário '{nome}' cadastrado como {cargo}.");
        });
        Pausar();
    }

    private static void ListarUsuarios()
    {
        Titulo("Usuários");
        var lista = Consultar(() => UsuarioRepository.Listar());
        if (lista != null)
        {
            Console.WriteLine(Col("ID", 6) + Col("Nome", 28) + Col("E-mail", 32) + "Cargo");
            Console.WriteLine(new string('-', 80));
            foreach (var u in lista)
                Console.WriteLine(Col(u.Id.ToString(), 6) + Col(u.Nome, 28) + Col(u.Email, 32) + u.Cargo);
        }
        Pausar();
    }

    private static void ExcluirUsuario()
    {
        Titulo("Excluir usuário");
        int id = LerInt("ID do usuário: ");
        if (id == _logado.Id) { Erro("Você não pode excluir o seu próprio usuário."); Pausar(); return; }
        if (!Confirmar($"Excluir o usuário #{id}?")) return;

        Executar(() =>
        {
            if (UsuarioRepository.Excluir(id)) Sucesso("Usuário excluído.");
            else Erro("Usuário não encontrado.");
        });
        Pausar();
    }

    // ===================== TRATAMENTO DE ERROS DO BD ====================
    private static void Executar(Action acao)
    {
        try { acao(); }
        catch (MySqlException ex) { Erro(TraduzirErro(ex)); }
    }

    private static T? Consultar<T>(Func<T> acao)
    {
        try { return acao(); }
        catch (MySqlException ex) { Erro(TraduzirErro(ex)); return default; }
    }

    private static string TraduzirErro(MySqlException ex) => ex.ErrorCode switch
    {
        MySqlErrorCode.DuplicateKeyEntry => "Já existe um registro com esse e-mail.",
        MySqlErrorCode.RowIsReferenced2 => "Não é possível excluir: existem ordens de serviço vinculadas a este registro.",
        MySqlErrorCode.NoReferencedRow2 => "Cliente, serviço ou responsável inválido.",
        _ => "Erro no banco de dados: " + ex.Message
    };

    // ========================= ENTRADA / SAÍDA =========================
    private static void Titulo(string texto)
    {
        Console.Clear();
        Console.WriteLine("=== G.O.S. - Gestão de Ordens de Serviço ===");
        Console.WriteLine($"--- {texto} ---\n");
    }

    private static void Erro(string msg) => Escrever(msg, ConsoleColor.Red);
    private static void Sucesso(string msg) => Escrever(msg, ConsoleColor.Green);

    private static void Escrever(string msg, ConsoleColor cor)
    {
        Console.ForegroundColor = cor;
        Console.WriteLine(msg);
        Console.ResetColor();
    }

    private static void Pausar()
    {
        Console.Write("\nPressione qualquer tecla para continuar...");
        Console.ReadKey(intercept: true);
    }

    private static string LerOpcao()
    {
        Console.Write("\nOpção: ");
        return (Console.ReadLine() ?? "").Trim();
    }

    private static bool Confirmar(string pergunta)
    {
        Console.Write($"{pergunta} (s/n): ");
        return (Console.ReadLine() ?? "").Trim().ToLower() == "s";
    }

    private static string LerTexto(string prompt, int max = 255, bool obrigatorio = true)
    {
        while (true)
        {
            Console.Write(prompt);
            var s = (Console.ReadLine() ?? "").Trim();
            if (s.Length == 0 && obrigatorio) { Erro("Campo obrigatório."); continue; }
            if (s.Length > max) { Erro($"Máximo de {max} caracteres."); continue; }
            return s;
        }
    }

    private static string LerEmail(string prompt, string? atual = null)
    {
        while (true)
        {
            var s = LerTexto(prompt, obrigatorio: atual == null);
            if (s.Length == 0 && atual != null) return atual;
            if (MailAddress.TryCreate(s, out _) && s.Contains('@')) return s;
            Erro("E-mail inválido.");
        }
    }

    private static int LerInt(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out var n)) return n;
            Erro("Digite um número válido.");
        }
    }

    private static int LerIdDaLista(string prompt, IEnumerable<int> validos, int? padrao = null)
    {
        var set = validos.ToHashSet();
        while (true)
        {
            Console.Write(prompt);
            var s = (Console.ReadLine() ?? "").Trim();
            if (s.Length == 0 && padrao.HasValue) return padrao.Value;
            if (int.TryParse(s, out var id) && set.Contains(id)) return id;
            Erro("ID inválido. Escolha um da lista.");
        }
    }

    private static string LerSenha(string prompt)
    {
        Console.Write(prompt);
        var sb = new StringBuilder();
        while (true)
        {
            var tecla = Console.ReadKey(intercept: true);
            if (tecla.Key == ConsoleKey.Enter) { Console.WriteLine(); break; }
            if (tecla.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0) { sb.Length--; Console.Write("\b \b"); }
            }
            else if (!char.IsControl(tecla.KeyChar))
            {
                sb.Append(tecla.KeyChar);
                Console.Write('*');
            }
        }
        return sb.ToString();
    }

    // Formatação de colunas
    private static string Col(string texto, int largura) => Cortar(texto, largura - 1).PadRight(largura);
    private static string Cortar(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
