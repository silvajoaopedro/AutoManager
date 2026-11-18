using AutoManager.Models;

namespace AutoManager.Data;

/// <summary>
/// Unidade de trabalho sobre os arquivos JSON. Cada coleção é carregada do disco
/// sob demanda; em uma sessão de escrita, as coleções carregadas são gravadas ao final.
/// Uma sessão só existe enquanto o <see cref="JsonDataStore"/> mantém o bloqueio,
/// o que garante que verificações de integridade e gravações ocorram sem concorrência.
/// </summary>
public sealed class DataSession
{
    public const string ArquivoUsuarios = "usuarios.json";
    public const string ArquivoClientes = "clientes.json";
    public const string ArquivoVeiculos = "veiculos.json";
    public const string ArquivoOrdens = "ordens.json";
    public const string ArquivoSequencias = "sequencias.json";

    private readonly string _pasta;
    private readonly Dictionary<string, object> _carregados = new();

    internal DataSession(string pasta) => _pasta = pasta;

    public List<Usuario> Usuarios => Obter<List<Usuario>>(ArquivoUsuarios);
    public List<Cliente> Clientes => Obter<List<Cliente>>(ArquivoClientes);
    public List<Veiculo> Veiculos => Obter<List<Veiculo>>(ArquivoVeiculos);
    public List<OrdemServico> Ordens => Obter<List<OrdemServico>>(ArquivoOrdens);
    private Sequencias Sequencias => Obter<Sequencias>(ArquivoSequencias);

    public int ProximoIdUsuario() =>
        Sequencias.Usuario = Math.Max(Sequencias.Usuario, Usuarios.Select(u => u.Id).DefaultIfEmpty().Max()) + 1;

    public int ProximoIdCliente() =>
        Sequencias.Cliente = Math.Max(Sequencias.Cliente, Clientes.Select(c => c.Id).DefaultIfEmpty().Max()) + 1;

    public int ProximoIdVeiculo() =>
        Sequencias.Veiculo = Math.Max(Sequencias.Veiculo, Veiculos.Select(v => v.Id).DefaultIfEmpty().Max()) + 1;

    public int ProximoNumeroOrdem() =>
        Sequencias.OrdemServico = Math.Max(Sequencias.OrdemServico, Ordens.Select(o => o.Numero).DefaultIfEmpty().Max()) + 1;

    private T Obter<T>(string arquivo) where T : new()
    {
        if (!_carregados.TryGetValue(arquivo, out var dados))
        {
            dados = JsonFile.Ler<T>(Path.Combine(_pasta, arquivo));
            _carregados[arquivo] = dados;
        }
        return (T)dados;
    }

    internal void SalvarAlteracoes()
    {
        foreach (var (arquivo, dados) in _carregados)
            JsonFile.Gravar(Path.Combine(_pasta, arquivo), dados);
    }
}
