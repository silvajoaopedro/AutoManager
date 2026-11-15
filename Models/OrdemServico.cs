using System.Text.Json.Serialization;

namespace AutoManager.Models;

public class OrdemServico
{
    private readonly List<Observacao> _observacoes;

    public long Numero { get; }
    public Guid VeiculoId { get; }
    public string ProblemaRelatado { get; }
    public DateTimeOffset DataAbertura { get; }

    public StatusOrdem Status { get; private set; }

    public IReadOnlyList<Observacao> Observacoes
        => _observacoes.AsReadOnly();

    public OrdemServico(
        long numero,
        Guid veiculoId,
        string problemaRelatado)
        : this(
            numero,
            veiculoId,
            problemaRelatado,
            DateTimeOffset.Now,
            StatusOrdem.Pendente,
            Array.Empty<Observacao>())
    {
    }

    // Reconstrói uma ordem já salva, preservando seus dados.
    [JsonConstructor]
    public OrdemServico(
        long numero,
        Guid veiculoId,
        string problemaRelatado,
        DateTimeOffset dataAbertura,
        StatusOrdem status,
        IReadOnlyList<Observacao> observacoes)
    {
        if (numero <= 0)
        {
            throw new ArgumentException(
                "O número da ordem deve ser positivo.");
        }

        if (veiculoId == Guid.Empty)
        {
            throw new ArgumentException(
                "A ordem deve estar vinculada a um veículo.");
        }

        if (string.IsNullOrWhiteSpace(problemaRelatado))
        {
            throw new ArgumentException(
                "O problema relatado é obrigatório.");
        }

        if (dataAbertura == default)
        {
            throw new ArgumentException(
                "A data de abertura é obrigatória.");
        }

        if (!Enum.IsDefined(typeof(StatusOrdem), status))
        {
            throw new ArgumentException("Status inválido.");
        }

        if (observacoes is null ||
            observacoes.Any(observacao => observacao is null))
        {
            throw new ArgumentException(
                "A coleção de observações é inválida.");
        }

        if (status == StatusOrdem.Concluida &&
            observacoes.Count == 0)
        {
            throw new ArgumentException(
                "Uma ordem concluída deve possuir observação.");
        }

        Numero = numero;
        VeiculoId = veiculoId;
        ProblemaRelatado = problemaRelatado.Trim();
        DataAbertura = dataAbertura;
        Status = status;

        // Copia a coleção para impedir alterações externas.
        _observacoes = observacoes.ToList();
    }

    public void Iniciar()
    {
        GarantirOrdemAberta();

        if (Status != StatusOrdem.Pendente)
        {
            throw new InvalidOperationException(
                "Somente uma ordem pendente pode ser iniciada.");
        }

        Status = StatusOrdem.EmAndamento;
    }

    public void Concluir(string texto, Guid autorId)
    {
        GarantirOrdemAberta();

        if (Status != StatusOrdem.EmAndamento)
        {
            throw new InvalidOperationException(
                "Somente uma ordem em andamento pode ser concluída.");
        }

        // Valida a observação antes de modificar a ordem.
        var observacao = new Observacao(texto, autorId);

        _observacoes.Add(observacao);
        Status = StatusOrdem.Concluida;
    }

    public void RegistrarObservacao(string texto, Guid autorId)
    {
        GarantirOrdemAberta();

        var observacao = new Observacao(texto, autorId);
        _observacoes.Add(observacao);
    }

    private void GarantirOrdemAberta()
    {
        if (Status == StatusOrdem.Concluida)
        {
            throw new InvalidOperationException(
                "Uma ordem concluída não pode ser modificada.");
        }
    }
}