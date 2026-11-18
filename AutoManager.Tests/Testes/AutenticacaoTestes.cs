using AutoManager.Models.Enums;
using AutoManager.Services;

namespace AutoManager.Tests.Testes;

public class AutenticacaoTestes : ContextoServicos
{
    private static UsuariosIniciaisOptions Iniciais() => new()
    {
        Usuarios =
        [
            new() { Login = "atendente", Nome = "Atendente", Senha = "Atendente@123", Perfil = PerfilUsuario.Atendente },
            new() { Login = "mecanico", Nome = "Mecânico", Senha = "Mecanico@123", Perfil = PerfilUsuario.Mecanico }
        ]
    };

    [Teste("Senha é armazenada com hash e salt, nunca em texto puro")]
    public async Task SenhaComHashESalt()
    {
        var auth = new AuthService(Dados, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthService>.Instance);
        await auth.GarantirUsuariosIniciaisAsync(Iniciais());

        var json = await File.ReadAllTextAsync(Path.Combine(Pasta, "usuarios.json"));
        Verificar.Falso(json.Contains("Atendente@123"), "A senha não pode aparecer no arquivo");
        Verificar.Contem("senhaHash", json, "usuarios.json");
        Verificar.Contem("senhaSalt", json, "usuarios.json");

        var (h1, s1) = SenhaHasher.GerarHash("mesma-senha");
        var (h2, s2) = SenhaHasher.GerarHash("mesma-senha");
        Verificar.Verdadeiro(s1 != s2 && h1 != h2, "Salts aleatórios devem gerar hashes diferentes");
        Verificar.Verdadeiro(SenhaHasher.Verificar("mesma-senha", h1, s1), "Senha correta deve ser aceita");
        Verificar.Falso(SenhaHasher.Verificar("outra", h1, s1), "Senha incorreta deve ser recusada");
    }

    [Teste("Login válido retorna o usuário com seu perfil; login inválido é recusado")]
    public async Task LoginValidoEInvalido()
    {
        var auth = new AuthService(Dados, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthService>.Instance);
        await auth.GarantirUsuariosIniciaisAsync(Iniciais());

        var atendente = await auth.AutenticarAsync("ATENDENTE", "Atendente@123");
        Verificar.Igual(PerfilUsuario.Atendente, atendente?.Perfil, "Perfil da atendente");
        var mecanico = await auth.AutenticarAsync("mecanico", "Mecanico@123");
        Verificar.Igual(PerfilUsuario.Mecanico, mecanico?.Perfil, "Perfil do mecânico");

        Verificar.Igual(null, await auth.AutenticarAsync("atendente", "senha-errada"), "Senha errada");
        Verificar.Igual(null, await auth.AutenticarAsync("inexistente", "Atendente@123"), "Usuário inexistente");
        Verificar.Igual(null, await auth.AutenticarAsync("", ""), "Campos vazios");
    }

    [Teste("Usuários iniciais são criados uma única vez")]
    public async Task UsuariosIniciaisUmaVez()
    {
        var auth = new AuthService(Dados, Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthService>.Instance);
        await auth.GarantirUsuariosIniciaisAsync(Iniciais());
        await auth.GarantirUsuariosIniciaisAsync(Iniciais());
        var total = await Dados.LerAsync(s => s.Usuarios.Count);
        Verificar.Igual(2, total, "Quantidade de usuários");
    }
}
