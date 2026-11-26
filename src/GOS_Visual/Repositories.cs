namespace GOS;

// =====================================================================
//  VERSÃO SEM BANCO DE DADOS
//  Os dados ficam só na memória e voltam ao estado inicial ao fechar o programa.
//
//  Os nomes das classes e dos métodos são os mesmos da versão com MySQL.
//  Para integrar o banco depois, basta trocar ESTE arquivo pelo
//  Repositories.cs da versão com banco (e adicionar o Database.cs e os pacotes).
// =====================================================================

internal record OrdemRegistro(
    int Id, string Descricao, string Status, DateTime Abertura, DateTime? Fechamento,
    int ClienteId, int ResponsavelId, int ServicoId);

internal static class Banco
{
    public static readonly List<Usuario> Usuarios = new();
    public static readonly List<Cliente> Clientes = new();
    public static readonly List<Servico> Servicos = new();
    public static readonly List<OrdemRegistro> Ordens = new();

    public static int ProxUsuario = 1;
    public static int ProxCliente = 1;
    public static int ProxOrdem = 1;

    // Dados de exemplo
    static Banco()
    {
        // Senha guardada em texto puro só porque é demonstração.
        // Com o banco, use BCrypt (como na versão anterior).
        Usuarios.Add(new Usuario(ProxUsuario++, "Administrador", "admin@gos.com", "admin123", Cargo.Admin));
        Usuarios.Add(new Usuario(ProxUsuario++, "Carlos Técnico", "tecnico@gos.com", "123456", Cargo.Padrao));

        Clientes.Add(new Cliente(ProxCliente++, "Maria Silva", "maria.silva@email.com", "(19) 99999-1111"));
        Clientes.Add(new Cliente(ProxCliente++, "João Souza", "joao.souza@email.com", "(19) 98888-2222"));
        Clientes.Add(new Cliente(ProxCliente++, "Padaria Pão Quente", "contato@paoquente.com", "(19) 3333-4444"));

        Servicos.Add(new Servico(1, "manutencao"));
        Servicos.Add(new Servico(2, "instalacao"));

        var agora = DateTime.Now;
        Ordens.Add(new OrdemRegistro(ProxOrdem++, "Troca da fonte do computador do caixa", StatusOS.Concluido, agora.AddDays(-6), agora.AddDays(-5), 3, 2, 1));
        Ordens.Add(new OrdemRegistro(ProxOrdem++, "Instalação de roteador Wi-Fi", StatusOS.Concluido, agora.AddDays(-4), agora.AddDays(-4).AddHours(3), 1, 2, 2));
        Ordens.Add(new OrdemRegistro(ProxOrdem++, "Notebook não liga após queda", StatusOS.EmAndamento, agora.AddDays(-2), null, 2, 2, 1));
        Ordens.Add(new OrdemRegistro(ProxOrdem++, "Instalação de câmeras de segurança (4 un.)", StatusOS.EmAndamento, agora.AddDays(-1), null, 3, 1, 2));
        Ordens.Add(new OrdemRegistro(ProxOrdem++, "Formatação e instalação de programas", StatusOS.Aberto, agora.AddHours(-3), null, 1, 1, 1));
    }
}

// ============================ USUÁRIOS ============================
public static class UsuarioRepository
{
    public static int Contar() => Banco.Usuarios.Count;

    public static void Criar(string nome, string email, string senha, string cargo)
    {
        if (Banco.Usuarios.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Já existe um usuário com esse e-mail.");

        Banco.Usuarios.Add(new Usuario(Banco.ProxUsuario++, nome, email, senha, cargo));
    }

    public static Usuario? Autenticar(string email, string senha) =>
        Banco.Usuarios.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) && u.SenhaHash == senha);

    public static List<Usuario> Listar() => Banco.Usuarios.OrderBy(u => u.Nome).ToList();

    public static bool Excluir(int id)
    {
        if (Banco.Ordens.Any(o => o.ResponsavelId == id))
            throw new InvalidOperationException("Não é possível excluir: existem ordens de serviço vinculadas a este usuário.");

        return Banco.Usuarios.RemoveAll(u => u.Id == id) > 0;
    }
}

// ============================ CLIENTES ============================
public static class ClienteRepository
{
    public static void Criar(string nome, string email, string telefone)
    {
        if (Banco.Clientes.Any(c => c.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Já existe um cliente com esse e-mail.");

        Banco.Clientes.Add(new Cliente(Banco.ProxCliente++, nome, email, telefone));
    }

    public static List<Cliente> Listar() => Banco.Clientes.OrderBy(c => c.Nome).ToList();

    public static void Atualizar(Cliente c)
    {
        if (Banco.Clientes.Any(x => x.Id != c.Id && x.Email.Equals(c.Email, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Já existe um cliente com esse e-mail.");

        int i = Banco.Clientes.FindIndex(x => x.Id == c.Id);
        if (i >= 0) Banco.Clientes[i] = c;
    }

    public static bool Excluir(int id)
    {
        if (Banco.Ordens.Any(o => o.ClienteId == id))
            throw new InvalidOperationException("Não é possível excluir: existem ordens de serviço vinculadas a este cliente.");

        return Banco.Clientes.RemoveAll(c => c.Id == id) > 0;
    }
}

// ============================ SERVIÇOS ============================
public static class ServicoRepository
{
    public static void GarantirPadrao() { /* já carregado em memória */ }

    public static List<Servico> Listar() => Banco.Servicos.OrderBy(s => s.Id).ToList();
}

// ======================= ORDENS DE SERVIÇO ========================
public static class OrdemServicoRepository
{
    public static int Abrir(string descricao, int clienteId, int responsavelId, int classeServicoId)
    {
        int id = Banco.ProxOrdem++;
        Banco.Ordens.Add(new OrdemRegistro(id, descricao, StatusOS.Aberto, DateTime.Now, null,
            clienteId, responsavelId, classeServicoId));
        return id;
    }

    public static List<OrdemServico> Listar(string? status = null) =>
        Banco.Ordens
            .Where(o => status == null || o.Status == status)
            .OrderByDescending(o => o.Id)
            .Select(Montar)
            .ToList();

    public static OrdemServico? BuscarPorId(int id) =>
        Banco.Ordens.Where(o => o.Id == id).Select(Montar).FirstOrDefault();

    public static bool AtualizarStatus(int id, string status)
    {
        int i = Banco.Ordens.FindIndex(o => o.Id == id);
        if (i < 0) return false;

        Banco.Ordens[i] = Banco.Ordens[i] with
        {
            Status = status,
            Fechamento = status == StatusOS.Concluido ? DateTime.Now : (DateTime?)null
        };
        return true;
    }

    public static bool Excluir(int id) => Banco.Ordens.RemoveAll(o => o.Id == id) > 0;

    // Equivale ao JOIN do SQL: junta os nomes de cliente, responsável e serviço
    private static OrdemServico Montar(OrdemRegistro r) => new(
        r.Id, r.Descricao, r.Status, r.Abertura, r.Fechamento,
        r.ClienteId, Banco.Clientes.First(c => c.Id == r.ClienteId).Nome,
        r.ResponsavelId, Banco.Usuarios.First(u => u.Id == r.ResponsavelId).Nome,
        r.ServicoId, Banco.Servicos.First(s => s.Id == r.ServicoId).Tipo);
}
