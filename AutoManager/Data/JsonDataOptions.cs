namespace AutoManager.Data;

public class JsonDataOptions
{
    public const string Secao = "Persistencia";

    /// <summary>
    /// Pasta onde ficam os arquivos JSON. Caminho relativo é resolvido a partir
    /// da raiz do projeto (ContentRoot) — nunca dentro de wwwroot.
    /// </summary>
    public string Pasta { get; set; } = "Data";
}
