using System.Globalization;
using AutoManager.Persistencia;
using AutoManager.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Persistência em arquivos JSON (pasta fora de wwwroot)
// ---------------------------------------------------------------------------
builder.Services.Configure<JsonDataOptions>(builder.Configuration.GetSection(JsonDataOptions.Secao));
builder.Services.Configure<UsuariosIniciaisOptions>(builder.Configuration.GetSection(UsuariosIniciaisOptions.Secao));
builder.Services.AddSingleton<JsonDataStore>();

// ---------------------------------------------------------------------------
// Serviços de negócio
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<AuthService>();
builder.Services.AddSingleton<ClienteService>();

// ---------------------------------------------------------------------------
// Autenticação por cookie e autorização por perfil (RN01, RN02)
// ---------------------------------------------------------------------------
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opcoes =>
    {
        opcoes.LoginPath = "/Account/Login";
        opcoes.LogoutPath = "/Account/Logout";
        opcoes.AccessDeniedPath = "/Account/AcessoNegado";
        opcoes.Cookie.Name = "AutoManager.Auth";
        opcoes.Cookie.HttpOnly = true;
        opcoes.Cookie.SameSite = SameSiteMode.Strict;
        opcoes.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        opcoes.ExpireTimeSpan = TimeSpan.FromHours(8);
        opcoes.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddControllersWithViews(opcoes =>
{
    // Toda página exige usuário autenticado, exceto as marcadas com [AllowAnonymous]
    opcoes.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
    // Proteção CSRF em todos os formulários POST
    opcoes.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

var app = builder.Build();

// Cultura pt-BR para datas e números
var culturaPadrao = new CultureInfo("pt-BR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culturaPadrao),
    SupportedCultures = [culturaPadrao],
    SupportedUICultures = [culturaPadrao]
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStatusCodePagesWithReExecute("/Home/Status/{0}");
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Cria os usuários iniciais na primeira execução
using (var escopo = app.Services.CreateScope())
{
    var auth = escopo.ServiceProvider.GetRequiredService<AuthService>();
    var iniciais = escopo.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<UsuariosIniciaisOptions>>();
    await auth.GarantirUsuariosIniciaisAsync(iniciais.Value);
}

app.Run();

// Permite referenciar o Program em testes
public partial class Program;
