using AutoManager.Infrastructure;
using AutoManager.Models.Enums;
using AutoManager.Persistencia;
using AutoManager.Services;
using AutoManager.ViewModels;

namespace AutoManager.Tests;

/// <summary>Base dos testes de serviço: cada teste usa uma pasta de dados temporária e vazia.</summary>
public abstract class ContextoServicos : IAsyncLifetime
{
    protected string Pasta = string.Empty;
    protected JsonDataStore Dados = null!;
    protected ClienteService Clientes = null!;
    protected VeiculoService Veiculos = null!;
    protected OrdemServicoService Ordens = null!;
    protected DashboardService Dashboard = null!;
    protected HistoricoService Historico = null!;

    protected static readonly UsuarioLogado Atendente = new(1, "Atendente Teste", PerfilUsuario.Atendente);
    protected static readonly UsuarioLogado Mecanico = new(2, "Mecânico Teste", PerfilUsuario.Mecanico);

    public Task IniciarAsync()
    {
        Pasta = Path.Combine(Path.GetTempPath(), "automanager-testes", Guid.NewGuid().ToString("N"));
        Recriar();
        return Task.CompletedTask;
    }

    public Task FinalizarAsync()
    {
        try { Directory.Delete(Pasta, recursive: true); } catch { /* pasta temporária */ }
        return Task.CompletedTask;
    }

    /// <summary>Simula a reinicialização da aplicação: novos serviços lendo a mesma pasta.</summary>
    protected void Recriar()
    {
        Dados = new JsonDataStore(Pasta);
        Clientes = new ClienteService(Dados);
        Veiculos = new VeiculoService(Dados);
        Ordens = new OrdemServicoService(Dados);
        Dashboard = new DashboardService(Dados);
        Historico = new HistoricoService(Dados);
    }

    protected async Task<int> NovoClienteAsync(string nome = "Maria Aparecida", string telefone = "(16) 99999-1234")
    {
        var r = await Clientes.CriarAsync(new ClienteFormViewModel { Nome = nome, Telefone = telefone });
        Verificar.Verdadeiro(r.Sucesso, "Falha ao preparar cliente: " + r.Mensagem);
        return r.Valor!.Id;
    }

    protected async Task<int> NovoVeiculoAsync(int clienteId, string placa = "ABC1D23")
    {
        var r = await Veiculos.CriarAsync(new VeiculoFormViewModel { ClienteId = clienteId, Placa = placa, Marca = "Volkswagen", Modelo = "Gol" });
        Verificar.Verdadeiro(r.Sucesso, "Falha ao preparar veículo: " + r.Mensagem);
        return r.Valor!.Id;
    }

    protected async Task<int> NovaOrdemAsync(int veiculoId, string descricao = "Barulho na suspensão dianteira.")
    {
        var r = await Ordens.AbrirAsync(new AbrirOrdemViewModel { VeiculoId = veiculoId, DescricaoProblema = descricao }, Atendente);
        Verificar.Verdadeiro(r.Sucesso, "Falha ao preparar ordem: " + r.Mensagem);
        return r.Valor!.Numero;
    }
}
