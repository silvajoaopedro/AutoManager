using System.ComponentModel.DataAnnotations;
using AutoManager.Models;

namespace AutoManager.ViewModels;

public class ClienteFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o nome completo do cliente.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "O nome deve ter entre {2} e {1} caracteres.")]
    [Display(Name = "Nome completo")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone do cliente.")]
    [RegularExpression(@"^\(?\d{2}\)?\s?\d{4,5}-?\d{4}$", ErrorMessage = "Informe o telefone com DDD, ex.: (16) 99999-9999.")]
    [Display(Name = "Telefone")]
    public string Telefone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
    [StringLength(120, ErrorMessage = "O e-mail deve ter no máximo {1} caracteres.")]
    [Display(Name = "E-mail")]
    public string? Email { get; set; }

    public static ClienteFormViewModel De(Cliente c) => new()
    {
        Id = c.Id,
        Nome = c.Nome,
        Telefone = Infrastructure.Formatacao.Telefone(c.Telefone),
        Email = c.Email
    };
}

public record ClienteListaItem(Cliente Cliente, int QuantidadeVeiculos, int QuantidadeOrdens, string Placas);

public class ClienteListaViewModel
{
    public string? Busca { get; set; }
    public List<ClienteListaItem> Itens { get; set; } = [];
    public int Total { get; set; }
}

public class ClienteDetalhesViewModel
{
    public required Cliente Cliente { get; init; }
    public List<VeiculoResumo> Veiculos { get; init; } = [];
    public List<OrdemResumo> Ordens { get; init; } = [];
    public bool RecemCadastrado { get; set; }
    public string? BloqueioExclusao { get; init; }
}

public class ClienteExclusaoViewModel
{
    public required Cliente Cliente { get; init; }
    public int QuantidadeVeiculos { get; init; }
    public int QuantidadeOrdens { get; init; }
    public string? BloqueioExclusao { get; init; }
}
