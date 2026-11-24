using MySqlConnector;

namespace GOS;

public static class Database
{
    /// <summary>
    /// Id do usuário logado no sistema. Toda conexão aberta grava esse valor em
    /// @gos_usuario_id, que as triggers de auditoria usam para registrar "quem fez".
    /// Fica nulo antes do login.
    /// </summary>
    public static int? UsuarioLogadoId { get; set; }

    private static string? _connectionString;
    private static string ConnectionString => _connectionString ??= Montar();

    // A conexão vem de variáveis de ambiente: senha e usuário nunca ficam no código.
    //   GOS_DB_USER      (obrigatória)  usuário do banco da aplicação (não use root)
    //   GOS_DB_PASSWORD  (obrigatória)
    //   GOS_DB_HOST      (padrão: localhost)
    //   GOS_DB_PORT      (padrão: 3306)
    //   GOS_DB_NAME      (padrão: gos)
    private static string Montar()
    {
        var usuario = Env("GOS_DB_USER")
            ?? throw new InvalidOperationException("Variável de ambiente GOS_DB_USER não definida.");
        var senha = Env("GOS_DB_PASSWORD")
            ?? throw new InvalidOperationException("Variável de ambiente GOS_DB_PASSWORD não definida.");

        var host = Env("GOS_DB_HOST") ?? "localhost";
        var porta = uint.TryParse(Env("GOS_DB_PORT"), out var p) ? p : 3306u;
        var local = host is "localhost" or "127.0.0.1" or "::1";

        return new MySqlConnectionStringBuilder
        {
            Server = host,
            Port = porta,
            Database = Env("GOS_DB_NAME") ?? "gos",
            UserID = usuario,
            Password = senha,
            CharacterSet = "utf8mb4",
            // Banco em outra máquina: TLS obrigatório. Local: usa TLS se disponível.
            SslMode = local ? MySqlSslMode.Preferred : MySqlSslMode.Required,
            // Necessário para usar a variável de sessão @gos_usuario_id.
            AllowUserVariables = true
        }.ConnectionString;
    }

    private static string? Env(string nome)
    {
        var valor = Environment.GetEnvironmentVariable(nome);
        return string.IsNullOrWhiteSpace(valor) ? null : valor;
    }

    /// <summary>Abre uma nova conexão (use sempre com "using") já identificando o usuário logado.</summary>
    public static MySqlConnection Open()
    {
        var conn = new MySqlConnection(ConnectionString);
        try
        {
            conn.Open();
            using var cmd = new MySqlCommand("SET @gos_usuario_id = @id", conn);
            cmd.Parameters.AddWithValue("@id", (object?)UsuarioLogadoId ?? DBNull.Value);
            cmd.ExecuteNonQuery();
            return conn;
        }
        catch
        {
            conn.Dispose();
            throw;
        }
    }

    /// <summary>Cria um comando parametrizado (evita SQL Injection).</summary>
    public static MySqlCommand Cmd(MySqlConnection conn, string sql, params (string nome, object? valor)[] parametros)
    {
        var cmd = new MySqlCommand(sql, conn);
        foreach (var (nome, valor) in parametros)
            cmd.Parameters.AddWithValue(nome, valor ?? DBNull.Value);
        return cmd;
    }

    public static bool TestarConexao(out string erro)
    {
        try
        {
            using var conn = Open();
            erro = "";
            return true;
        }
        catch (Exception ex)
        {
            erro = ex.Message;
            return false;
        }
    }
}
