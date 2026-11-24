using MySqlConnector;

namespace GOS;

// ============================ USUÁRIOS ============================
public static class UsuarioRepository
{
    private const int MaxTentativas = 5;      // falhas seguidas até bloquear
    private const int MinutosBloqueio = 15;   // duração do bloqueio
    private const int CustoBcrypt = 12;

    // Hash usado quando o e-mail não existe: o login gasta o mesmo tempo (bcrypt)
    // com e-mail certo ou errado, o que dificulta descobrir quais e-mails existem.
    private static readonly string HashFalso =
        BCrypt.Net.BCrypt.EnhancedHashPassword("hash-falso-gos", CustoBcrypt);

    public static int Contar()
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn, "SELECT COUNT(*) FROM usuarios");
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>Cadastra pessoa + usuário numa transação (procedure). Devolve o id criado.</summary>
    public static int Criar(string nome, string email, string senha, string cargo)
    {
        if (cargo != Cargo.Admin && cargo != Cargo.Padrao)
            throw new ErroDeNegocio("Cargo inválido.");
        if (senha.Length < 8 || senha.Length > 128)
            throw new ErroDeNegocio("A senha deve ter entre 8 e 128 caracteres.");

        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "CALL sp_cadastrar_usuario(@nome, @email_cifrado, @email_hash, @senha_hash, @cargo)",
            ("@nome", nome),
            ("@email_cifrado", Cripto.CifrarEmail(email)),
            ("@email_hash", Cripto.HashEmail(email)),
            ("@senha_hash", BCrypt.Net.BCrypt.EnhancedHashPassword(senha, CustoBcrypt)),
            ("@cargo", cargo));
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Devolve o usuário se e-mail e senha conferem; null se não conferem.
    /// Conta bloqueada por excesso de tentativas lança ErroDeNegocio.
    /// </summary>
    public static Usuario? Autenticar(string email, string senha)
    {
        using var conn = Database.Open();

        int id;
        string nome, cargo, senhaHash;
        byte[] emailCifrado;
        bool bloqueado;

        using (var cmd = Database.Cmd(conn,
            @"SELECT u.pessoa_id, p.nome, p.email_cifrado, u.senha_hash, u.cargo,
                     (u.bloqueado_ate IS NOT NULL AND u.bloqueado_ate > NOW()) AS bloqueado
              FROM usuarios u
              JOIN pessoas p ON p.id = u.pessoa_id
              WHERE p.email_hash = @hash",
            ("@hash", Cripto.HashEmail(email))))
        using (var r = cmd.ExecuteReader())
        {
            if (!r.Read())
            {
                BCrypt.Net.BCrypt.EnhancedVerify(senha, HashFalso); // igualar o tempo de resposta
                return null;
            }

            id = r.GetInt32("pessoa_id");
            nome = r.GetString("nome");
            emailCifrado = (byte[])r["email_cifrado"];
            senhaHash = r.GetString("senha_hash");
            cargo = r.GetString("cargo");
            bloqueado = Convert.ToInt32(r["bloqueado"]) == 1;
        } // o leitor precisa ser fechado antes de usar a conexão de novo

        if (bloqueado)
            throw new ErroDeNegocio(
                $"Conta temporariamente bloqueada por excesso de tentativas. Tente novamente em até {MinutosBloqueio} minutos.");

        if (!BCrypt.Net.BCrypt.EnhancedVerify(senha, senhaHash))
        {
            RegistrarFalha(conn, id);
            return null;
        }

        ResetarTentativas(conn, id);
        return new Usuario(id, nome, Cripto.DecifrarEmail(emailCifrado), cargo);
    }

    private static void RegistrarFalha(MySqlConnection conn, int id)
    {
        // bloqueado_ate vem antes de tentativas_falhas no SET: o MySQL avalia da esquerda
        // para a direita, então as duas condições enxergam o valor antigo de tentativas_falhas.
        // Ao bloquear, o contador volta a zero.
        using var cmd = Database.Cmd(conn,
            @"UPDATE usuarios
              SET bloqueado_ate = CASE WHEN tentativas_falhas + 1 >= @maxTent
                                       THEN DATE_ADD(NOW(), INTERVAL @minBloq MINUTE)
                                       ELSE bloqueado_ate END,
                  tentativas_falhas = CASE WHEN tentativas_falhas + 1 >= @maxTent
                                           THEN 0 ELSE tentativas_falhas + 1 END
              WHERE pessoa_id = @id",
            ("@maxTent", MaxTentativas), ("@minBloq", MinutosBloqueio), ("@id", id));
        cmd.ExecuteNonQuery();
    }

    private static void ResetarTentativas(MySqlConnection conn, int id)
    {
        using var cmd = Database.Cmd(conn,
            @"UPDATE usuarios SET tentativas_falhas = 0, bloqueado_ate = NULL
              WHERE pessoa_id = @id AND (tentativas_falhas <> 0 OR bloqueado_ate IS NOT NULL)",
            ("@id", id));
        cmd.ExecuteNonQuery();
    }

    public static List<Usuario> Listar()
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            @"SELECT u.pessoa_id, p.nome, p.email_cifrado, u.cargo
              FROM usuarios u JOIN pessoas p ON p.id = u.pessoa_id
              ORDER BY p.nome");
        using var r = cmd.ExecuteReader();
        var lista = new List<Usuario>();
        while (r.Read())
            lista.Add(new Usuario(
                r.GetInt32("pessoa_id"),
                r.GetString("nome"),
                Cripto.DecifrarEmail((byte[])r["email_cifrado"]),
                r.GetString("cargo")));
        return lista;
    }

    /// <summary>Exclui o usuário (procedure). A trigger impede excluir o último admin.</summary>
    public static bool Excluir(int id)
    {
        using var conn = Database.Open();
        using (var existe = Database.Cmd(conn,
            "SELECT COUNT(*) FROM usuarios WHERE pessoa_id = @id", ("@id", id)))
        {
            if (Convert.ToInt32(existe.ExecuteScalar()) == 0) return false;
        }

        using var cmd = Database.Cmd(conn, "CALL sp_excluir_usuario(@id)", ("@id", id));
        cmd.ExecuteNonQuery();
        return true;
    }
}

// ============================ CLIENTES ============================
public static class ClienteRepository
{
    /// <summary>Cadastra pessoa + cliente numa transação (procedure).</summary>
    public static void Criar(string nome, string email, string telefone)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "CALL sp_cadastrar_cliente(@nome, @email_cifrado, @email_hash, @telefone_cifrado)",
            ("@nome", nome),
            ("@email_cifrado", Cripto.CifrarEmail(email)),
            ("@email_hash", Cripto.HashEmail(email)),
            ("@telefone_cifrado", Cripto.CifrarTelefone(telefone)));
        cmd.ExecuteNonQuery();
    }

    public static List<Cliente> Listar()
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            @"SELECT c.pessoa_id, p.nome, p.email_cifrado, c.telefone_cifrado
              FROM clientes c JOIN pessoas p ON p.id = c.pessoa_id
              ORDER BY p.nome");
        using var r = cmd.ExecuteReader();
        var lista = new List<Cliente>();
        while (r.Read())
            lista.Add(new Cliente(
                r.GetInt32("pessoa_id"),
                r.GetString("nome"),
                Cripto.DecifrarEmail((byte[])r["email_cifrado"]),
                Cripto.DecifrarTelefone((byte[])r["telefone_cifrado"])));
        return lista;
    }

    /// <summary>Atualiza pessoas e clientes na mesma transação: ou grava os dois, ou nenhum.</summary>
    public static void Atualizar(Cliente c)
    {
        using var conn = Database.Open();
        using var tx = conn.BeginTransaction();

        using (var cmd = Database.Cmd(conn,
            "UPDATE pessoas SET nome = @nome, email_cifrado = @ec, email_hash = @eh WHERE id = @id",
            ("@nome", c.Nome),
            ("@ec", Cripto.CifrarEmail(c.Email)),
            ("@eh", Cripto.HashEmail(c.Email)),
            ("@id", c.Id)))
        {
            cmd.Transaction = tx;
            cmd.ExecuteNonQuery();
        }

        using (var cmd = Database.Cmd(conn,
            "UPDATE clientes SET telefone_cifrado = @tc WHERE pessoa_id = @id",
            ("@tc", Cripto.CifrarTelefone(c.Telefone)),
            ("@id", c.Id)))
        {
            cmd.Transaction = tx;
            cmd.ExecuteNonQuery();
        }

        tx.Commit();
    }

    public static bool Excluir(int id)
    {
        using var conn = Database.Open();
        using (var existe = Database.Cmd(conn,
            "SELECT COUNT(*) FROM clientes WHERE pessoa_id = @id", ("@id", id)))
        {
            if (Convert.ToInt32(existe.ExecuteScalar()) == 0) return false;
        }

        using var cmd = Database.Cmd(conn, "CALL sp_excluir_cliente(@id)", ("@id", id));
        cmd.ExecuteNonQuery();
        return true;
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
    // A view já junta ordem + nome do cliente + nome do responsável + tipo de serviço.
    private const string SelectBase = @"
        SELECT id, descricao, status, data_abertura, data_fechamento,
               cliente_id, cliente_nome, responsavel_id, responsavel_nome,
               classe_servico_id, tipo_de_servico
        FROM vw_ordens_detalhadas ";

    /// <summary>Abre uma ordem. Status e data de abertura ficam por conta do banco (default + trigger).</summary>
    public static int Abrir(string descricao, int clienteId, int responsavelId, int classeServicoId)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            @"INSERT INTO ordem_servicos (descricao, cliente_id, responsavel_id, classe_servico_id)
              VALUES (@descricao, @cliente, @responsavel, @classe)",
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
            : Consultar("WHERE status = @status", ("@status", status));
    }

    public static OrdemServico? BuscarPorId(int id)
    {
        return Consultar("WHERE id = @id", ("@id", id)).FirstOrDefault();
    }

    /// <summary>
    /// Atualiza o status. A data de fechamento e a regra "concluída/cancelada não muda mais"
    /// ficam na trigger trg_os_before_update (o erro dela chega como MySqlException, SQLSTATE 45000).
    /// </summary>
    public static bool AtualizarStatus(int id, string status)
    {
        using var conn = Database.Open();
        using var cmd = Database.Cmd(conn,
            "UPDATE ordem_servicos SET status = @status WHERE id = @id",
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
        using var cmd = Database.Cmd(conn, SelectBase + where + " ORDER BY id DESC", parametros);
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
