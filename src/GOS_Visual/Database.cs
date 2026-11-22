using MySqlConnector;

namespace GOS;

public static class Database
{
    // =====================================================================
    //  AJUSTE AQUI com os dados do MySQL da sua máquina
    // =====================================================================
    private static readonly string ConnectionString = new MySqlConnectionStringBuilder
    {
        Server = "localhost",
        Port = 3306,
        Database = "gos",
        UserID = "root",
        Password = "sua_senha_aqui",
        CharacterSet = "utf8mb4"
    }.ConnectionString;

    /// <summary>Abre uma nova conexão (use sempre com "using").</summary>
    public static MySqlConnection Open()
    {
        var conn = new MySqlConnection(ConnectionString);
        conn.Open();
        return conn;
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
