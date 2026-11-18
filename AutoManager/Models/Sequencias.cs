namespace AutoManager.Models;

/// <summary>
/// Últimos identificadores emitidos. Mantidos em arquivo próprio para que
/// um identificador nunca seja reutilizado, mesmo após exclusões (RN08).
/// </summary>
public class Sequencias
{
    public int Usuario { get; set; }
    public int Cliente { get; set; }
    public int Veiculo { get; set; }
    public int OrdemServico { get; set; }
}
