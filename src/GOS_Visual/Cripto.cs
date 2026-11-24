using System;
using System.Security.Cryptography;
using System.Text;

namespace GOS;

public static class Cripto
{

    private const string VarChaveHmac = "GOS_HMAC_KEY";
    private const string VarChaveAes = "GOS_AES_KEY";

    private const int TamanhoChave = 32;  
    private const int TamanhoNonce = 12;  
    private const int TamanhoTag = 16;    

    public const int LimiteEmailCifrado = 512;
    public const int LimiteTelefoneCifrado = 128;


    public static string NormalizarEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        return email.Trim().ToLowerInvariant();
    }


    public static string GerarChave()
    {
        byte[] chave = RandomNumberGenerator.GetBytes(TamanhoChave); 

        try
        {
            return Convert.ToBase64String(chave); 
        }
        finally
        {
            CryptographicOperations.ZeroMemory(chave);
        }
    }


    private static byte[] CarregarChave(string nomeVariavel)
    {
        string? valor = Environment.GetEnvironmentVariable(nomeVariavel); 

        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new InvalidOperationException($"Chave não encontrada: defina a variável de ambiente {nomeVariavel}.");
        }

        byte[] chave;
        try
        {
            chave = Convert.FromBase64String(valor.Trim());
        }
        catch (FormatException)
        {
            throw new InvalidOperationException($"A variável {nomeVariavel} não contém uma chave em Base64 válido.");
        }

        if (chave.Length != TamanhoChave)
        {
            int tamanho = chave.Length;
            CryptographicOperations.ZeroMemory(chave);
            throw new InvalidOperationException($"A chave {nomeVariavel} deve ter 32 bytes (veio com {tamanho}).");
        }

        return chave;
    }


    public static bool ChavesOk(out string erro)
    {
        try
        {
            ValidarConfiguracao();
            erro = "";
            return true;
        }
        catch (InvalidOperationException ex)
        {
            erro = ex.Message;
            return false;
        }
    }
    

    public static void ValidarConfiguracao()
    {
        byte[] chaveHmac = CarregarChave(VarChaveHmac);
        byte[] chaveAes = CarregarChave(VarChaveAes);

        try
        {
            if (CryptographicOperations.FixedTimeEquals(chaveHmac, chaveAes))
            {
                throw new InvalidOperationException($"As chaves {VarChaveHmac} e {VarChaveAes} devem ser diferentes.");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(chaveHmac);
            CryptographicOperations.ZeroMemory(chaveAes);
        }
    }


    public static string HashEmail(string email)
    {
        string emailNormalizado = NormalizarEmail(email); 
        byte[] dados = Encoding.UTF8.GetBytes(emailNormalizado); 
        byte[] chave = CarregarChave(VarChaveHmac);

        try
        {
            byte[] hash = HMACSHA256.HashData(chave, dados); 

            return Convert.ToHexString(hash).ToLowerInvariant(); 
        }
        finally
        {
            CryptographicOperations.ZeroMemory(chave); 
        }
    }


    public static byte[] Cifrar(string textoPlano, int limiteBytes = int.MaxValue)
    {
        ArgumentNullException.ThrowIfNull(textoPlano);

        byte[] dados = Encoding.UTF8.GetBytes(textoPlano);
        byte[]? chave = null;

        try
        {
            int tamanhoFinal = TamanhoNonce + dados.Length + TamanhoTag;
            if (tamanhoFinal > limiteBytes)
            {
                throw new ArgumentException(
                    $"Texto grande demais para a coluna (máximo de {limiteBytes - TamanhoNonce - TamanhoTag} bytes).",
                    nameof(textoPlano));
            }

            chave = CarregarChave(VarChaveAes);

            byte[] resultado = new byte[tamanhoFinal];
            Span<byte> nonce = resultado.AsSpan(0, TamanhoNonce);
            Span<byte> cifrado = resultado.AsSpan(TamanhoNonce, dados.Length);
            Span<byte> tag = resultado.AsSpan(TamanhoNonce + dados.Length, TamanhoTag);

            RandomNumberGenerator.Fill(nonce);

            using var aes = new AesGcm(chave, TamanhoTag);
            aes.Encrypt(nonce, dados, cifrado, tag);

            return resultado;
        }
        finally
        {
            if (chave != null) CryptographicOperations.ZeroMemory(chave);
            CryptographicOperations.ZeroMemory(dados);
        }
    }


    public static byte[] CifrarEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        string texto = email.Trim();

        if (Encoding.UTF8.GetByteCount(texto) > LimiteEmailCifrado - TamanhoNonce - TamanhoTag)
        {
            throw new ErroDeNegocio("E-mail muito longo.");
        }
        
        return Cifrar(texto, LimiteEmailCifrado);
    }


    public static string DecifrarEmail(byte[] dado)
    {
        return Decifrar(dado);
    }


    public static byte[] CifrarTelefone(string telefone)
    {
        ArgumentNullException.ThrowIfNull(telefone);
        string texto = telefone.Trim();

        if (Encoding.UTF8.GetByteCount(texto) > LimiteTelefoneCifrado - TamanhoNonce - TamanhoTag)
        {
            throw new ErroDeNegocio("Telefone muito longo.");
        }

        return Cifrar(texto, LimiteTelefoneCifrado);
    }


    public static string DecifrarTelefone(byte[] dado)
    {
        return Decifrar(dado);
    }   
    
     
    public static string Decifrar(byte[] dados)
    {
        ArgumentNullException.ThrowIfNull(dados);

        if (dados.Length < TamanhoNonce + TamanhoTag)
        {
            throw new CryptographicException("Dado cifrado inválido.");
        }

        int tamanhoCifrado = dados.Length - TamanhoNonce - TamanhoTag;

        ReadOnlySpan<byte> nonce = dados.AsSpan(0, TamanhoNonce);
        ReadOnlySpan<byte> cifrado = dados.AsSpan(TamanhoNonce, tamanhoCifrado);
        ReadOnlySpan<byte> tag = dados.AsSpan(TamanhoNonce + tamanhoCifrado, TamanhoTag);

        byte[] chave = CarregarChave(VarChaveAes);
        byte[] plano = new byte[tamanhoCifrado];

        try
        {
            using var aes = new AesGcm(chave, TamanhoTag);
            aes.Decrypt(nonce, cifrado, tag, plano);

            return Encoding.UTF8.GetString(plano);
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("Falha ao decifrar: dado alterado ou chave incorreta.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(chave);
            CryptographicOperations.ZeroMemory(plano);
        }
    }
}
