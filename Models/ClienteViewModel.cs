using System.ComponentModel.DataAnnotations;

namespace AutoManager.Models;

public class ClienteViewModel
{
    [Required(ErrorMessage = "Informe o nome.")]
    [Display(Name = "Nome")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o telefone.")]
    [Display(Name = "Telefone")]
    public string Telefone { get; set; } = string.Empty;
}