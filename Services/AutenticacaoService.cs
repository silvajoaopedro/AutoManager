using System.Text.Json;
using AutoManager.Models;

namespace AutoManager.Services;

public class AutenticacaoService
{
    private readonly string _caminhoUsuarios;

    private readonly JsonSerializerOptions _opcoes = new()
    {
        WriteIndented = true
    };

    public AutenticacaoService(IWebHostEnvironment ambiente)
    {
        string diretorio = Path.Combine(
            ambiente.ContentRootPath,
            "App_Data");

        Directory.CreateDirectory(diretorio);

        _caminhoUsuarios = Path.Combine(
            diretorio,
            "usuarios.json");
    }

    public void InicializarContas(IConfiguration configuracao)
    {
        if (File.Exists(_caminhoUsuarios))
        {
            return;
        }

        string? senhaAtendente =
            configuracao["ContasIniciais:Atendente:Senha"];

        string? senhaMecanico =
            configuracao["ContasIniciais:Mecanico:Senha"];

        if (string.IsNullOrWhiteSpace(senhaAtendente) ||
            string.IsNullOrWhiteSpace(senhaMecanico))
        {
            throw new InvalidOperationException(
                "Configure as senhas iniciais antes de iniciar a aplicação.");
        }

        var usuarios = new List<Usuario>
        {
            new Usuario(
                "atendente",
                senhaAtendente,
                PerfilUsuario.Atendente),

            new Usuario(
                "mecanico",
                senhaMecanico,
                PerfilUsuario.Mecanico)
        };

        GravarUsuarios(usuarios, substituir: false);
    }

    public Usuario? Autenticar(string login, string senha)
    {
        if (string.IsNullOrWhiteSpace(login) ||
            string.IsNullOrWhiteSpace(senha))
        {
            return null;
        }

        List<Usuario> usuarios = LerUsuarios();

        Usuario? usuario = usuarios.FirstOrDefault(item =>
            string.Equals(
                item.Login,
                login.Trim(),
                StringComparison.OrdinalIgnoreCase));

        if (usuario is null || !usuario.VerificarSenha(senha))
        {
            return null;
        }

        return usuario;
    }

    public void RedefinirSenhaAtendente(string novaSenha)
    {
        if (string.IsNullOrWhiteSpace(novaSenha))
        {
            throw new ArgumentException(
                "Informe a nova senha.");
        }

        List<Usuario> usuarios = LerUsuarios();

        int indice = usuarios.FindIndex(usuario =>
            string.Equals(
                usuario.Login,
                "atendente",
                StringComparison.OrdinalIgnoreCase) &&
            usuario.Perfil == PerfilUsuario.Atendente);

        if (indice < 0)
        {
            throw new InvalidOperationException(
                "Conta do Atendente não encontrada.");
        }

        Usuario atual = usuarios[indice];

        var senhaAtualizada = new Usuario(
            atual.Login,
            novaSenha,
            atual.Perfil);

        usuarios[indice] = new Usuario(
            atual.Id,
            atual.Login,
            senhaAtualizada.SenhaHash,
            atual.Perfil);

        GravarUsuarios(usuarios, substituir: true);
    }

    private List<Usuario> LerUsuarios()
    {
        string json = File.ReadAllText(_caminhoUsuarios);

        var usuarios = JsonSerializer.Deserialize<List<Usuario>>(json);

        if (usuarios is null || usuarios.Any(item => item is null))
        {
            throw new InvalidOperationException(
                "Não foi possível carregar os usuários.");
        }

        return usuarios;
    }

    private void GravarUsuarios(
        List<Usuario> usuarios,
        bool substituir)
    {
        string temporario =
            _caminhoUsuarios + "." +
            Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            using (var arquivo = new FileStream(
                temporario,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                JsonSerializer.Serialize(
                    arquivo,
                    usuarios,
                    _opcoes);

                arquivo.Flush(flushToDisk: true);
            }

            File.Move(
                temporario,
                _caminhoUsuarios,
                overwrite: substituir);
        }
        finally
        {
            if (File.Exists(temporario))
            {
                File.Delete(temporario);
            }
        }
    }
}