using System.Security.Claims;
using AutoManager.Models;
using AutoManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoManager.Controllers;

[Authorize(Roles = "Atendente,Mecanico")]
public class OrdensController : Controller
{
    private readonly OficinaService _oficina;

    public OrdensController(OficinaService oficina) => _oficina = oficina;

    [HttpGet]
    public IActionResult Index(StatusOrdem? status)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
            return BadRequest();

        ViewBag.StatusSelecionado = status;
        var ordens = _oficina.ConsultarOrdens();
        if (status.HasValue)
            ordens = ordens.Where(o => o.Status == status.Value).ToList();
        return View(ordens);
    }

    [HttpGet]
    public IActionResult Detalhes(long numero)
    {
        var ordem = _oficina.ConsultarOrdem(numero);
        if (ordem is null) return NotFound();

        ViewBag.Veiculo = _oficina.ConsultarVeiculos()
            .FirstOrDefault(v => v.Id == ordem.VeiculoId);
        return View(ordem);
    }

    [HttpGet]
    [Authorize(Roles = "Atendente")]
    public IActionResult Abrir()
    {
        ViewBag.Veiculos = _oficina.ConsultarVeiculos();
        return View();
    }

    [HttpPost]
    [Authorize(Roles = "Atendente")]
    [ValidateAntiForgeryToken]
    public IActionResult Abrir(Guid veiculoId, string problema)
    {
        try
        {
            var ordem = _oficina.AbrirOrdem(veiculoId, problema);
            TempData["Sucesso"] = $"Ordem #{ordem.Numero} criada com sucesso.";
            return RedirectToAction(nameof(Detalhes), new { numero = ordem.Numero });
        }
        catch (Exception erro) when (erro is ArgumentException or InvalidOperationException)
        {
            ModelState.AddModelError("", erro.Message);
            ViewBag.Veiculos = _oficina.ConsultarVeiculos();
            ViewBag.VeiculoId = veiculoId;
            ViewBag.Problema = problema;
            return View();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AtualizarStatus(long numero, StatusOrdem destino, string? observacao)
    {
        var autor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(autor, out Guid autorId))
            return Forbid();

        try
        {
            _oficina.AtualizarStatus(numero, destino, observacao ?? "", autorId);
            TempData["Sucesso"] = "Status atualizado com sucesso.";
        }
        catch (Exception erro) when (erro is ArgumentException or InvalidOperationException)
        {
            TempData["Erro"] = erro.Message;
        }

        return RedirectToAction(nameof(Detalhes), new { numero });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RegistrarObservacao(long numero, string? texto)
    {
        var autor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(autor, out Guid autorId))
            return Forbid();

        try
        {
            _oficina.RegistrarObservacao(numero, texto ?? "", autorId);
            TempData["Sucesso"] = "Observação registrada.";
        }
        catch (Exception erro) when (erro is ArgumentException or InvalidOperationException)
        {
            TempData["Erro"] = erro.Message;
        }

        return RedirectToAction(nameof(Detalhes), new { numero });
    }
}
