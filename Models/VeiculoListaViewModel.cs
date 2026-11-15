namespace AutoManager.Models;

public class VeiculoListaViewModel
{
    public Guid Id { get; set; }

    public string Placa { get; set; } = string.Empty;

    public string Marca { get; set; } = string.Empty;

    public string Modelo { get; set; } = string.Empty;

    public string NomeCliente { get; set; } = string.Empty;
}
