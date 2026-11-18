using AutoManager.Infrastructure;
using AutoManager.Models;
using AutoManager.Persistencia;
using AutoManager.ViewModels;

namespace AutoManager.Services;

public class VeiculoService
{
    private readonly JsonDataStore _dados;

    public VeiculoService(JsonDataStore dados) => _dados = dados;

    /// <summary>Lista veículos filtrando por placa, marca, modelo ou nome do proprietário.</summary>
    public Task<VeiculoListaViewModel> ListarAsync(string? busca) => _dados.LerAsync(s =>
    {
        var termo = busca?.Trim() ?? string.Empty;
        var termoPlaca = Formatacao.NormalizarPlaca(termo);

        var itens = Projecoes.Veiculos(s, s.Veiculos)
            .Where(v => termo.Length == 0
                || (termoPlaca.Length >= 2 && v.Placa.Contains(termoPlaca))
                || v.Marca.Contains(termo, StringComparison.CurrentCultureIgnoreCase)
                || v.Modelo.Contains(termo, StringComparison.CurrentCultureIgnoreCase)
                || v.Descricao.Contains(termo, StringComparison.CurrentCultureIgnoreCase)
                || v.ClienteNome.Contains(termo, StringComparison.CurrentCultureIgnoreCase))
            // Placa idêntica à pesquisada aparece primeiro
            .OrderByDescending(v => v.Placa == termoPlaca)
            .ThenBy(v => v.Placa)
            .ToList();

        return new VeiculoListaViewModel { Busca = busca, Itens = itens, Total = s.Veiculos.Count };
    });

    public Task<Veiculo?> ObterAsync(int id) =>
        _dados.LerAsync(s => s.Veiculos.FirstOrDefault(v => v.Id == id));

    public Task<Veiculo?> ObterPorPlacaAsync(string placa)
    {
        var normalizada = Formatacao.NormalizarPlaca(placa);
        return _dados.LerAsync(s => s.Veiculos.FirstOrDefault(v => v.Placa == normalizada));
    }

    public Task<VeiculoDetalhesViewModel?> ObterDetalhesAsync(int id) => _dados.LerAsync(s =>
    {
        var veiculo = s.Veiculos.FirstOrDefault(v => v.Id == id);
        if (veiculo is null) return null;

        return new VeiculoDetalhesViewModel
        {
            Veiculo = veiculo,
            Cliente = s.Clientes.FirstOrDefault(c => c.Id == veiculo.ClienteId),
            Historico = s.Ordens.Where(o => o.VeiculoId == id).OrderByDescending(o => o.Numero).ToList(),
            BloqueioExclusao = MotivoBloqueioExclusao(s, id)
        };
    });

    public Task<VeiculoExclusaoViewModel?> ObterParaExclusaoAsync(int id) => _dados.LerAsync(s =>
    {
        var veiculo = s.Veiculos.FirstOrDefault(v => v.Id == id);
        if (veiculo is null) return null;
        return new VeiculoExclusaoViewModel
        {
            Veiculo = veiculo,
            Cliente = s.Clientes.FirstOrDefault(c => c.Id == veiculo.ClienteId),
            QuantidadeOrdens = s.Ordens.Count(o => o.VeiculoId == id),
            BloqueioExclusao = MotivoBloqueioExclusao(s, id)
        };
    });

    public Task<Resultado<Veiculo>> CriarAsync(VeiculoFormViewModel form) => _dados.EscreverAsync(s =>
    {
        var erro = Validar(s, form, idAtual: null, out var dados);
        if (erro is not null) return erro;

        dados.Id = s.ProximoIdVeiculo();
        dados.DataCadastro = DateTime.Now;
        s.Veiculos.Add(dados);
        return Resultado<Veiculo>.Ok(dados, $"Veículo {Formatacao.Placa(dados.Placa)} cadastrado com sucesso.");
    }, r => r.Sucesso);

    public Task<Resultado<Veiculo>> AtualizarAsync(int id, VeiculoFormViewModel form) => _dados.EscreverAsync(s =>
    {
        var veiculo = s.Veiculos.FirstOrDefault(v => v.Id == id);
        if (veiculo is null) return Resultado<Veiculo>.Falha("Veículo não encontrado.");

        var erro = Validar(s, form, idAtual: id, out var dados);
        if (erro is not null) return erro;

        veiculo.Placa = dados.Placa;
        veiculo.Marca = dados.Marca;
        veiculo.Modelo = dados.Modelo;
        veiculo.Ano = dados.Ano;
        veiculo.Cor = dados.Cor;
        veiculo.ClienteId = dados.ClienteId;
        veiculo.DataAtualizacao = DateTime.Now;
        return Resultado<Veiculo>.Ok(veiculo, "Dados do veículo atualizados.");
    }, r => r.Sucesso);

    /// <summary>Exclui o veículo somente se não houver ordens de serviço vinculadas (RN16).</summary>
    public Task<Resultado> ExcluirAsync(int id) => _dados.EscreverAsync(s =>
    {
        var veiculo = s.Veiculos.FirstOrDefault(v => v.Id == id);
        if (veiculo is null) return Resultado.Falha("Veículo não encontrado.");

        var bloqueio = MotivoBloqueioExclusao(s, id);
        if (bloqueio is not null) return Resultado.Falha(bloqueio);

        s.Veiculos.Remove(veiculo);
        return Resultado.Ok($"Veículo {Formatacao.Placa(veiculo.Placa)} excluído.");
    }, r => r.Sucesso);

    private static string? MotivoBloqueioExclusao(DataSession s, int veiculoId)
    {
        var ordens = s.Ordens.Count(o => o.VeiculoId == veiculoId);
        return ordens > 0
            ? $"Este veículo possui {ordens} ordem(ns) de serviço no histórico. Para preservar o histórico de serviços, ele não pode ser excluído."
            : null;
    }

    private static Resultado<Veiculo>? Validar(DataSession s, VeiculoFormViewModel form, int? idAtual, out Veiculo dados)
    {
        static string Limpar(string? t) => string.Join(' ', (t ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));

        dados = new Veiculo
        {
            Placa = Formatacao.NormalizarPlaca(form.Placa),
            Marca = Limpar(form.Marca),
            Modelo = Limpar(form.Modelo),
            Ano = form.Ano,
            Cor = string.IsNullOrWhiteSpace(form.Cor) ? null : Limpar(form.Cor),
            ClienteId = form.ClienteId
        };

        if (!Formatacao.PlacaValida(dados.Placa))
            return Resultado<Veiculo>.FalhaCampo(nameof(form.Placa), "Placa inválida. Use o padrão ABC-1234 ou Mercosul ABC1D23.");

        var placa = dados.Placa;
        var duplicado = s.Veiculos.FirstOrDefault(v => v.Placa == placa && v.Id != idAtual);
        if (duplicado is not null)
            return Resultado<Veiculo>.FalhaCampo(nameof(form.Placa), $"Já existe um veículo cadastrado com a placa {Formatacao.Placa(placa)}.");

        if (dados.Marca.Length is < 2 or > 40)
            return Resultado<Veiculo>.FalhaCampo(nameof(form.Marca), "Informe a marca (2 a 40 caracteres).");

        if (dados.Modelo.Length is < 1 or > 60)
            return Resultado<Veiculo>.FalhaCampo(nameof(form.Modelo), "Informe o modelo (até 60 caracteres).");

        if (dados.Ano is { } ano && (ano < 1900 || ano > DateTime.Now.Year + 1))
            return Resultado<Veiculo>.FalhaCampo(nameof(form.Ano), $"Informe um ano entre 1900 e {DateTime.Now.Year + 1}.");

        if (dados.Cor is { Length: > 30 })
            return Resultado<Veiculo>.FalhaCampo(nameof(form.Cor), "A cor deve ter no máximo 30 caracteres.");

        var clienteId = dados.ClienteId;
        if (!s.Clientes.Any(c => c.Id == clienteId))
            return Resultado<Veiculo>.FalhaCampo(nameof(form.ClienteId), "Selecione um cliente proprietário cadastrado.");

        return null;
    }
}
