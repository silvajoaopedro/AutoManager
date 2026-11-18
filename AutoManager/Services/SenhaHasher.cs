using System.Security.Cryptography;

namespace AutoManager.Services;

/// <summary>
/// Geração e verificação de hash de senha com PBKDF2 (HMAC-SHA256) e salt aleatório.
/// A senha em texto puro nunca é armazenada.
/// </summary>
public static class SenhaHasher
{
    private const int TamanhoSalt = 16;   // 128 bits
    private const int TamanhoHash = 32;   // 256 bits
    private const int Iteracoes = 100_000;
    private static readonly HashAlgorithmName Algoritmo = HashAlgorithmName.SHA256;

    public static (string Hash, string Salt) GerarHash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, Algoritmo, TamanhoHash);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    public static bool Verificar(string senha, string hashBase64, string saltBase64)
    {
        if (string.IsNullOrEmpty(hashBase64) || string.IsNullOrEmpty(saltBase64))
            return false;

        try
        {
            var salt = Convert.FromBase64String(saltBase64);
            var esperado = Convert.FromBase64String(hashBase64);
            var calculado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, Algoritmo, esperado.Length);
            // Comparação em tempo constante evita ataques de temporização
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
