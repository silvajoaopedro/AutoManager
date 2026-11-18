namespace AutoManager.Models;

public record PainelOficinaViewModel(
    int Pendentes,
    int EmAndamento,
    int Concluidas,
    IReadOnlyList<OrdemServico> OrdensRecentes,
    IReadOnlyDictionary<Guid, Veiculo> Veiculos,
    IReadOnlyList<int> ConclusoesPorDia,
    DateOnly InicioPeriodo);
