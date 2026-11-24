namespace GOS;

public record Usuario(int Id, string Nome, string Email, string SenhaHash, string Cargo);

public record Cliente(int Id, string Nome, string Email, string Telefone);

public record Servico(int Id, string Tipo);

public record OrdemServico(
    int Id,
    string Descricao,
    string Status,
    DateTime DataAbertura,
    DateTime? DataFechamento,
    int ClienteId,
    string ClienteNome,
    int ResponsavelId,
    string ResponsavelNome,
    int ClasseServicoId,
    string TipoServico);

// Valores iguais aos ENUMs do banco de dados
public static class StatusOS
{
    public const string Aberto = "aberto";
    public const string EmAndamento = "em andamento";
    public const string Concluido = "concluido";

    public static readonly string[] Todos = { Aberto, EmAndamento, Concluido };
}

public static class Cargo
{
    public const string Admin = "admin";
    public const string Padrao = "padrao";
}
