using System.Security.Claims;
using AutoManager.Models;
using AutoManager.Models.Enums;

namespace AutoManager.Infrastructure;

/// <summary>Dados do usuário autenticado, lidos a partir das claims do cookie.</summary>
public record UsuarioLogado(int Id, string Nome, PerfilUsuario Perfil);

public static class UsuarioClaims
{
    public static ClaimsPrincipal CriarPrincipal(Usuario usuario, string esquema)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new("login", usuario.Login),
            new(ClaimTypes.Role, usuario.Perfil.ToString())
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, esquema));
    }

    public static UsuarioLogado ObterUsuario(this ClaimsPrincipal principal)
    {
        var id = int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var valor) ? valor : 0;
        var nome = principal.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var perfil = Enum.TryParse<PerfilUsuario>(principal.FindFirstValue(ClaimTypes.Role), out var p)
            ? p
            : PerfilUsuario.Mecanico;
        return new UsuarioLogado(id, nome, perfil);
    }

    public static bool EhAtendente(this ClaimsPrincipal principal) => principal.IsInRole(Perfis.Atendente);
}
