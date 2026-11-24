using System.Security.Cryptography;
using MySqlConnector;

namespace GOS;

/// <summary>Converte exceções em mensagens seguras para mostrar na tela (sem detalhes internos do banco, CWE-209).</summary>
public static class Erros
{
    public static string Traduzir(Exception ex)
    {
        switch (ex)
        {
            case ErroDeNegocio e:
                return e.Message;

            case MySqlException e:
                // Regras das triggers/procedures (SIGNAL SQLSTATE '45000'): a mensagem já vem em português.
                if (e.SqlState == "45000") return e.Message;
                return e.ErrorCode switch
                {
                    MySqlErrorCode.DuplicateKeyEntry => "Já existe um registro com esse e-mail.",
                    MySqlErrorCode.RowIsReferenced2 => "Não é possível excluir: existem ordens de serviço vinculadas a este registro.",
                    MySqlErrorCode.NoReferencedRow2 => "Cliente, serviço ou responsável inválido.",
                    _ => Detalhe("Erro no banco de dados. Tente novamente ou procure o administrador.", e)
                };

            case CryptographicException:
                return "Falha ao decifrar os dados. Confira se as chaves de criptografia são as mesmas usadas no cadastro.";

            default:
                return Detalhe("Ocorreu um erro inesperado. Tente novamente ou procure o administrador.", ex);
        }
    }

    // Para depurar durante o desenvolvimento: defina a variável de ambiente GOS_DEBUG=1.
    private static string Detalhe(string mensagem, Exception ex) =>
        Environment.GetEnvironmentVariable("GOS_DEBUG") == "1" ? mensagem + "\n\n" + ex.Message : mensagem;
}
