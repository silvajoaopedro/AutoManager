using AutoManager.Models.Enums;
using AutoManager.Persistencia;
using AutoManager.ViewModels;

namespace AutoManager.Services;

/// <summary>Indicadores operacionais calculados a partir dos dados reais dos arquivos JSON.</summary>
public class DashboardService
{
    private readonly JsonDataStore _dados;

    public DashboardService(JsonDataStore dados) => _dados = dados;

    public Task<DashboardViewModel> ObterAsync() => _dados.LerAsync(s =>
    {
        var hoje = DateTime.Today;
        return new DashboardViewModel
        {
            TotalOrdens = s.Ordens.Count,
            Pendentes = s.Ordens.Count(o => o.Status == StatusOrdem.Pendente),
            EmAndamento = s.Ordens.Count(o => o.Status == StatusOrdem.EmAndamento),
            Concluidas = s.Ordens.Count(o => o.Status == StatusOrdem.Concluida),
            ConcluidasHoje = s.Ordens.Count(o => o.DataConclusao?.Date == hoje),
            TotalClientes = s.Clientes.Count,
            TotalVeiculos = s.Veiculos.Count,
            Recentes = Projecoes.Ordens(s, s.Ordens.OrderByDescending(o => o.Numero).Take(8)),
            FilaPendentes = Projecoes.Ordens(s, s.Ordens
                .Where(o => o.Status == StatusOrdem.Pendente)
                .OrderBy(o => o.DataAbertura)
                .Take(6)),
            EmExecucao = Projecoes.Ordens(s, s.Ordens
                .Where(o => o.Status == StatusOrdem.EmAndamento)
                .OrderBy(o => o.DataAbertura)
                .Take(6))
        };
    });
}
