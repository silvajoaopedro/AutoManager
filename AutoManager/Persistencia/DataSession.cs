using AutoManager.Models;

namespace AutoManager.Persistencia;

/// <summary>Estado carregado para uma operação protegida pelo bloqueio do JsonDataStore.</summary>
public sealed class DataSession
{
    public const string ArquivoEstado = "oficina.json";

    private readonly string _caminho;
    private readonly EstadoOficina _estado;

    internal DataSession(string pasta)
    {
        _caminho = Path.Combine(pasta, ArquivoEstado);
        _estado = File.Exists(_caminho)
            ? JsonFile.Ler<EstadoOficina>(_caminho)
            : new EstadoOficina
            {
                Usuarios = JsonFile.Ler<List<Usuario>>(Path.Combine(pasta, "usuarios.json")),
                Clientes = JsonFile.Ler<List<Cliente>>(Path.Combine(pasta, "clientes.json")),
                Veiculos = JsonFile.Ler<List<Veiculo>>(Path.Combine(pasta, "veiculos.json")),
                Ordens = JsonFile.Ler<List<OrdemServico>>(Path.Combine(pasta, "ordens.json")),
                Sequencias = JsonFile.Ler<Sequencias>(Path.Combine(pasta, "sequencias.json"))
            };
    }

    public List<Usuario> Usuarios => _estado.Usuarios;
    public List<Cliente> Clientes => _estado.Clientes;
    public List<Veiculo> Veiculos => _estado.Veiculos;
    public List<OrdemServico> Ordens => _estado.Ordens;
    private Sequencias Sequencias => _estado.Sequencias;

    public int ProximoIdUsuario() =>
        Sequencias.Usuario = Math.Max(Sequencias.Usuario, Usuarios.Select(u => u.Id).DefaultIfEmpty().Max()) + 1;

    public int ProximoIdCliente() =>
        Sequencias.Cliente = Math.Max(Sequencias.Cliente, Clientes.Select(c => c.Id).DefaultIfEmpty().Max()) + 1;

    public int ProximoIdVeiculo() =>
        Sequencias.Veiculo = Math.Max(Sequencias.Veiculo, Veiculos.Select(v => v.Id).DefaultIfEmpty().Max()) + 1;

    public int ProximoNumeroOrdem() =>
        Sequencias.OrdemServico = Math.Max(Sequencias.OrdemServico, Ordens.Select(o => o.Numero).DefaultIfEmpty().Max()) + 1;

    /// <summary>Número que a próxima ordem receberá, sem reservá-lo.</summary>
    public int ConsultarProximoNumeroOrdem() =>
        Math.Max(Sequencias.OrdemServico, Ordens.Select(o => o.Numero).DefaultIfEmpty().Max()) + 1;

    internal void SalvarAlteracoes() => JsonFile.Gravar(_caminho, _estado);
}

internal sealed class EstadoOficina
{
    public List<Usuario> Usuarios { get; set; } = [];
    public List<Cliente> Clientes { get; set; } = [];
    public List<Veiculo> Veiculos { get; set; } = [];
    public List<OrdemServico> Ordens { get; set; } = [];
    public Sequencias Sequencias { get; set; } = new();
}
