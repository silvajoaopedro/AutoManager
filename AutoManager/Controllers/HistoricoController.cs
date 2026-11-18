using AutoManager.Services;
using Microsoft.AspNetCore.Mvc;

namespace AutoManager.Controllers;

/// <summary>Consulta do histórico de serviços por veículo — disponível aos dois perfis.</summary>
public class HistoricoController : Controller
{
    private readonly HistoricoService _historico;
    private readonly VeiculoService _veiculos;

    public HistoricoController(HistoricoService historico, VeiculoService veiculos)
    {
        _historico = historico;
        _veiculos = veiculos;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? busca)
    {
        return View(await _historico.ListarAsync(busca));
    }

    [HttpGet]
    public async Task<IActionResult> Veiculo(int id)
    {
        var modelo = await _veiculos.ObterDetalhesAsync(id);
        if (modelo is null) return NotFound();
        return View(modelo);
    }
}
