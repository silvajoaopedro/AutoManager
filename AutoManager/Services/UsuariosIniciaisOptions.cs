using AutoManager.Models.Enums;

namespace AutoManager.Services;

/// <summary>
/// Usuários criados automaticamente na primeira execução, quando o arquivo
/// usuarios.json ainda não possui registros. Configurados em appsettings.json.
/// </summary>
public class UsuariosIniciaisOptions
{
    public const string Secao = "UsuariosIniciais";

    public List<UsuarioInicial> Usuarios { get; set; } = [];

    public class UsuarioInicial
    {
        public string Login { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public PerfilUsuario Perfil { get; set; }
    }
}
