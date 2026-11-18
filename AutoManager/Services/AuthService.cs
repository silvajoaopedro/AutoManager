using AutoManager.Persistencia;
using AutoManager.Models;
using Microsoft.Extensions.Options;

namespace AutoManager.Services;

public class AuthService
{
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
            SenhaHasher.Verificar(senha, "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=", "AAAAAAAAAAAAAAAAAAAAAA==");
            return null;
        }

        return SenhaHasher.Verificar(senha, usuario.SenhaHash, usuario.SenhaSalt) ? usuario : null;
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
                var (hash, salt) = SenhaHasher.GerarHash(inicial.Senha);
                sessao.Usuarios.Add(new Usuario
                {
                    Id = sessao.ProximoIdUsuario(),
                    Login = inicial.Login.Trim().ToLowerInvariant(),
                    Nome = inicial.Nome.Trim(),
                    Perfil = inicial.Perfil,
                    SenhaHash = hash,
                    SenhaSalt = salt,
                    Ativo = true,
                    DataCriacao = DateTime.Now
                });
            }
            return sessao.Usuarios.Count;
        }, deveSalvar: quantidade => quantidade > 0);

        if (criados > 0)
            _logger.LogInformation("{Quantidade} usuário(s) inicial(is) criado(s) em usuarios.json.", criados);
    }
}
