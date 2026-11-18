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

        var inicioPeriodo = DateOnly.FromDateTime(DateTime.Today).AddDays(-6);
        var conclusoes = new int[7];
        foreach (var ordem in ordens.Where(ordem => ordem.Status == StatusOrdem.Concluida))
        {
            var dataConclusao = DateOnly.FromDateTime(ordem.Observacoes[^1].DataRegistro.ToLocalTime().DateTime);
            var dia = dataConclusao.DayNumber - inicioPeriodo.DayNumber;
            if (dia >= 0 && dia < conclusoes.Length)
                conclusoes[dia]++;
        }

        return View(new PainelOficinaViewModel(
            ordens.Count(ordem => ordem.Status == StatusOrdem.Pendente),
            ordens.Count(ordem => ordem.Status == StatusOrdem.EmAndamento),
            ordens.Count(ordem => ordem.Status == StatusOrdem.Concluida),
            ordens.Take(6).ToList(),
            veiculos,
            conclusoes,
            inicioPeriodo));
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
