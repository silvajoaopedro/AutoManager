using AutoManager.Models;
using AutoManager.Persistencia;
using AutoManager.ViewModels;

namespace AutoManager.Services;

/// <summary>
/// Monta as visões resumidas cruzando clientes, veículos e ordens
/// dentro de uma mesma sessão de leitura.
/// </summary>
internal static class Projecoes
{
    public static List<OrdemResumo> Ordens(DataSession s, IEnumerable<OrdemServico> ordens)
    {
        var veiculos = s.Veiculos.ToDictionary(v => v.Id);
        var clientes = s.Clientes.ToDictionary(c => c.Id);

        return ordens.Select(o =>
        {
            veiculos.TryGetValue(o.VeiculoId, out var v);
            clientes.TryGetValue(o.ClienteId, out var c);
            return new OrdemResumo(
                o.Numero, o.Status, o.DataAbertura, o.DataConclusao, o.DescricaoProblema,
                o.VeiculoId, v?.Placa ?? "—", v is null ? "Veículo não encontrado" : $"{v.Marca} {v.Modelo}",
                o.ClienteId, c?.Nome ?? "Cliente não encontrado", o.Observacoes.Count);
        }).ToList();
    }

    public static List<VeiculoResumo> Veiculos(DataSession s, IEnumerable<Veiculo> veiculos)
    {
        var clientes = s.Clientes.ToDictionary(c => c.Id);
        var ordensPorVeiculo = s.Ordens.GroupBy(o => o.VeiculoId).ToDictionary(g => g.Key, g => g.ToList());

        return veiculos.Select(v =>
        {
            clientes.TryGetValue(v.ClienteId, out var c);
            ordensPorVeiculo.TryGetValue(v.Id, out var ordens);
            return new VeiculoResumo(
                v.Id, v.Placa, v.Marca, v.Modelo, v.Ano, v.Cor, v.ClienteId,
                c?.Nome ?? "Cliente não encontrado",
                ordens?.Count ?? 0,
                ordens?.Count(o => o.Status != Models.Enums.StatusOrdem.Concluida) ?? 0);
        }).ToList();
    }
}
