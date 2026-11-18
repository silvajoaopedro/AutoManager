using System.ComponentModel.DataAnnotations;
using AutoManager.Models;
using AutoManager.Models.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace AutoManager.ViewModels;

public class AbrirOrdemViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Selecione o veículo que será atendido.")]
    [Display(Name = "Veículo")]
    public int VeiculoId { get; set; }

    [Required(ErrorMessage = "Descreva o problema relatado pelo cliente.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "A descrição deve ter entre {2} e {1} caracteres.")]
    [Display(Name = "Descrição do problema relatado")]
    public string DescricaoProblema { get; set; } = string.Empty;

    /// <summary>Veículo veio do fluxo Cliente → Veículo → OS e já está selecionado.</summary>
    public bool VeiculoFixo { get; set; }

    [ValidateNever]
    public List<VeiculoResumo> Veiculos { get; set; } = [];

    [ValidateNever]
    public VeiculoResumo? VeiculoSelecionado { get; set; }

    [ValidateNever]
    public List<OrdemResumo> OrdensEmAbertoDoVeiculo { get; set; } = [];

    /// <summary>Número que será atribuído (apenas informativo; o definitivo é gerado ao salvar).</summary>
    public int ProximoNumero { get; set; }
}

public class OrdemListaViewModel
{
    public StatusOrdem? Status { get; set; }
    public string? Busca { get; set; }
    public List<OrdemResumo> Itens { get; set; } = [];
    public int Total { get; set; }
    public int Pendentes { get; set; }
    public int EmAndamento { get; set; }
    public int Concluidas { get; set; }
}

public class OrdemDetalhesViewModel
{
    public required OrdemServico Ordem { get; init; }
    public Veiculo? Veiculo { get; init; }
    public Cliente? Cliente { get; init; }
    public int TotalOrdensVeiculo { get; init; }

    public bool Concluida => Ordem.Status == StatusOrdem.Concluida;
}

public class ObservacaoViewModel
{
    [Required(ErrorMessage = "Escreva a observação.")]
    [StringLength(1000, MinimumLength = 3, ErrorMessage = "A observação deve ter entre {2} e {1} caracteres.")]
    public string Texto { get; set; } = string.Empty;
}
