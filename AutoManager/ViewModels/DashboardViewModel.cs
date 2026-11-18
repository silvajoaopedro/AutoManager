namespace AutoManager.ViewModels;

public class DashboardViewModel
{
    public int TotalOrdens { get; init; }
    public int Pendentes { get; init; }
    public int EmAndamento { get; init; }
    public int Concluidas { get; init; }
    public int ConcluidasHoje { get; init; }
    public int TotalClientes { get; init; }
    public int TotalVeiculos { get; init; }

    public List<OrdemResumo> Recentes { get; init; } = [];

    /// <summary>Ordens pendentes, da mais antiga para a mais nova (fila de atendimento).</summary>
    public List<OrdemResumo> FilaPendentes { get; init; } = [];

    public List<OrdemResumo> EmExecucao { get; init; } = [];
}
