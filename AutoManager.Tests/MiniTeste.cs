using System.Diagnostics;
using System.Reflection;

namespace AutoManager.Tests;

/// <summary>Marca um método público assíncrono como caso de teste.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class TesteAttribute(string descricao) : Attribute
{
    public string Descricao { get; } = descricao;
}

public class FalhaDeTeste(string mensagem) : Exception(mensagem);

public static class Verificar
{
    public static void Verdadeiro(bool condicao, string mensagem)
    {
        if (!condicao) throw new FalhaDeTeste(mensagem);
    }

    public static void Falso(bool condicao, string mensagem) => Verdadeiro(!condicao, mensagem);

    public static void Igual<T>(T esperado, T obtido, string contexto)
    {
        if (!EqualityComparer<T>.Default.Equals(esperado, obtido))
            throw new FalhaDeTeste($"{contexto}: esperado '{esperado}', obtido '{obtido}'.");
    }

    public static void Contem(string trecho, string? texto, string contexto)
    {
        if (texto is null || !texto.Contains(trecho, StringComparison.OrdinalIgnoreCase))
            throw new FalhaDeTeste($"{contexto}: o texto não contém '{trecho}'.");
    }

    public static async Task<TExcecao> LancaAsync<TExcecao>(Func<Task> acao, string contexto) where TExcecao : Exception
    {
        try { await acao(); }
        catch (TExcecao ex) { return ex; }
        throw new FalhaDeTeste($"{contexto}: era esperada a exceção {typeof(TExcecao).Name}.");
    }
}

/// <summary>Executor simples: encontra métodos [Teste], executa e resume o resultado.</summary>
public static class Executor
{
    public static async Task<int> ExecutarAsync(params Type[] classes)
    {
        int ok = 0, falhas = 0;
        var cronometro = Stopwatch.StartNew();

        foreach (var classe in classes)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"■ {classe.Name}");
            Console.ResetColor();

            foreach (var metodo in classe.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var atributo = metodo.GetCustomAttribute<TesteAttribute>();
                if (atributo is null) continue;

                var instancia = Activator.CreateInstance(classe)!;
                try
                {
                    if (instancia is IAsyncLifetime vida) await vida.IniciarAsync();
                    await (Task)metodo.Invoke(instancia, null)!;
                    ok++;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("  ✔ ");
                    Console.ResetColor();
                    Console.WriteLine(atributo.Descricao);
                }
                catch (Exception ex)
                {
                    falhas++;
                    var erro = ex is TargetInvocationException { InnerException: not null } tie ? tie.InnerException! : ex;
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  ✘ {atributo.Descricao}");
                    Console.WriteLine($"      {erro.GetType().Name}: {erro.Message}");
                    Console.ResetColor();
                }
                finally
                {
                    if (instancia is IAsyncLifetime vida) await vida.FinalizarAsync();
                }
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = falhas == 0 ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"Resultado: {ok} aprovado(s), {falhas} reprovado(s) em {cronometro.Elapsed.TotalSeconds:0.0}s.");
        Console.ResetColor();
        return falhas == 0 ? 0 : 1;
    }
}

public interface IAsyncLifetime
{
    Task IniciarAsync();
    Task FinalizarAsync();
}
