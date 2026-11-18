using AutoManager.Tests;
using AutoManager.Tests.Testes;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("AutoManager — testes automatizados");

var codigo = await Executor.ExecutarAsync(
    typeof(AutenticacaoTestes),
    typeof(ClienteTestes),
    typeof(VeiculoTestes),
    typeof(OrdemServicoTestes),
    typeof(PersistenciaTestes),
    typeof(FluxoHttpTestes));

try { Directory.Delete(ServidorTeste.PastaDados, recursive: true); } catch { /* pasta temporária */ }
Environment.Exit(codigo);
