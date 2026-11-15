using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace AutoManager.Models;

public class Usuario
{
    private static readonly PasswordHasher<Usuario> Hasher = new();

    public Guid Id { get; }
    public string Login { get; }
    public string SenhaHash { get; }
    public PerfilUsuario Perfil { get; }

    // Cria uma conta e transforma a senha em hash.
    public Usuario(
        string login,
        string senha,
        PerfilUsuario perfil)
    {
        ValidarLoginEPerfil(login, perfil);

        if (string.IsNullOrWhiteSpace(senha))
        {
            throw new ArgumentException(
                "A senha é obrigatória.");
        }

        Id = Guid.NewGuid();
        Login = login.Trim();
        Perfil = perfil;
        SenhaHash = Hasher.HashPassword(this, senha);
    }

    // Recupera uma conta do JSON sem gerar outro hash.
    [JsonConstructor]
    public Usuario(
        Guid id,
        string login,
        string senhaHash,
        PerfilUsuario perfil)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "O identificador do usuário é obrigatório.");
        }

        ValidarLoginEPerfil(login, perfil);

        if (string.IsNullOrWhiteSpace(senhaHash))
        {
            throw new ArgumentException(
                "O hash da senha é obrigatório.");
        }

        Id = id;
        Login = login.Trim();
        SenhaHash = senhaHash;
        Perfil = perfil;
    }

    public bool VerificarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha))
        {
            return false;
        }

        var resultado = Hasher.VerifyHashedPassword(
            this,
            SenhaHash,
            senha);

        return resultado == PasswordVerificationResult.Success
            || resultado ==
                PasswordVerificationResult.SuccessRehashNeeded;
    }

    private static void ValidarLoginEPerfil(
        string login,
        PerfilUsuario perfil)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            throw new ArgumentException(
                "O login é obrigatório.");
        }

        if (!Enum.IsDefined(typeof(PerfilUsuario), perfil))
        {
            throw new ArgumentException(
                "O perfil do usuário é inválido.");
        }
    }
}