using System.Globalization;
using System.Text.RegularExpressions;

namespace AutoManager.Infrastructure;

/// <summary>Formatação e normalização de dados exibidos/recebidos pela interface.</summary>
public static partial class Formatacao
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    /// <summary>Remove tudo que não for dígito.</summary>
    public static string SomenteDigitos(string? valor) =>
        string.IsNullOrEmpty(valor) ? string.Empty : new string(valor.Where(char.IsDigit).ToArray());

    /// <summary>(16) 99999-9999 para celular e (16) 3333-4444 para fixo.</summary>
    public static string Telefone(string? digitos)
    {
        var d = SomenteDigitos(digitos);
        return d.Length switch
        {
            11 => $"({d[..2]}) {d.Substring(2, 5)}-{d[7..]}",
            10 => $"({d[..2]}) {d.Substring(2, 4)}-{d[6..]}",
            _ => d
        };
    }

    /// <summary>Placa em maiúsculas, sem hífen e sem espaços.</summary>
    public static string NormalizarPlaca(string? placa) =>
        string.IsNullOrWhiteSpace(placa)
            ? string.Empty
            : new string(placa.ToUpperInvariant().Where(char.IsLetterOrDigit).ToArray());

    /// <summary>Formato antigo (AAA9999) ou Mercosul (AAA9A99).</summary>
    public static bool PlacaValida(string? placaNormalizada) =>
        !string.IsNullOrEmpty(placaNormalizada) && PlacaRegex().IsMatch(placaNormalizada);

    /// <summary>Exibe placas no padrão antigo com hífen (ABC-1234); Mercosul sem hífen (ABC1D23).</summary>
    public static string Placa(string? placa)
    {
        var p = NormalizarPlaca(placa);
        return p.Length == 7 && char.IsDigit(p[4]) ? $"{p[..3]}-{p[3..]}" : p;
    }

    public static string Iniciais(string? nome)
    {
        var partes = (nome ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..1].ToUpperInvariant(),
            _ => (partes[0][..1] + partes[^1][..1]).ToUpperInvariant()
        };
    }

    public static string Data(DateTime data) => data.ToString("dd/MM/yyyy", PtBr);

    public static string DataHora(DateTime data) => data.ToString("dd/MM/yyyy 'às' HH:mm", PtBr);

    public static string DataHora(DateTime? data) => data.HasValue ? DataHora(data.Value) : "—";

    /// <summary>Texto relativo curto: "agora", "há 5 min", "há 3 h", "há 2 dias".</summary>
    public static string Relativo(DateTime data)
    {
        var diff = DateTime.Now - data;
        if (diff.TotalMinutes < 1) return "agora";
        if (diff.TotalMinutes < 60) return $"há {(int)diff.TotalMinutes} min";
        if (diff.TotalHours < 24) return $"há {(int)diff.TotalHours} h";
        if (diff.TotalDays < 2) return "ontem";
        if (diff.TotalDays < 30) return $"há {(int)diff.TotalDays} dias";
        return Data(data);
    }

    public static string DataExtenso(DateTime data)
    {
        var texto = data.ToString("dddd, d 'de' MMMM", PtBr);
        return char.ToUpper(texto[0]) + texto[1..];
    }

    [GeneratedRegex("^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$")]
    private static partial Regex PlacaRegex();
}
