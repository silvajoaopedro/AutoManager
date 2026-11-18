namespace AutoManager.Models;

public class Veiculo
{
    public int Id { get; set; }

    /// <summary>Placa normalizada: maiúsculas, sem hífen (ex.: ABC1234 ou ABC1D23). Única.</summary>
    public string Placa { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public int? Ano { get; set; }

    public string? Cor { get; set; }

    /// <summary>Cliente proprietário.</summary>
    public int ClienteId { get; set; }

    public DateTime DataCadastro { get; set; }

    public DateTime? DataAtualizacao { get; set; }
}
