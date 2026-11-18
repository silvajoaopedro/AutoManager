using AutoManager.Infrastructure;
using AutoManager.Models.Enums;
using AutoManager.Services;
using AutoManager.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoManager.Controllers;

/// <summary>
/// Gerenciamento de clientes. Consulta liberada para os dois perfis;
/// cadastro, edição e exclusão exclusivos da Atendente.
/// </summary>
public class ClientesController : Controller
{
    private readonly ClienteService _clientes;

    public ClientesController(ClienteService clientes) => _clientes = clientes;

    [HttpGet]
    public async Task<IActionResult> Index(string? busca)
    {
        return View(await _clientes.ListarAsync(busca));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, bool novo = false)
    {
        var modelo = await _clientes.ObterDetalhesAsync(id);
        if (modelo is null) return NotFound();
        modelo.RecemCadastrado = novo && User.EhAtendente();
        return View(modelo);
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpGet]
    public IActionResult Create() => View(new ClienteFormViewModel());

    /// <param name="continuar">"veiculo" para seguir direto ao cadastro do veículo.</param>
    [Authorize(Roles = Perfis.Atendente)]
    [HttpPost]
    public async Task<IActionResult> Create(ClienteFormViewModel form, string? continuar)
    {
        if (!ModelState.IsValid) return View(form);

        var resultado = await _clientes.CriarAsync(form);
        if (!resultado.Sucesso)
        {
            this.AplicarErros(resultado);
            return View(form);
        }

        this.Sucesso(resultado.Mensagem);
        var cliente = resultado.Valor!;
        if (continuar == "veiculo")
            return RedirectToAction("Create", "Veiculos", new { clienteId = cliente.Id });

        return RedirectToAction(nameof(Details), new { id = cliente.Id, novo = true });
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var cliente = await _clientes.ObterAsync(id);
        if (cliente is null) return NotFound();
        return View(ClienteFormViewModel.De(cliente));
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, ClienteFormViewModel form)
    {
        form.Id = id;
        if (await _clientes.ObterAsync(id) is null) return NotFound();
        if (!ModelState.IsValid) return View(form);

        var resultado = await _clientes.AtualizarAsync(id, form);
        if (!resultado.Sucesso)
        {
            this.AplicarErros(resultado);
            return View(form);
        }

        this.Sucesso(resultado.Mensagem);
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var modelo = await _clientes.ObterParaExclusaoAsync(id);
        if (modelo is null) return NotFound();
        return View(modelo);
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var resultado = await _clientes.ExcluirAsync(id);
        if (!resultado.Sucesso)
        {
            this.Erro(resultado.Mensagem);
            return RedirectToAction(nameof(Details), new { id });
        }

        this.Sucesso(resultado.Mensagem);
        return RedirectToAction(nameof(Index));
    }
}
