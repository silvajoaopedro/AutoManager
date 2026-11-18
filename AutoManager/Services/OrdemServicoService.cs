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

    // -----------------------------------------------------------------------
    // Atualização de status e observações (RN10 a RN14)
    // -----------------------------------------------------------------------

    public const int ObservacaoMinimo = 3;
    public const int ObservacaoMaximo = 1000;
    public const int ConclusaoMinimo = 10;

    /// <summary>Pendente → Em andamento.</summary>
    public Task<Resultado<OrdemServico>> IniciarAsync(int numero, UsuarioLogado usuario) =>
        AtualizarStatusAsync(numero, StatusOrdem.EmAndamento, observacao: null, usuario);

    /// <summary>Em andamento → Concluída, exigindo observação sobre o serviço realizado (RN13).</summary>
    public Task<Resultado<OrdemServico>> ConcluirAsync(int numero, string? observacao, UsuarioLogado usuario) =>
        AtualizarStatusAsync(numero, StatusOrdem.Concluida, observacao, usuario);

    /// <summary>
    /// Ponto único de mudança de status. Só aceita a etapa imediatamente seguinte
    /// (Pendente → Em andamento → Concluída), sem pular nem retroceder (RN11, RN12),
    /// e recusa qualquer alteração em ordem concluída (RN14).
    /// </summary>
    public Task<Resultado<OrdemServico>> AtualizarStatusAsync(int numero, StatusOrdem novoStatus, string? observacao, UsuarioLogado usuario) =>
        _dados.EscreverAsync(s =>
        {
            var ordem = s.Ordens.FirstOrDefault(o => o.Numero == numero);
            if (ordem is null)
                return Resultado<OrdemServico>.Inexistente("Ordem de serviço não encontrada.");

            if (ordem.Status == StatusOrdem.Concluida)
                return Resultado<OrdemServico>.Falha("Esta ordem já foi concluída e não pode ser reaberta nem alterada.");

            if (novoStatus == ordem.Status)
                return Resultado<OrdemServico>.Falha($"A ordem já está com status {ordem.Status.Nome()}.");

            var permitido = ordem.Status.Proximo();
            if (novoStatus != permitido)
                return Resultado<OrdemServico>.Falha(
                    $"Transição não permitida: {ordem.Status.Nome()} → {novoStatus.Nome()}. " +
                    $"A próxima etapa desta ordem é {permitido?.Nome()}.");

            var agora = DateTime.Now;
            switch (novoStatus)
            {
                case StatusOrdem.EmAndamento:
                    ordem.DataInicio = agora;
                    ordem.IniciadaPorId = usuario.Id;
                    ordem.IniciadaPorNome = usuario.Nome;
                    break;

                case StatusOrdem.Concluida:
                    var texto = (observacao ?? string.Empty).Trim();
                    if (texto.Length < ConclusaoMinimo)
                        return Resultado<OrdemServico>.FalhaCampo("Texto",
                            $"Para concluir, registre uma observação sobre o serviço realizado (mínimo de {ConclusaoMinimo} caracteres).");
                    if (texto.Length > ObservacaoMaximo)
                        return Resultado<OrdemServico>.FalhaCampo("Texto", $"A observação deve ter no máximo {ObservacaoMaximo} caracteres.");

                    ordem.Observacoes.Add(NovaObservacao(texto, TipoObservacao.Conclusao, usuario, agora));
                    ordem.DataConclusao = agora;
                    ordem.ConcluidaPorId = usuario.Id;
                    ordem.ConcluidaPorNome = usuario.Nome;
                    break;
            }

            ordem.Status = novoStatus;
            var mensagem = novoStatus == StatusOrdem.EmAndamento
                ? $"Serviço da {ordem.NumeroFormatado} iniciado. Status: Em andamento."
                : $"{ordem.NumeroFormatado} concluída. A ordem permanece disponível para consulta no histórico.";
            return Resultado<OrdemServico>.Ok(ordem, mensagem);
        }, r => r.Sucesso);

    /// <summary>Registra uma observação. Não é permitido em ordens concluídas (RN14).</summary>
    public Task<Resultado<OrdemServico>> RegistrarObservacaoAsync(int numero, string? texto, UsuarioLogado usuario) =>
        _dados.EscreverAsync(s =>
        {
            var ordem = s.Ordens.FirstOrDefault(o => o.Numero == numero);
            if (ordem is null)
                return Resultado<OrdemServico>.Inexistente("Ordem de serviço não encontrada.");

            if (ordem.Status == StatusOrdem.Concluida)
                return Resultado<OrdemServico>.Falha("Esta ordem já foi concluída e não pode receber novas observações.");

            var conteudo = (texto ?? string.Empty).Trim();
            if (conteudo.Length is < ObservacaoMinimo or > ObservacaoMaximo)
                return Resultado<OrdemServico>.FalhaCampo("Texto",
                    $"A observação deve ter entre {ObservacaoMinimo} e {ObservacaoMaximo} caracteres.");

            ordem.Observacoes.Add(NovaObservacao(conteudo, TipoObservacao.Registro, usuario, DateTime.Now));
            return Resultado<OrdemServico>.Ok(ordem, "Observação registrada.");
        }, r => r.Sucesso);

    private static Observacao NovaObservacao(string texto, TipoObservacao tipo, UsuarioLogado usuario, DateTime quando) => new()
    {
        Id = Guid.NewGuid(),
        Texto = texto,
        Tipo = tipo,
        DataHora = quando,
        UsuarioId = usuario.Id,
        UsuarioNome = usuario.Nome,
        UsuarioPerfil = usuario.Perfil
    };
}
