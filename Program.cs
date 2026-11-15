using AutoManager.Services;
using AutoManager.Verificacoes;

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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<RepositorioJson>();
builder.Services.AddSingleton<OficinaService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();