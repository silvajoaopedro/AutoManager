using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AutoManager.Models;
using AutoManager.Services;

namespace AutoManager.Controllers;

public class HomeController(OficinaService oficina) : Controller
{
    public IActionResult Index()
    {
        var ordens = oficina.ConsultarOrdens();
        var veiculos = oficina.ConsultarVeiculos()
            .ToDictionary(veiculo => veiculo.Id);

        return View(new PainelOficinaViewModel(
            ordens.Count(ordem => ordem.Status == StatusOrdem.Pendente),
            ordens.Count(ordem => ordem.Status == StatusOrdem.EmAndamento),
            ordens.Count(ordem => ordem.Status == StatusOrdem.Concluida),
            ordens.Take(6).ToList(),
            veiculos));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
