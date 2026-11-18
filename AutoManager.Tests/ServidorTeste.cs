using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace AutoManager.Tests;

/// <summary>
/// Sobe a aplicação real (Kestrel) dentro do processo de testes, em uma porta livre
/// e com pasta de dados temporária, para testes ponta a ponta via HTTP.
/// </summary>
public static partial class ServidorTeste
{
    private static readonly Lazy<Task<string>> Inicio = new(IniciarAsync);

    public static string PastaDados { get; } = Path.Combine(Path.GetTempPath(), "automanager-http", Guid.NewGuid().ToString("N"));

    public static Task<string> UrlAsync() => Inicio.Value;

    private static async Task<string> IniciarAsync()
    {
        var porta = PortaLivre();
        var url = $"http://127.0.0.1:{porta}";
        var raizProjeto = LocalizarProjetoWeb();
        var args = new[]
        {
            // O nome da aplicação define onde o MVC procura controllers e views compiladas
            "--applicationName", "AutoManager",
            "--urls", url,
            "--environment", "Development",
            "--contentRoot", raizProjeto,
            "--Persistencia:Pasta", PastaDados,
            "--Logging:LogLevel:Default", "Warning"
        };

        var entrada = typeof(AutoManager.Services.AuthService).Assembly.EntryPoint!;
        var execucao = Task.Run(() => entrada.Invoke(null, [args]));

        using var http = new HttpClient(new HttpClientHandler { UseProxy = false });
        for (var i = 0; i < 100; i++)
        {
            if (execucao.IsFaulted)
                throw new InvalidOperationException("Falha ao iniciar a aplicação: " + execucao.Exception?.GetBaseException().ToString());
            try
            {
                var r = await http.GetAsync(url + "/Account/Login");
                if (r.StatusCode == HttpStatusCode.OK) return url;
            }
            catch (HttpRequestException) { }
            await Task.Delay(100);
        }
        throw new InvalidOperationException("A aplicação não iniciou a tempo.");
    }

    private static int PortaLivre()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var porta = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return porta;
    }

    private static string LocalizarProjetoWeb()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidato = Path.Combine(dir.FullName, "AutoManager", "AutoManager.csproj");
            if (File.Exists(candidato)) return Path.GetDirectoryName(candidato)!;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Projeto AutoManager não encontrado.");
    }

    /// <summary>Navegador simplificado: mantém cookies, não segue redirecionamentos e envia o token antiforgery.</summary>
    public sealed partial class Navegador : IDisposable
    {
        private readonly HttpClient _http;
        public string Base { get; }

        public Navegador(string baseUrl)
        {
            Base = baseUrl;
            _http = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer(), AllowAutoRedirect = false, UseProxy = false })
            {
                BaseAddress = new Uri(baseUrl)
            };
        }

        public Task<HttpResponseMessage> GetAsync(string caminho) => _http.GetAsync(caminho);

        public async Task<string> HtmlAsync(string caminho)
        {
            var r = await _http.GetAsync(caminho);
            return WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());
        }

        /// <summary>Abre a página do formulário para obter o token e envia o POST.</summary>
        public async Task<HttpResponseMessage> PostAsync(string paginaFormulario, string acao, Dictionary<string, string> campos, bool comToken = true)
        {
            var dados = new Dictionary<string, string>(campos);
            if (comToken)
            {
                var html = await (await _http.GetAsync(paginaFormulario)).Content.ReadAsStringAsync();
                var token = TokenRegex().Match(html);
                if (!token.Success) throw new FalhaDeTeste($"Token antiforgery não encontrado em {paginaFormulario}.");
                dados["__RequestVerificationToken"] = token.Groups[1].Value;
            }
            return await _http.PostAsync(acao, new FormUrlEncodedContent(dados));
        }

        public async Task EntrarAsync(string login, string senha)
        {
            var r = await PostAsync("/Account/Login", "/Account/Login", new() { ["Login"] = login, ["Senha"] = senha });
            if (r.StatusCode != HttpStatusCode.Redirect) throw new FalhaDeTeste($"Login de '{login}' falhou ({(int)r.StatusCode}).");
        }

        public void Dispose() => _http.Dispose();

        [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
        private static partial Regex TokenRegex();
    }
}
