namespace AutoManager.Services;

/// <summary>
/// Resultado de uma operação de negócio. Em caso de falha, traz a mensagem
/// geral e, quando aplicável, erros associados a campos do formulário.
/// </summary>
public class Resultado
{
    public bool Sucesso { get; protected init; }
    public string? Mensagem { get; protected init; }
    public Dictionary<string, string> ErrosCampos { get; } = new();

    public static Resultado Ok(string? mensagem = null) => new() { Sucesso = true, Mensagem = mensagem };

    public static Resultado Falha(string mensagem) => new() { Sucesso = false, Mensagem = mensagem };

    public static Resultado FalhaCampo(string campo, string mensagem)
    {
        var r = new Resultado { Sucesso = false, Mensagem = mensagem };
        r.ErrosCampos[campo] = mensagem;
        return r;
    }
}

public class Resultado<T> : Resultado
{
    public T? Valor { get; private init; }

    public static Resultado<T> Ok(T valor, string? mensagem = null) =>
        new() { Sucesso = true, Valor = valor, Mensagem = mensagem };

    public new static Resultado<T> Falha(string mensagem) => new() { Sucesso = false, Mensagem = mensagem };

    public new static Resultado<T> FalhaCampo(string campo, string mensagem)
    {
        var r = new Resultado<T> { Sucesso = false, Mensagem = mensagem };
        r.ErrosCampos[campo] = mensagem;
        return r;
    }
}
