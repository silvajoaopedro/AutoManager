using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutoManager.Tests.Testes;

/// <summary>
/// Testes ponta a ponta contra a aplicação real: autenticação, permissões verificadas
/// no servidor e o fluxo LOGIN → CLIENTE → VEÍCULO → OS → PENDENTE → EM ANDAMENTO → CONCLUÍDA → HISTÓRICO.
/// </summary>
public class FluxoHttpTestes
{
    private static string PlacaUnica()
    {
        var r = Random.Shared;
        char L() => (char)('A' + r.Next(26));
        return $"{L()}{L()}{L()}{r.Next(10)}{L()}{r.Next(10)}{r.Next(10)}";
    }

    private static string Local(HttpResponseMessage r) => r.Headers.Location?.ToString() ?? string.Empty;

    [Teste("Usuário não autenticado é redirecionado para o login")]
    public async Task BloqueioNaoAutenticado()
    {
        using var nav = new ServidorTeste.Navegador(await ServidorTeste.UrlAsync());
        foreach (var pagina in new[] { "/", "/Clientes", "/Veiculos", "/OrdensServico", "/Historico" })
        {
            var r = await nav.GetAsync(pagina);
            Verificar.Igual(HttpStatusCode.Redirect, r.StatusCode, $"GET {pagina}");
            Verificar.Contem("/Account/Login", Local(r), $"Redirecionamento de {pagina}");
        }
    }

    [Teste("Login inválido exibe mensagem; login válido e logout funcionam")]
    public async Task LoginLogout()
    {
        using var nav = new ServidorTeste.Navegador(await ServidorTeste.UrlAsync());
        var invalido = await nav.PostAsync("/Account/Login", "/Account/Login", new() { ["Login"] = "atendente", ["Senha"] = "errada" });
        Verificar.Igual(HttpStatusCode.OK, invalido.StatusCode, "Login inválido permanece na página");
        Verificar.Contem("Usuário ou senha inválidos", WebUtility.HtmlDecode(await invalido.Content.ReadAsStringAsync()), "Mensagem de erro");

        await nav.EntrarAsync("atendente", "Atendente@123");
        Verificar.Igual(HttpStatusCode.OK, (await nav.GetAsync("/")).StatusCode, "Dashboard após login");

        var sair = await nav.PostAsync("/", "/Account/Logout", []);
        Verificar.Igual(HttpStatusCode.Redirect, sair.StatusCode, "Logout");
        Verificar.Igual(HttpStatusCode.Redirect, (await nav.GetAsync("/")).StatusCode, "Após logout o acesso é bloqueado");
    }

    [Teste("Mecânico não pode cadastrar, editar ou excluir — verificação no servidor")]
    public async Task RestricoesMecanico()
    {
        using var nav = new ServidorTeste.Navegador(await ServidorTeste.UrlAsync());
        await nav.EntrarAsync("mecanico", "Mecanico@123");

        foreach (var pagina in new[] { "/Clientes/Create", "/Veiculos/Create", "/OrdensServico/Create", "/Clientes/Edit/1", "/Veiculos/Delete/1" })
        {
            var r = await nav.GetAsync(pagina);
            Verificar.Contem("/Account/AcessoNegado", Local(r), $"GET {pagina}");
        }

        // POST direto (sem passar pela interface) também é bloqueado
        var post = await nav.PostAsync("/", "/Clientes/Create", new() { ["Nome"] = "Invasor Teste", ["Telefone"] = "16999990000" });
        Verificar.Contem("/Account/AcessoNegado", Local(post), "POST /Clientes/Create");

        var clientes = await nav.HtmlAsync("/Clientes?busca=Invasor");
        Verificar.Falso(clientes.Contains("Invasor Teste"), "Nenhum cliente pode ter sido criado");

        // Consultas continuam liberadas
        Verificar.Igual(HttpStatusCode.OK, (await nav.GetAsync("/Clientes")).StatusCode, "Mecânico consulta clientes");
        Verificar.Igual(HttpStatusCode.OK, (await nav.GetAsync("/Veiculos")).StatusCode, "Mecânico consulta veículos");
    }

    [Teste("POST sem token antiforgery é rejeitado")]
    public async Task Antiforgery()
    {
        using var nav = new ServidorTeste.Navegador(await ServidorTeste.UrlAsync());
        await nav.EntrarAsync("atendente", "Atendente@123");
        var r = await nav.PostAsync("/", "/Clientes/Create", new() { ["Nome"] = "Sem Token", ["Telefone"] = "16999990000" }, comToken: false);
        Verificar.Igual(HttpStatusCode.BadRequest, r.StatusCode, "Resposta sem token");
    }

    [Teste("Fluxo completo: login → cliente → veículo → OS → pendente → em andamento → concluída → histórico")]
    public async Task FluxoCompleto()
    {
        var url = await ServidorTeste.UrlAsync();
        var placa = PlacaUnica();
        var nomeCliente = "Cliente Fluxo " + placa;

        // Atendente: cadastro do cliente seguindo direto para o veículo
        using var atendente = new ServidorTeste.Navegador(url);
        await atendente.EntrarAsync("atendente", "Atendente@123");
        var rCliente = await atendente.PostAsync("/Clientes/Create", "/Clientes/Create",
            new() { ["Nome"] = nomeCliente, ["Telefone"] = "(16) 99876-5432", ["continuar"] = "veiculo" });
        Verificar.Contem("/Veiculos/Create?clienteId=", Local(rCliente), "Continuar para o cadastro do veículo");
        var clienteId = Local(rCliente).Split('=').Last();

        // Veículo com o cliente já selecionado
        var formVeiculo = await atendente.HtmlAsync(Local(rCliente));
        Verificar.Contem(nomeCliente, formVeiculo, "Cliente pré-selecionado no formulário do veículo");
        var rVeiculo = await atendente.PostAsync(Local(rCliente), "/Veiculos/Create",
            new() { ["ClienteId"] = clienteId, ["ClienteFixo"] = "true", ["Placa"] = placa, ["Marca"] = "Fiat", ["Modelo"] = "Strada" });
        Verificar.Contem("/Veiculos/Details/", Local(rVeiculo), "Redireciona para detalhes do veículo");
        var detalhesVeiculo = await atendente.HtmlAsync(Local(rVeiculo));
        Verificar.Contem("Continuar para abertura da ordem de serviço", detalhesVeiculo, "Opção de continuar para a OS");
        var veiculoId = Regex.Match(Local(rVeiculo), @"Details/(\d+)").Groups[1].Value;

        // Placa duplicada é recusada
        var duplicada = await atendente.PostAsync("/Veiculos/Create", "/Veiculos/Create",
            new() { ["ClienteId"] = clienteId, ["Placa"] = placa.ToLowerInvariant(), ["Marca"] = "Fiat", ["Modelo"] = "Uno" });
        Verificar.Igual(HttpStatusCode.OK, duplicada.StatusCode, "Placa duplicada permanece no formulário");
        Verificar.Contem("Já existe um veículo", WebUtility.HtmlDecode(await duplicada.Content.ReadAsStringAsync()), "Mensagem de placa duplicada");

        // Abertura da OS com cliente e veículo mantidos
        var paginaOs = $"/OrdensServico/Create?veiculoId={veiculoId}";
        Verificar.Contem(nomeCliente, await atendente.HtmlAsync(paginaOs), "Cliente e veículo mantidos na abertura da OS");
        var rOs = await atendente.PostAsync(paginaOs, "/OrdensServico/Create",
            new() { ["VeiculoId"] = veiculoId, ["VeiculoFixo"] = "true", ["DescricaoProblema"] = "Luz da injeção acesa e perda de potência." });
        Verificar.Contem("/OrdensServico/Details/", Local(rOs), "OS aberta");
        var numero = Regex.Match(Local(rOs), @"Details/(\d+)").Groups[1].Value;
        var detalhesOs = await atendente.HtmlAsync(Local(rOs));
        Verificar.Contem("Pendente", detalhesOs, "Status inicial Pendente");

        // Mecânico vê a ordem pendente no dashboard e executa o serviço
        using var mecanico = new ServidorTeste.Navegador(url);
        await mecanico.EntrarAsync("mecanico", "Mecanico@123");
        Verificar.Contem($"OS-{int.Parse(numero):D5}", await mecanico.HtmlAsync("/"), "OS aparece no dashboard do mecânico");

        var paginaDetalhes = $"/OrdensServico/Details/{numero}";
        await mecanico.PostAsync(paginaDetalhes, $"/OrdensServico/Iniciar/{numero}", []);
        Verificar.Contem("Em andamento", await mecanico.HtmlAsync(paginaDetalhes), "Status Em andamento");

        await mecanico.PostAsync(paginaDetalhes, $"/OrdensServico/Observacao/{numero}", new() { ["Texto"] = "Diagnóstico: sensor de oxigênio com defeito." });

        await mecanico.PostAsync(paginaDetalhes, $"/OrdensServico/Concluir/{numero}", new() { ["Texto"] = "" });
        Verificar.Contem("Para concluir, registre uma observação", await mecanico.HtmlAsync(paginaDetalhes), "Conclusão sem observação recusada");

        await mecanico.PostAsync(paginaDetalhes, $"/OrdensServico/Concluir/{numero}", new() { ["Texto"] = "Substituído o sensor de oxigênio e apagado o código de falha." });
        var concluida = await mecanico.HtmlAsync(paginaDetalhes);
        Verificar.Contem("Ordem concluída", concluida, "Status Concluída");
        Verificar.Falso(concluida.Contains("Adicionar observação"), "Ordem concluída não oferece novas observações");

        // Tentativa de alteração após conclusão é recusada pelo servidor
        await mecanico.PostAsync(paginaDetalhes, $"/OrdensServico/Observacao/{numero}", new() { ["Texto"] = "Alteração indevida" });
        Verificar.Contem("não pode receber novas observações", await mecanico.HtmlAsync(paginaDetalhes), "Bloqueio após conclusão");

        // Histórico do veículo
        var historico = await mecanico.HtmlAsync($"/Historico/Veiculo/{veiculoId}");
        Verificar.Contem("Luz da injeção acesa", historico, "Problema relatado no histórico");
        Verificar.Contem("Diagnóstico: sensor de oxigênio", historico, "Observação no histórico");
        Verificar.Contem("Substituído o sensor de oxigênio", historico, "Observação de conclusão no histórico");

        // Dados realmente gravados nos arquivos JSON
        using var ordens = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(ServidorTeste.PastaDados, "oficina.json")));
        var ordem = ordens.RootElement.GetProperty("ordens").EnumerateArray().First(o => o.GetProperty("numero").GetInt32() == int.Parse(numero));
        Verificar.Igual("Concluida", ordem.GetProperty("status").GetString(), "Status gravado em oficina.json");
        Verificar.Igual(2, ordem.GetProperty("observacoes").GetArrayLength(), "Observações gravadas em oficina.json");
    }
}
