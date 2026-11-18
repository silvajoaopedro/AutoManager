namespace AutoManager.Models;

public record PainelOficinaViewModel(
    int Pendentes,
    int EmAndamento,
    int Concluidas,
    IReadOnlyList<OrdemServico> OrdensRecentes,
    IReadOnlyDictionary<Guid, Veiculo> Veiculos,
    IReadOnlyDictionary<Guid, Cliente> Clientes,
    IReadOnlyList<int> ConclusoesPorDia,
    DateOnly InicioPeriodo);
