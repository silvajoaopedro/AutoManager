using Microsoft.Extensions.Options;

namespace AutoManager.Data;

/// <summary>
/// Ponto único de acesso aos arquivos JSON. Registrado como singleton:
/// um semáforo serializa leituras e gravações, protegendo operações
/// concorrentes de diferentes usuários (requisito de persistência).
/// </summary>
public sealed class JsonDataStore
{
    private readonly SemaphoreSlim _bloqueio = new(1, 1);

    public string Pasta { get; }

    public JsonDataStore(IOptions<JsonDataOptions> opcoes, IWebHostEnvironment ambiente)
        : this(Path.IsPathRooted(opcoes.Value.Pasta)
            ? opcoes.Value.Pasta
            : Path.Combine(ambiente.ContentRootPath, opcoes.Value.Pasta))
    {
    }

    public JsonDataStore(string pasta)
    {
        Pasta = Path.GetFullPath(pasta);
        Directory.CreateDirectory(Pasta);
    }

    /// <summary>Executa uma consulta. Nada é gravado.</summary>
    public async Task<T> LerAsync<T>(Func<DataSession, T> consulta)
    {
        await _bloqueio.WaitAsync();
        try
        {
            return consulta(new DataSession(Pasta));
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    /// <summary>
    /// Executa uma operação de escrita. Se a operação lançar exceção ou pedir
    /// cancelamento (retornando <c>Salvar = false</c>), nada é gravado.
    /// </summary>
    public async Task<T> EscreverAsync<T>(Func<DataSession, T> operacao, Func<T, bool>? deveSalvar = null)
    {
        await _bloqueio.WaitAsync();
        try
        {
            var sessao = new DataSession(Pasta);
            var resultado = operacao(sessao);
            if (deveSalvar?.Invoke(resultado) ?? true)
                sessao.SalvarAlteracoes();
            return resultado;
        }
        finally
        {
            _bloqueio.Release();
        }
    }
}
