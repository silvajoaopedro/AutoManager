using AutoManager.Models.Enums;

namespace AutoManager.Tests.Testes;

public class PersistenciaTestes : ContextoServicos
{
    [Teste("Dados gravados em JSON permanecem após reiniciar a aplicação")]
    public async Task PersisteAposReiniciar()
    {
        var cliente = await NovoClienteAsync();
        var veiculo = await NovoVeiculoAsync(cliente);
        var numero = await NovaOrdemAsync(veiculo);
        await Ordens.IniciarAsync(numero, Mecanico);

        Verificar.Verdadeiro(File.Exists(Path.Combine(Pasta, "oficina.json")), "O estado deve existir");

        Recriar(); // nova instância lendo a mesma pasta

        var ordem = await Ordens.ObterAsync(numero);
        Verificar.Igual(StatusOrdem.EmAndamento, ordem?.Status, "Status lido do disco");
        Verificar.Igual(veiculo, ordem!.VeiculoId, "Relacionamento OS → veículo");
        Verificar.Igual(cliente, (await Veiculos.ObterAsync(veiculo))!.ClienteId, "Relacionamento veículo → cliente");
        Verificar.Igual(numero + 1, await NovaOrdemAsync(veiculo), "Numeração continua após reiniciar");
    }

    [Teste("Gravação atômica mantém cópia de segurança (.bak) e não deixa temporários")]
    public async Task GravacaoAtomica()
    {
        await NovoClienteAsync("Primeiro Cliente");
        await NovoClienteAsync("Segundo Cliente", "16911112222");
        Verificar.Verdadeiro(File.Exists(Path.Combine(Pasta, "oficina.json.bak")), "Cópia .bak deve existir");
        Verificar.Falso(File.Exists(Path.Combine(Pasta, "oficina.json.tmp")), "Arquivo temporário não deve sobrar");
    }

    [Teste("Arquivo corrompido não é sobrescrito (evita perda de dados)")]
    public async Task ArquivoCorrompido()
    {
        var caminho = Path.Combine(Pasta, "oficina.json");
        await File.WriteAllTextAsync(caminho, "{ isto não é json válido");
        await Verificar.LancaAsync<InvalidDataException>(() => NovoClienteAsync(), "Gravação sobre arquivo inválido");
        Verificar.Contem("isto não é json", await File.ReadAllTextAsync(caminho), "Conteúdo original preservado");
    }

    [Teste("Falha ao salvar preserva a ordem e o contador anteriores")]
    public async Task AberturaSemGravacaoParcial()
    {
        var cliente = await NovoClienteAsync();
        var veiculo = await NovoVeiculoAsync(cliente);
        var numero = await NovaOrdemAsync(veiculo);
        var caminho = Path.Combine(Pasta, "oficina.json");
        var anterior = await File.ReadAllTextAsync(caminho);
        var temporario = caminho + ".tmp";
        Directory.CreateDirectory(temporario);
        try
        {
            await Verificar.LancaAsync<UnauthorizedAccessException>(
                () => NovaOrdemAsync(veiculo), "Caminho temporário bloqueado");
            Verificar.Igual(anterior, await File.ReadAllTextAsync(caminho), "O estado anterior permanece intacto");
        }
        finally
        {
            Directory.Delete(temporario);
        }

        Recriar();
        Verificar.Igual(numero + 1, await NovaOrdemAsync(veiculo), "A tentativa anterior não consumiu um número");
    }

    [Teste("Operações concorrentes geram identificadores únicos sem perda de registros")]
    public async Task Concorrencia()
    {
        var tarefas = Enumerable.Range(1, 40)
            .Select(i => Clientes.CriarAsync(new ViewModels.ClienteFormViewModel { Nome = $"Cliente {i:00}", Telefone = $"169{i:00000000}" }));
        var resultados = await Task.WhenAll(tarefas);

        Verificar.Verdadeiro(resultados.All(r => r.Sucesso), "Todos os cadastros devem ser aceitos");
        Verificar.Igual(40, resultados.Select(r => r.Valor!.Id).Distinct().Count(), "Identificadores únicos");

        Recriar();
        Verificar.Igual(40, (await Clientes.ListarTodosAsync()).Count, "Todos os registros gravados");
    }
}
