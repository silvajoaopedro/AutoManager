using AutoManager.Infrastructure;
using AutoManager.Models.Enums;
using AutoManager.Persistencia;
using AutoManager.ViewModels;

namespace AutoManager.Services;

/// <summary>
/// Histórico de serviços: gerado a partir das ordens armazenadas,
/// agrupadas por veículo.
/// </summary>
public class HistoricoService
{
    private readonly JsonDataStore _dados;

    public HistoricoService(JsonDataStore dados) => _dados = dados;

    public Task<HistoricoListaViewModel> ListarAsync(string? busca) => _dados.LerAsync(s =>
    {
        var termo = busca?.Trim() ?? string.Empty;
        var termoPlaca = Formatacao.NormalizarPlaca(termo);
        var ordensPorVeiculo = s.Ordens.GroupBy(o => o.VeiculoId).ToDictionary(g => g.Key, g => g.ToList());

        var itens = Projecoes.Veiculos(s, s.Veiculos.Where(v => ordensPorVeiculo.ContainsKey(v.Id)))
            .Where(v => termo.Length == 0
                || (termoPlaca.Length >= 2 && v.Placa.Contains(termoPlaca))
                || v.Descricao.Contains(termo, StringComparison.CurrentCultureIgnoreCase)
                || v.ClienteNome.Contains(termo, StringComparison.CurrentCultureIgnoreCase))
            .Select(v =>
            {
                var ordens = ordensPorVeiculo[v.Id];
                return new HistoricoVeiculoItem(v,
                    ordens.Count(o => o.Status == StatusOrdem.Concluida),
                    ordens.Max(o => o.DataConclusao ?? o.DataInicio ?? o.DataAbertura));
            })
            .OrderByDescending(i => i.Veiculo.Placa == termoPlaca)
            .ThenByDescending(i => i.UltimoServico)
            .ToList();

        return new HistoricoListaViewModel
        {
            Busca = busca,
            Itens = itens,
            TotalOrdens = s.Ordens.Count,
            TotalConcluidas = s.Ordens.Count(o => o.Status == StatusOrdem.Concluida),
            UltimasConcluidas = Projecoes.Ordens(s, s.Ordens
                .Where(o => o.Status == StatusOrdem.Concluida)
                .OrderByDescending(o => o.DataConclusao)
                .Take(6))
        };
    });
}
