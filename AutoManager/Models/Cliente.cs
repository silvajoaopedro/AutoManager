namespace AutoManager.Models;

public class Cliente
{
    public int Id { get; set; }

    public string Nome { get; set; } = string.Empty;

    /// <summary>Telefone armazenado apenas com dígitos (DDD + número).</summary>
    public string Telefone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public DateTime DataCadastro { get; set; }

    public DateTime? DataAtualizacao { get; set; }
}
