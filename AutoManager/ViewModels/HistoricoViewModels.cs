namespace AutoManager.ViewModels;

public record HistoricoVeiculoItem(VeiculoResumo Veiculo, int Concluidas, DateTime? UltimoServico);

public class HistoricoListaViewModel
{
    public string? Busca { get; set; }
    public List<HistoricoVeiculoItem> Itens { get; set; } = [];
    public int TotalOrdens { get; set; }
    public int TotalConcluidas { get; set; }
    public List<OrdemResumo> UltimasConcluidas { get; set; } = [];
}
