using AutoManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoManager.Infrastructure;

public static class ControllerExtensions
{
    /// <summary>Copia os erros de uma operação de negócio para o ModelState do formulário.</summary>
    public static void AplicarErros(this Controller controller, Resultado resultado)
    {
        if (resultado.ErrosCampos.Count == 0)
        {
            controller.ModelState.AddModelError(string.Empty, resultado.Mensagem ?? "Não foi possível concluir a operação.");
            return;
        }
        foreach (var (campo, mensagem) in resultado.ErrosCampos)
            controller.ModelState.AddModelError(campo, mensagem);
    }

    public static void Sucesso(this Controller controller, string? mensagem)
    {
        if (!string.IsNullOrEmpty(mensagem)) controller.TempData["Sucesso"] = mensagem;
    }

    public static void Erro(this Controller controller, string? mensagem)
    {
        if (!string.IsNullOrEmpty(mensagem)) controller.TempData["Erro"] = mensagem;
    }
}
