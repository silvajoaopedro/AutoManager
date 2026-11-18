namespace AutoManager.Models.Enums;

/// <summary>
/// Perfis de acesso do sistema.
/// O nome do valor é usado como "role" na autenticação.
/// </summary>
public enum PerfilUsuario
{
    Atendente = 1,
    Mecanico = 2
}

public static class Perfis
{
    public const string Atendente = nameof(PerfilUsuario.Atendente);
    public const string Mecanico = nameof(PerfilUsuario.Mecanico);

    /// <summary>Perfis que podem atualizar status e registrar observações.</summary>
    public const string Todos = Atendente + "," + Mecanico;

    public static string Nome(this PerfilUsuario perfil) => perfil switch
    {
        PerfilUsuario.Atendente => "Atendente",
        PerfilUsuario.Mecanico => "Mecânico",
        _ => perfil.ToString()
    };
}
