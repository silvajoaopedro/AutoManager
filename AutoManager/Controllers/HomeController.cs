using System.Diagnostics;
using AutoManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoManager.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    [AllowAnonymous]
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
