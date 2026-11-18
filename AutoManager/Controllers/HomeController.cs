using System.Diagnostics;
using AutoManager.Models;
using AutoManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoManager.Controllers;

public class HomeController : Controller
{
    private readonly DashboardService _dashboard;

    public HomeController(DashboardService dashboard) => _dashboard = dashboard;

    /// <summary>Dashboard operacional.</summary>
    public async Task<IActionResult> Index()
    {
        return View(await _dashboard.ObterAsync());
    }

    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    /// <summary>Páginas amigáveis para 404 e demais códigos de status.</summary>
    [AllowAnonymous]
    [Route("Home/Status/{codigo:int}")]
    public IActionResult Status(int codigo)
    {
        ViewData["Codigo"] = codigo;
        return View();
    }
}
