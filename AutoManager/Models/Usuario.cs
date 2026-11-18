using AutoManager.Models.Enums;
using System.Text.Json.Serialization;

namespace AutoManager.Models;

public class Usuario
{
    public int Id { get; set; }

    /// <summary>Nome de usuário utilizado no login (único, sem diferenciar maiúsculas).</summary>
    public string Login { get; set; } = string.Empty;

    public string Nome { get; set; } = string.Empty;

    public PerfilUsuario Perfil { get; set; }

    /// <summary>Hash de senha no formato do ASP.NET Core Identity.</summary>
    public string SenhaHash { get; set; } = string.Empty;

    /// <summary>Salt separado, presente apenas nos registros legados.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SenhaSalt { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime DataCriacao { get; set; }
}
