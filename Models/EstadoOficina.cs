namespace AutoManager.Models;

public class EstadoOficina
{
    public List<Cliente> Clientes { get; set; } = new();
    public List<Veiculo> Veiculos { get; set; } = new();
    public List<OrdemServico> Ordens { get; set; } = new();

    public long UltimoNumeroOrdem { get; set; }
}