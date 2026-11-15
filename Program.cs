using System.Text;
using AutoManager.Services;
using AutoManager.Verificacoes;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

if (args.Contains("--verificar-json"))
{
    VerificacaoPersistencia.Executar();
    return;
}

if (args.Contains("--verificar-regras"))
{
    VerificacaoOficina.Executar();
    return;
}

bool redefinirSenha =
    args.Contains("--redefinir-senha-atendente");

string[] argumentosAplicacao = args
    .Where(argumento =>
        argumento != "--redefinir-senha-atendente")
    .ToArray();

var builder = WebApplication.CreateBuilder(argumentosAplicacao);

if (redefinirSenha)
{
    Console.Write("Nova senha do Atendente: ");

    string novaSenha = LerSenha();

    var autenticacao = new AutenticacaoService(
        builder.Environment);

    autenticacao.RedefinirSenhaAtendente(novaSenha);

    Console.WriteLine("Senha do Atendente redefinida.");
    return;
}

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<RepositorioJson>();
builder.Services.AddSingleton<OficinaService>();
builder.Services.AddSingleton<AutenticacaoService>();

builder.Services
    .AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.LoginPath = "/Conta/Login";
        opcoes.AccessDeniedPath = "/Conta/AcessoNegado";

        opcoes.Cookie.Name = "AutoManager.Auth";
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SameSite = SameSiteMode.Lax;

        opcoes.Cookie.SecurePolicy =
            builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;

        opcoes.ExpireTimeSpan = TimeSpan.FromHours(2);
        opcoes.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(opcoes =>
{
    opcoes.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Services
    .GetRequiredService<AutenticacaoService>()
    .InicializarContas(app.Configuration);

app.Run();

static string LerSenha()
{
    var senha = new StringBuilder();

    while (true)
    {
        ConsoleKeyInfo tecla = Console.ReadKey(intercept: true);

        if (tecla.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return senha.ToString();
        }

        if (tecla.Key == ConsoleKey.Backspace)
        {
            if (senha.Length > 0)
            {
                senha.Length--;
            }

            continue;
        }

        if (!char.IsControl(tecla.KeyChar))
        {
            senha.Append(tecla.KeyChar);
        }
    }
}