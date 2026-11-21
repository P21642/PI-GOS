using MySqlConnector;

namespace GOS;

// ============================ USUÁRIOS ============================
public static class UsuarioRepository
{
    public static int Contar()
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "SELECT COUNT(*) FROM usuarios");
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public static void Criar(string nome, string email, string senha, string cargo)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "INSERT INTO usuarios (nome, email, senha_hash, cargo) VALUES (@nome, @email, @hash, @cargo)",
            ("@nome", nome),
            ("@email", email),
            ("@hash", BCrypt.Net.BCrypt.HashPassword(senha)),
            ("@cargo", cargo));
        cmd.ExecuteNonQuery();
    }

    public static Usuario? Autenticar(string email, string senha)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "SELECT id, nome, email, senha_hash, cargo FROM usuarios WHERE email = @email",
            ("@email", email));
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        var usuario = Ler(r);
        return BCrypt.Net.BCrypt.Verify(senha, usuario.SenhaHash) ? usuario : null;
    }

    public static List<Usuario> Listar()
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "SELECT id, nome, email, senha_hash, cargo FROM usuarios ORDER BY nome");
        using var r = cmd.ExecuteReader();
        var lista = new List<Usuario>();
        while (r.Read()) lista.Add(Ler(r));
        return lista;
    }

    public static bool Excluir(int id)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "DELETE FROM usuarios WHERE id = @id", ("@id", id));
        return cmd.ExecuteNonQuery() > 0;
    }

    private static Usuario Ler(MySqlDataReader r) => new(
        r.GetInt32("id"),
        r.GetString("nome"),
        r.GetString("email"),
        r.GetString("senha_hash"),
        r.GetString("cargo"));
}

// ============================ CLIENTES ============================
public static class ClienteRepository
{
    public static void Criar(string nome, string email, string telefone)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "INSERT INTO clientes (nome, email, telefone) VALUES (@nome, @email, @telefone)",
            ("@nome", nome), ("@email", email), ("@telefone", telefone));
        cmd.ExecuteNonQuery();
    }

    public static List<Cliente> Listar()
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "SELECT id, nome, email, telefone FROM clientes ORDER BY nome");
        using var r = cmd.ExecuteReader();
        var lista = new List<Cliente>();
        while (r.Read())
            lista.Add(new Cliente(r.GetInt32("id"), r.GetString("nome"), r.GetString("email"), r.GetString("telefone")));
        return lista;
    }

    public static void Atualizar(Cliente c)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "UPDATE clientes SET nome = @nome, email = @email, telefone = @telefone WHERE id = @id",
            ("@nome", c.Nome), ("@email", c.Email), ("@telefone", c.Telefone), ("@id", c.Id));
        cmd.ExecuteNonQuery();
    }

    public static bool Excluir(int id)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "DELETE FROM clientes WHERE id = @id", ("@id", id));
        return cmd.ExecuteNonQuery() > 0;
    }
}

// ============================ SERVIÇOS ============================
public static class ServicoRepository
{
    /// <summary>Garante que os tipos "manutencao" e "instalacao" existam.</summary>
    public static void GarantirPadrao()
    {
        using var conn = Database.Open();
        using var count = Database.Cmd(conn, "SELECT COUNT(*) FROM servicos");
        if (Convert.ToInt32(count.ExecuteScalar()) > 0) return;

        using var ins = Database.Cmd(conn,
            "INSERT INTO servicos (tipo_de_servico) VALUES ('manutencao'), ('instalacao')");
        ins.ExecuteNonQuery();
    }

    public static List<Servico> Listar()
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "SELECT id, tipo_de_servico FROM servicos ORDER BY id");
        using var r = cmd.ExecuteReader();
        var lista = new List<Servico>();
        while (r.Read()) lista.Add(new Servico(r.GetInt32("id"), r.GetString("tipo_de_servico")));
        return lista;
    }
}

// ======================= ORDENS DE SERVIÇO ========================
public static class OrdemServicoRepository
{
    private const string SelectBase = @"
        SELECT os.id, os.descricao, os.status, os.data_abertura, os.data_fechamento,
               os.cliente_id, c.nome AS cliente_nome,
               os.responsavel_id, u.nome AS responsavel_nome,
               os.classe_servico_id, s.tipo_de_servico
        FROM ordem_servicos os
        JOIN clientes c ON c.id = os.cliente_id
        JOIN usuarios u ON u.id = os.responsavel_id
        JOIN servicos s ON s.id = os.classe_servico_id ";

    public static int Abrir(string descricao, int clienteId, int responsavelId, int classeServicoId)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            @"INSERT INTO ordem_servicos
                (descricao, status, data_abertura, cliente_id, responsavel_id, classe_servico_id)
              VALUES (@descricao, 'aberto', NOW(), @cliente, @responsavel, @classe)",
            ("@descricao", descricao),
            ("@cliente", clienteId),
            ("@responsavel", responsavelId),
            ("@classe", classeServicoId));
        cmd.ExecuteNonQuery();
        return (int)cmd.LastInsertedId;
    }

    public static List<OrdemServico> Listar(string? status = null)
    {
        return status == null
            ? Consultar("")
            : Consultar("WHERE os.status = @status", ("@status", status));
    }

    public static OrdemServico? BuscarPorId(int id)
    {
        return Consultar("WHERE os.id = @id", ("@id", id)).FirstOrDefault();
    }

    /// <summary>Atualiza o status. Ao concluir, grava a data de fechamento; ao reabrir, limpa.</summary>
    public static bool AtualizarStatus(int id, string status)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            @"UPDATE ordem_servicos
              SET status = @status,
                  data_fechamento = CASE WHEN @status = 'concluido' THEN NOW() ELSE NULL END
              WHERE id = @id",
            ("@status", status), ("@id", id));
        return cmd.ExecuteNonQuery() > 0;
    }

    public static bool Excluir(int id)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "DELETE FROM ordem_servicos WHERE id = @id", ("@id", id));
        return cmd.ExecuteNonQuery() > 0;
    }

    private static List<OrdemServico> Consultar(string where, params (string nome, object? valor)[] parametros)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, SelectBase + where + " ORDER BY os.id DESC", parametros);
        using var r = cmd.ExecuteReader();

        var lista = new List<OrdemServico>();
        while (r.Read())
        {
            lista.Add(new OrdemServico(
                r.GetInt32("id"),
                r.GetString("descricao"),
                r.GetString("status"),
                r.GetDateTime("data_abertura"),
                r.IsDBNull(r.GetOrdinal("data_fechamento")) ? null : r.GetDateTime("data_fechamento"),
                r.GetInt32("cliente_id"),
                r.GetString("cliente_nome"),
                r.GetInt32("responsavel_id"),
                r.GetString("responsavel_nome"),
                r.GetInt32("classe_servico_id"),
                r.GetString("tipo_de_servico")));
        }
        return lista;
    }
}
