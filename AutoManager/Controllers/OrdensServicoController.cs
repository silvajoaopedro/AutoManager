using AutoManager.Infrastructure;
using AutoManager.Models.Enums;
using AutoManager.Services;
using AutoManager.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoManager.Controllers;

/// <summary>Módulo central: abertura, consulta e acompanhamento das ordens de serviço.</summary>
public class OrdensServicoController : Controller
{
    private readonly OrdemServicoService _ordens;

    public OrdensServicoController(OrdemServicoService ordens) => _ordens = ordens;

    [HttpGet]
    public async Task<IActionResult> Index(StatusOrdem? status, string? busca)
    {
        return View(await _ordens.ListarAsync(status, busca));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var modelo = await _ordens.ObterDetalhesAsync(id);
        if (modelo is null) return NotFound();
        return View(modelo);
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpGet]
    public async Task<IActionResult> Create(int? veiculoId)
    {
        var form = new AbrirOrdemViewModel { VeiculoId = veiculoId ?? 0, VeiculoFixo = veiculoId is > 0 };
        await _ordens.PrepararAberturaAsync(form);
        return View(form);
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpPost]
    public async Task<IActionResult> Create(AbrirOrdemViewModel form)
    {
        if (ModelState.IsValid)
        {
            var resultado = await _ordens.AbrirAsync(form, User.ObterUsuario());
            if (resultado.Sucesso)
            {
                this.Sucesso(resultado.Mensagem);
                return RedirectToAction(nameof(Details), new { id = resultado.Valor!.Numero });
            }
            this.AplicarErros(resultado);
        }

        await _ordens.PrepararAberturaAsync(form);
        return View(form);
    }
}
