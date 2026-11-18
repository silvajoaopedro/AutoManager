using System.ComponentModel.DataAnnotations;

namespace AutoManager.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o usuário.")]
    [StringLength(50, ErrorMessage = "O usuário deve ter no máximo {1} caracteres.")]
    [Display(Name = "Usuário")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [StringLength(100, ErrorMessage = "A senha deve ter no máximo {1} caracteres.")]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = string.Empty;

    [Display(Name = "Manter conectado")]
    public bool Lembrar { get; set; }

    public string? ReturnUrl { get; set; }
}
