namespace GOS;

// E-mail e telefone chegam aqui já decifrados. O hash da senha nunca sai da camada de dados.
public record Usuario(int Id, string Nome, string Email, string Cargo);

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
    public const string Cancelado = "cancelado";

    public static readonly string[] Todos = { Aberto, EmAndamento, Concluido, Cancelado };
}

public static class Cargo
{
    public const string Admin = "admin";
    public const string Padrao = "padrao";
}

/// <summary>Erro de regra do sistema com mensagem segura para mostrar ao usuário.</summary>
public class ErroDeNegocio : Exception
{
    public ErroDeNegocio(string mensagem) : base(mensagem) { }
}
