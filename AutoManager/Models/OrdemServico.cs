using AutoManager.Models.Enums;

namespace AutoManager.Models;

public class OrdemServico
{
    /// <summary>Número da ordem, gerado automaticamente e nunca reutilizado (RN08).</summary>
    public int Numero { get; set; }

    public int ClienteId { get; set; }

    /// <summary>Veículo atendido (RN07).</summary>
    public int VeiculoId { get; set; }

    public DateTime DataAbertura { get; set; }

    public string DescricaoProblema { get; set; } = string.Empty;

    public StatusOrdem Status { get; set; } = StatusOrdem.Pendente;

    public int AbertaPorId { get; set; }
    public string AbertaPorNome { get; set; } = string.Empty;

    public DateTime? DataInicio { get; set; }
    public int? IniciadaPorId { get; set; }
    public string? IniciadaPorNome { get; set; }

    public DateTime? DataConclusao { get; set; }
    public int? ConcluidaPorId { get; set; }
    public string? ConcluidaPorNome { get; set; }

    public List<Observacao> Observacoes { get; set; } = [];

    public string NumeroFormatado => FormatarNumero(Numero);

    public static string FormatarNumero(int numero) => $"OS-{numero:D5}";
}
