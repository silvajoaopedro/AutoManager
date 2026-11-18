using AutoManager.Persistencia;
using AutoManager.Models;
using Microsoft.AspNetCore.Identity;

namespace AutoManager.Services;

public class AuthService
{
    private static readonly PasswordHasher<Usuario> Hasher = new();
    private static readonly Usuario UsuarioFicticio = new();
    private static readonly string HashFicticio = Hasher.HashPassword(UsuarioFicticio, Guid.NewGuid().ToString());

    private readonly JsonDataStore _dados;
    private readonly ILogger<AuthService> _logger;

    public AuthService(JsonDataStore dados, ILogger<AuthService> logger)
    {
        _dados = dados;
        _logger = logger;
    }

    /// <summary>
    /// Valida as credenciais. Retorna o usuário quando login e senha conferem
    /// e o usuário está ativo; caso contrário, retorna nulo.
    /// </summary>
    public async Task<Usuario?> AutenticarAsync(string login, string senha)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(senha))
            return null;

        var loginNormalizado = login.Trim();
        var usuario = await _dados.LerAsync(s => s.Usuarios.FirstOrDefault(u =>
            string.Equals(u.Login, loginNormalizado, StringComparison.OrdinalIgnoreCase)));

        if (usuario is null || !usuario.Ativo)
        {
            // Executa o hash mesmo assim para não revelar, pelo tempo de resposta, se o usuário existe
            Hasher.VerifyHashedPassword(UsuarioFicticio, HashFicticio, senha);
            return null;
        }

        if (string.IsNullOrEmpty(usuario.SenhaSalt))
            return Hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha) != PasswordVerificationResult.Failed
                ? usuario : null;

        return await _dados.EscreverAsync(sessao =>
        {
            var atual = sessao.Usuarios.FirstOrDefault(u => u.Id == usuario.Id && u.Ativo);
            if (atual is null) return (Usuario?)null;
            if (string.IsNullOrEmpty(atual.SenhaSalt))
                return Hasher.VerifyHashedPassword(atual, atual.SenhaHash, senha) != PasswordVerificationResult.Failed
                    ? atual : null;
            if (!SenhaHasher.Verificar(senha, atual.SenhaHash, atual.SenhaSalt)) return null;

            atual.SenhaHash = Hasher.HashPassword(atual, senha);
            atual.SenhaSalt = null;
            return atual;
        }, deveSalvar: resultado => resultado is not null);
    }

    /// <summary>
    /// Cria os usuários iniciais configurados quando ainda não existe nenhum usuário.
    /// </summary>
    public async Task GarantirUsuariosIniciaisAsync(UsuariosIniciaisOptions opcoes)
    {
        var criados = await _dados.EscreverAsync(sessao =>
        {
            if (sessao.Usuarios.Count > 0)
                return 0;

            foreach (var inicial in opcoes.Usuarios.Where(u => !string.IsNullOrWhiteSpace(u.Login)))
            {
                var usuario = new Usuario
                {
                    Id = sessao.ProximoIdUsuario(),
                    Login = inicial.Login.Trim().ToLowerInvariant(),
                    Nome = inicial.Nome.Trim(),
                    Perfil = inicial.Perfil,
                    Ativo = true,
                    DataCriacao = DateTime.Now
                };
                usuario.SenhaHash = Hasher.HashPassword(usuario, inicial.Senha);
                sessao.Usuarios.Add(usuario);
            }
            return sessao.Usuarios.Count;
        }, deveSalvar: quantidade => quantidade > 0);

        if (criados > 0)
            _logger.LogInformation("{Quantidade} usuário(s) inicial(is) criado(s) em oficina.json.", criados);
    }
}
