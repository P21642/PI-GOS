// =====================================================================================
//  !!!  ATENÇÃO: ESTE ARQUIVO É PROVISÓRIO E NÃO CIFRA NADA  !!!
//
//  Ele existe só para a integração (banco <-> C#) poder ser compilada e testada
//  enquanto a criptografia de verdade (AES-256-GCM etc.) não é implementada.
//  A implementação real é do LUAN e deve SUBSTITUIR o conteúdo deste arquivo.
//
//  Enquanto for provisório:
//   - e-mail e telefone são gravados em TEXTO CLARO, só convertidos em bytes;
//   - email_hash é um SHA-256 simples, SEM chave (dá para descobrir o e-mail
//     testando uma lista de candidatos).
//
//  Trava de segurança: a aplicação se recusa a iniciar, a menos que exista a
//  variável de ambiente GOS_PERMITIR_CRIPTO_PROVISORIA=1. Assim, se este arquivo
//  for esquecido, o sistema não roda sem ninguém perceber.
//
//  CONTRATO que a versão real deve manter (o resto do sistema só depende disto):
//    - public static bool   ChavesOk(out string erro)    -> chamada na inicialização
//    - public static byte[] CifrarEmail(string email)    -> formato do banco: nonce(12) || cifrado || tag(16)
//    - public static string DecifrarEmail(byte[] dado)
//    - public static string HashEmail(string email)      -> HMAC-SHA256 do e-mail normalizado
//                                                           (minúsculas, sem espaços), hex, 64 caracteres
//    - public static byte[] CifrarTelefone(string telefone)
//    - public static string DecifrarTelefone(byte[] dado)
//  Limites das colunas: email_cifrado varbinary(512), telefone_cifrado varbinary(128)
//  (o dado cifrado inclui nonce e tag; o provisório já respeita o mesmo limite de texto).
//  Se a decifragem falhar, lance System.Security.Cryptography.CryptographicException.
//  Textos longos demais devem lançar ErroDeNegocio (em Models.cs).
// =====================================================================================

using System.Security.Cryptography;
using System.Text;

namespace GOS;

public static class Cripto
{
    // Mesmos limites que a versão real terá (coluna menos nonce de 12 e tag de 16 bytes).
    private const int MaxBytesEmail = 512 - 12 - 16;
    private const int MaxBytesTelefone = 128 - 12 - 16;

    private const string VarPermitir = "GOS_PERMITIR_CRIPTO_PROVISORIA";

    /// <summary>Chamada na inicialização. No provisório, só libera se a variável de teste estiver ligada.</summary>
    public static bool ChavesOk(out string erro)
    {
        if (Environment.GetEnvironmentVariable(VarPermitir) == "1")
        {
            erro = "";
            return true;
        }

        erro = "Criptografia PROVISÓRIA (sem cifragem). Para testar, defina " + VarPermitir + "=1. " +
               "Para uso real, o Luan deve implementar o Cripto.cs.";
        return false;
    }

    // ------------------------------ e-mail ------------------------------
    public static byte[] CifrarEmail(string email) => Bytes(email.Trim(), MaxBytesEmail, "E-mail muito longo.");

    public static string DecifrarEmail(byte[] dado) => Encoding.UTF8.GetString(dado);

    /// <summary>PROVISÓRIO: SHA-256 sem chave. A versão real deve usar HMAC-SHA256 com chave secreta.</summary>
    public static string HashEmail(string email)
    {
        var normalizado = email.Trim().ToLowerInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizado))).ToLowerInvariant();
    }

    // ----------------------------- telefone -----------------------------
    public static byte[] CifrarTelefone(string telefone) => Bytes(telefone.Trim(), MaxBytesTelefone, "Telefone muito longo.");

    public static string DecifrarTelefone(byte[] dado) => Encoding.UTF8.GetString(dado);

    private static byte[] Bytes(string texto, int maxBytes, string msgMuitoLongo)
    {
        var bytes = Encoding.UTF8.GetBytes(texto);
        if (bytes.Length > maxBytes) throw new ErroDeNegocio(msgMuitoLongo);
        return bytes;
    }
}
