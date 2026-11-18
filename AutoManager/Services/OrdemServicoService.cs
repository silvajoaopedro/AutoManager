using AutoManager.Infrastructure;
using AutoManager.Models;
using AutoManager.Models.Enums;
using AutoManager.Persistencia;
using AutoManager.ViewModels;

namespace AutoManager.Services;

public class OrdemServicoService
{
    public const int DescricaoMinimo = 10;
    public const int DescricaoMaximo = 1000;

    private readonly JsonDataStore _dados;

    public OrdemServicoService(JsonDataStore dados) => _dados = dados;

    /// <summary>Lista ordens com filtro por status e busca por número, placa, veículo, cliente ou problema.</summary>
    public Task<OrdemListaViewModel> ListarAsync(StatusOrdem? status, string? busca) => _dados.LerAsync(s =>
    {
        var termo = busca?.Trim() ?? string.Empty;
        var termoPlaca = Formatacao.NormalizarPlaca(termo);
        var termoNumero = int.TryParse(Formatacao.SomenteDigitos(termo), out var n) ? n : (int?)null;

        var itens = Projecoes.Ordens(s, s.Ordens)
            .Where(o => status is null || o.Status == status)
            .Where(o => termo.Length == 0
                || (termoNumero.HasValue && o.Numero == termoNumero)
                || (termoPlaca.Length >= 3 && o.Placa.Contains(termoPlaca))
                || o.ClienteNome.Contains(termo, StringComparison.CurrentCultureIgnoreCase)
                || o.Veiculo.Contains(termo, StringComparison.CurrentCultureIgnoreCase)
                || o.DescricaoProblema.Contains(termo, StringComparison.CurrentCultureIgnoreCase))
            .OrderByDescending(o => o.Numero)
            .ToList();

        return new OrdemListaViewModel
        {
            Status = status,
            Busca = busca,
            Itens = itens,
            Total = s.Ordens.Count,
            Pendentes = s.Ordens.Count(o => o.Status == StatusOrdem.Pendente),
            EmAndamento = s.Ordens.Count(o => o.Status == StatusOrdem.EmAndamento),
            Concluidas = s.Ordens.Count(o => o.Status == StatusOrdem.Concluida)
        };
    });

    public Task<OrdemServico?> ObterAsync(int numero) =>
        _dados.LerAsync(s => s.Ordens.FirstOrDefault(o => o.Numero == numero));

    public Task<OrdemDetalhesViewModel?> ObterDetalhesAsync(int numero) => _dados.LerAsync(s =>
    {
        var ordem = s.Ordens.FirstOrDefault(o => o.Numero == numero);
        if (ordem is null) return null;
        return new OrdemDetalhesViewModel
        {
            Ordem = ordem,
            Veiculo = s.Veiculos.FirstOrDefault(v => v.Id == ordem.VeiculoId),
            Cliente = s.Clientes.FirstOrDefault(c => c.Id == ordem.ClienteId),
            TotalOrdensVeiculo = s.Ordens.Count(o => o.VeiculoId == ordem.VeiculoId)
        };
    });

    /// <summary>Carrega os dados de apoio do formulário de abertura.</summary>
    public Task PrepararAberturaAsync(AbrirOrdemViewModel form) => _dados.LerAsync(s =>
    {
        form.Veiculos = Projecoes.Veiculos(s, s.Veiculos).OrderBy(v => v.Placa).ToList();
        form.VeiculoSelecionado = form.Veiculos.FirstOrDefault(v => v.Id == form.VeiculoId);
        if (form.VeiculoSelecionado is null) form.VeiculoFixo = false;
        form.OrdensEmAbertoDoVeiculo = form.VeiculoSelecionado is null
            ? []
            : Projecoes.Ordens(s, s.Ordens.Where(o => o.VeiculoId == form.VeiculoId && o.Status != StatusOrdem.Concluida));
        form.ProximoNumero = s.ConsultarProximoNumeroOrdem();
        return true;
    });

    /// <summary>
    /// Abre uma ordem de serviço para um veículo cadastrado (RN07), com número
    /// único gerado automaticamente (RN08) e status inicial Pendente (RN09).
    /// </summary>
    public Task<Resultado<OrdemServico>> AbrirAsync(AbrirOrdemViewModel form, UsuarioLogado usuario) => _dados.EscreverAsync(s =>
    {
        var veiculo = s.Veiculos.FirstOrDefault(v => v.Id == form.VeiculoId);
        if (veiculo is null)
            return Resultado<OrdemServico>.FalhaCampo(nameof(form.VeiculoId), "Selecione um veículo cadastrado.");

        if (!s.Clientes.Any(c => c.Id == veiculo.ClienteId))
            return Resultado<OrdemServico>.FalhaCampo(nameof(form.VeiculoId), "O veículo selecionado não possui um cliente válido vinculado.");

        var descricao = (form.DescricaoProblema ?? string.Empty).Trim();
        if (descricao.Length is < DescricaoMinimo or > DescricaoMaximo)
            return Resultado<OrdemServico>.FalhaCampo(nameof(form.DescricaoProblema),
                $"Descreva o problema relatado ({DescricaoMinimo} a {DescricaoMaximo} caracteres).");

        var ordem = new OrdemServico
        {
            Numero = s.ProximoNumeroOrdem(),
            VeiculoId = veiculo.Id,
            ClienteId = veiculo.ClienteId,
            DataAbertura = DateTime.Now,
            DescricaoProblema = descricao,
            Status = StatusOrdem.Pendente,
            AbertaPorId = usuario.Id,
            AbertaPorNome = usuario.Nome
        };
        s.Ordens.Add(ordem);
        return Resultado<OrdemServico>.Ok(ordem, $"Ordem de serviço {ordem.NumeroFormatado} aberta com status Pendente.");
    }, r => r.Sucesso);
}
