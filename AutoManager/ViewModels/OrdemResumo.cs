using AutoManager.Models.Enums;

namespace AutoManager.ViewModels;

/// <summary>Linha de ordem de serviço usada em listas, dashboard e históricos.</summary>
public record OrdemResumo(
    int Numero,
    StatusOrdem Status,
    DateTime DataAbertura,
    DateTime? DataConclusao,
    string DescricaoProblema,
    int VeiculoId,
    string Placa,
    string Veiculo,
    int ClienteId,
    string ClienteNome,
    int QuantidadeObservacoes)
{
    public string NumeroFormatado => Models.OrdemServico.FormatarNumero(Numero);
}

/// <summary>Linha de veículo usada em listas.</summary>
public record VeiculoResumo(
    int Id,
    string Placa,
    string Marca,
    string Modelo,
    int? Ano,
    string? Cor,
    int ClienteId,
    string ClienteNome,
    int TotalOrdens,
    int OrdensAbertas)
{
    public string Descricao => $"{Marca} {Modelo}".Trim();
}
