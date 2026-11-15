using System.Security.Claims;
using System.Text.Json;
using AutoManager.Models;
using AutoManager.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoManager.Controllers;

public class ContaController : Controller
{
    private readonly AutenticacaoService _autenticacao;
    private readonly ILogger<ContaController> _logger;

    public ContaController(
        AutenticacaoService autenticacao,
        ILogger<ContaController> logger)
    {
        _autenticacao = autenticacao;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        Usuario? usuario;

        try
        {
            usuario = _autenticacao.Autenticar(
                modelo.Login,
                modelo.Senha);
        }
        catch (Exception erro) when (
            erro is IOException or
            UnauthorizedAccessException or
            JsonException or
            FormatException or
            ArgumentException or
            InvalidOperationException)
        {
            _logger.LogError(
                erro,
                "Falha ao consultar as contas de usuários.");

            ModelState.AddModelError(
                "",
                "Não foi possível entrar. Tente novamente.");

            return View(modelo);
        }

        if (usuario is null)
        {
            ModelState.AddModelError(
                "",
                "Login ou senha inválidos.");

            return View(modelo);
        }

        var claims = new List<Claim>
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                usuario.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                usuario.Login),

            new Claim(
                ClaimTypes.Role,
                usuario.Perfil.ToString())
        };

        var identidade = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidade),
            new AuthenticationProperties
            {
                IsPersistent = false
            });

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sair()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AcessoNegado()
    {
        return StatusCode(
            StatusCodes.Status403Forbidden,
            "Seu perfil não possui permissão para esta operação.");
    }
}