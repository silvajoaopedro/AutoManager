using System.ComponentModel.DataAnnotations;

namespace AutoManager.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o login.")]
    [Display(Name = "Login")]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    [Display(Name = "Senha")]
    public string Senha { get; set; } = "";
}