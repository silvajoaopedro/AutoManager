using AutoManager.Models;
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

    [HttpGet]
    [Authorize(Roles = "Atendente")]
    public IActionResult Cadastrar()
    {
        return View(new ClienteViewModel());
    }

    [HttpPost]
    [Authorize(Roles = "Atendente")]
    [ValidateAntiForgeryToken]
    public IActionResult Cadastrar(ClienteViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        try
        {
            _oficina.CadastrarCliente(
                modelo.Nome,
                modelo.Telefone);
        }
        catch (ArgumentException erro)
        {
            ModelState.AddModelError(
                string.Empty,
                erro.Message);

            return View(modelo);
        }

        TempData["Sucesso"] = "Cliente cadastrado com sucesso.";

        return RedirectToAction(nameof(Index));
    }
}