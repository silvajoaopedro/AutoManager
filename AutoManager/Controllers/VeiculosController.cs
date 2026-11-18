using AutoManager.Infrastructure;
using AutoManager.Models.Enums;
using AutoManager.Services;
using AutoManager.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoManager.Controllers;

/// <summary>
/// Gerenciamento de veículos. Consulta e histórico liberados para os dois perfis;
/// cadastro, edição e exclusão exclusivos da Atendente (RN03).
/// </summary>
public class VeiculosController : Controller
{
    private readonly VeiculoService _veiculos;
    private readonly ClienteService _clientes;

    public VeiculosController(VeiculoService veiculos, ClienteService clientes)
    {
        _veiculos = veiculos;
        _clientes = clientes;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? busca)
    {
        return View(await _veiculos.ListarAsync(busca));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, bool novo = false)
    {
        var modelo = await _veiculos.ObterDetalhesAsync(id);
        if (modelo is null) return NotFound();
        modelo.RecemCadastrado = novo && User.EhAtendente();
        return View(modelo);
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpGet]
    public async Task<IActionResult> Create(int? clienteId)
    {
        var form = new VeiculoFormViewModel();
        if (clienteId is > 0 && await _clientes.ObterAsync(clienteId.Value) is not null)
        {
            form.ClienteId = clienteId.Value;
            form.ClienteFixo = true;
        }
        await PrepararFormularioAsync(form);
        return View(form);
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpPost]
    public async Task<IActionResult> Create(VeiculoFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await PrepararFormularioAsync(form);
            return View(form);
        }

        var resultado = await _veiculos.CriarAsync(form);
        if (!resultado.Sucesso)
        {
            this.AplicarErros(resultado);
            await PrepararFormularioAsync(form);
            return View(form);
        }

        this.Sucesso(resultado.Mensagem);
        // Após salvar, a tela de detalhes oferece "Continuar para abertura da ordem de serviço"
        return RedirectToAction(nameof(Details), new { id = resultado.Valor!.Id, novo = true });
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var veiculo = await _veiculos.ObterAsync(id);
        if (veiculo is null) return NotFound();
        var form = VeiculoFormViewModel.De(veiculo);
        await PrepararFormularioAsync(form);
        return View(form);
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpPost]
    public async Task<IActionResult> Edit(int id, VeiculoFormViewModel form)
    {
        form.Id = id;
        form.ClienteFixo = false;
        if (await _veiculos.ObterAsync(id) is null) return NotFound();

        if (!ModelState.IsValid)
        {
            await PrepararFormularioAsync(form);
            return View(form);
        }

        var resultado = await _veiculos.AtualizarAsync(id, form);
        if (!resultado.Sucesso)
        {
            this.AplicarErros(resultado);
            await PrepararFormularioAsync(form);
            return View(form);
        }

        this.Sucesso(resultado.Mensagem);
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var modelo = await _veiculos.ObterParaExclusaoAsync(id);
        if (modelo is null) return NotFound();
        return View(modelo);
    }

    [Authorize(Roles = Perfis.Atendente)]
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var resultado = await _veiculos.ExcluirAsync(id);
        if (!resultado.Sucesso)
        {
            this.Erro(resultado.Mensagem);
            return RedirectToAction(nameof(Details), new { id });
        }

        this.Sucesso(resultado.Mensagem);
        return RedirectToAction(nameof(Index));
    }

    private async Task PrepararFormularioAsync(VeiculoFormViewModel form)
    {
        var clientes = await _clientes.ListarTodosAsync();
        form.Clientes = clientes
            .Select(c => new SelectListItem($"{c.Nome} — {Formatacao.Telefone(c.Telefone)}", c.Id.ToString(), c.Id == form.ClienteId))
            .ToList();
        form.ClienteSelecionado = clientes.FirstOrDefault(c => c.Id == form.ClienteId);
        if (form.ClienteSelecionado is null) form.ClienteFixo = false;
    }
}
