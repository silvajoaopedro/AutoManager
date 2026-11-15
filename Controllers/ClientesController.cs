using AutoManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoManager.Controllers;

[Authorize(Roles = "Atendente,Mecanico")]
public class ClientesController : Controller
{
    private readonly OficinaService _oficina;

    public ClientesController(OficinaService oficina)
    {
        _oficina = oficina;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var clientes = _oficina.ConsultarClientes();

        return View(clientes);
    }
}