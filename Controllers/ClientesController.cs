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
        return View(_oficina.ConsultarClientes());
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
            ModelState.AddModelError(string.Empty, erro.Message);
            return View(modelo);
        }

        TempData["Sucesso"] = "Cliente cadastrado com sucesso.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Atendente")]
    public IActionResult Editar(Guid id)
    {
        var cliente = _oficina.ConsultarClientes()
            .FirstOrDefault(item => item.Id == id);

        if (cliente is null)
        {
            return NotFound();
        }

        ViewData["ClienteId"] = cliente.Id;

        return View(new ClienteViewModel
        {
            Nome = cliente.Nome,
            Telefone = cliente.Telefone
        });
    }

    [HttpPost]
    [Authorize(Roles = "Atendente")]
    [ValidateAntiForgeryToken]
    public IActionResult Editar(Guid id, ClienteViewModel modelo)
    {
        var cliente = _oficina.ConsultarClientes()
            .FirstOrDefault(item => item.Id == id);

        if (cliente is null)
        {
            return NotFound();
        }

        ViewData["ClienteId"] = id;

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        try
        {
            _oficina.EditarCliente(
                id,
                modelo.Nome,
                modelo.Telefone);
        }
        catch (ArgumentException erro)
        {
            ModelState.AddModelError(string.Empty, erro.Message);
            return View(modelo);
        }
        catch (InvalidOperationException erro)
        {
            ModelState.AddModelError(string.Empty, erro.Message);
            return View(modelo);
        }

        TempData["Sucesso"] = "Cliente atualizado com sucesso.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Atendente")]
    public IActionResult Excluir(Guid id)
    {
        var cliente = _oficina.ConsultarClientes()
            .FirstOrDefault(item => item.Id == id);

        if (cliente is null)
        {
            return NotFound();
        }

        return View(cliente);
    }

    [HttpPost]
    [ActionName("Excluir")]
    [Authorize(Roles = "Atendente")]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmarExclusao(Guid id)
    {
        var cliente = _oficina.ConsultarClientes()
            .FirstOrDefault(item => item.Id == id);

        if (cliente is null)
        {
            return NotFound();
        }

        try
        {
            _oficina.ExcluirCliente(id, confirmado: true);
        }
        catch (InvalidOperationException erro)
        {
            ModelState.AddModelError(string.Empty, erro.Message);
            return View("Excluir", cliente);
        }

        TempData["Sucesso"] = "Cliente excluído com sucesso.";

        return RedirectToAction(nameof(Index));
    }
}