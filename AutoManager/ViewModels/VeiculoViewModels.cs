using System.ComponentModel.DataAnnotations;
using AutoManager.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoManager.ViewModels;

public class VeiculoFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe a placa do veículo.")]
    [RegularExpression(@"^\s*[A-Za-z]{3}-?\s?[0-9][A-Za-z0-9][0-9]{2}\s*$", ErrorMessage = "Placa inválida. Use o padrão ABC-1234 ou Mercosul ABC1D23.")]
    [Display(Name = "Placa")]
    public string Placa { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a marca.")]
    [StringLength(40, MinimumLength = 2, ErrorMessage = "A marca deve ter entre {2} e {1} caracteres.")]
    [Display(Name = "Marca")]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o modelo.")]
    [StringLength(60, MinimumLength = 1, ErrorMessage = "O modelo deve ter no máximo {1} caracteres.")]
    [Display(Name = "Modelo")]
    public string Modelo { get; set; } = string.Empty;

    [Range(1900, 2100, ErrorMessage = "Informe um ano válido.")]
    [Display(Name = "Ano")]
    public int? Ano { get; set; }

    [StringLength(30, ErrorMessage = "A cor deve ter no máximo {1} caracteres.")]
    [Display(Name = "Cor")]
    public string? Cor { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Selecione o cliente proprietário.")]
    [Display(Name = "Cliente proprietário")]
    public int ClienteId { get; set; }

    /// <summary>Quando verdadeiro, o cliente veio do fluxo de cadastro e é exibido como selecionado.</summary>
    public bool ClienteFixo { get; set; }

    [ValidateNever]
    public List<SelectListItem> Clientes { get; set; } = [];

    [ValidateNever]
    public Cliente? ClienteSelecionado { get; set; }

    public static VeiculoFormViewModel De(Veiculo v) => new()
    {
        Id = v.Id,
        Placa = Infrastructure.Formatacao.Placa(v.Placa),
        Marca = v.Marca,
        Modelo = v.Modelo,
        Ano = v.Ano,
        Cor = v.Cor,
        ClienteId = v.ClienteId
    };
}

public class VeiculoListaViewModel
{
    public string? Busca { get; set; }
    public List<VeiculoResumo> Itens { get; set; } = [];
    public int Total { get; set; }
}

public class VeiculoDetalhesViewModel
{
    public required Veiculo Veiculo { get; init; }
    public Cliente? Cliente { get; init; }

    /// <summary>Ordens do veículo, da mais recente para a mais antiga, com observações (histórico — RN15).</summary>
    public List<OrdemServico> Historico { get; init; } = [];

    public bool RecemCadastrado { get; set; }
    public string? BloqueioExclusao { get; init; }

    public int Concluidas => Historico.Count(o => o.Status == Models.Enums.StatusOrdem.Concluida);
    public int EmAberto => Historico.Count(o => o.Status != Models.Enums.StatusOrdem.Concluida);
}

public class VeiculoExclusaoViewModel
{
    public required Veiculo Veiculo { get; init; }
    public Cliente? Cliente { get; init; }
    public int QuantidadeOrdens { get; init; }
    public string? BloqueioExclusao { get; init; }
}
