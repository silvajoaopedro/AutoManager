using AutoManager.Models;
using AutoManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoManager.Controllers;

[Authorize(Roles = "Atendente,Mecanico")]
public class VeiculosController : Controller
{
    private readonly OficinaService _oficina;

    public VeiculosController(OficinaService oficina)
    {
        _oficina = oficina;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var clientes = _oficina.ConsultarClientes()
            .ToDictionary(cliente => cliente.Id, cliente => cliente.Nome);

        var veiculos = _oficina.ConsultarVeiculos()
            .Select(veiculo => new VeiculoListaViewModel
            {
                Id = veiculo.Id,
                Placa = veiculo.Placa,
                Marca = veiculo.Marca,
                Modelo = veiculo.Modelo,
                NomeCliente = clientes.TryGetValue(
                    veiculo.ClienteId, out var nome)
                        ? nome
                        : "Cliente não encontrado"
            })
            .ToList();

        return View(veiculos);
    }
}
