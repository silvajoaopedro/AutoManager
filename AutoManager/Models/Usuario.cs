using AutoManager.Models.Enums;

namespace AutoManager.Models;

public class Usuario
{
    public int Id { get; set; }

    /// <summary>Nome de usuário utilizado no login (único, sem diferenciar maiúsculas).</summary>
    public string Login { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public PerfilUsuario Perfil { get; set; }

    /// <summary>Hash PBKDF2-SHA256 da senha, em Base64.</summary>
    public string SenhaHash { get; set; } = string.Empty;

    /// <summary>Salt aleatório da senha, em Base64.</summary>
    public string SenhaSalt { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; }
}
