namespace AutoManager.Models.Enums;

/// <summary>
/// Status possíveis de uma ordem de serviço.
/// A sequência obrigatória é Pendente → EmAndamento → Concluida (RN11).
/// </summary>
public enum StatusOrdem
{
    Pendente = 1,
    EmAndamento = 2,
    Concluida = 3
}

public static class StatusOrdemExtensions
{
    public static string Nome(this StatusOrdem status) => status switch
    {
        StatusOrdem.Pendente => "Pendente",
        StatusOrdem.EmAndamento => "Em andamento",
        StatusOrdem.Concluida => "Concluída",
        _ => status.ToString()
    };

    /// <summary>Classe CSS usada nos badges de status.</summary>
    public static string Css(this StatusOrdem status) => status switch
    {
        StatusOrdem.Pendente => "status-pendente",
        StatusOrdem.EmAndamento => "status-andamento",
        StatusOrdem.Concluida => "status-concluida",
        _ => string.Empty
    };

    /// <summary>Ícone (Bootstrap Icons) associado ao status.</summary>
    public static string Icone(this StatusOrdem status) => status switch
    {
        StatusOrdem.Pendente => "bi-hourglass-split",
        StatusOrdem.EmAndamento => "bi-gear-wide-connected",
        StatusOrdem.Concluida => "bi-check-circle-fill",
        _ => "bi-circle"
    };

    /// <summary>Próximo status permitido; nulo quando a ordem já está concluída.</summary>
    public static StatusOrdem? Proximo(this StatusOrdem status) => status switch
    {
        StatusOrdem.Pendente => StatusOrdem.EmAndamento,
        StatusOrdem.EmAndamento => StatusOrdem.Concluida,
        _ => null
    };
}
