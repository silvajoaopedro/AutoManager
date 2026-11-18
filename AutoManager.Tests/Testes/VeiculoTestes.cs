using AutoManager.ViewModels;

namespace AutoManager.Tests.Testes;

public class VeiculoTestes : ContextoServicos
{
    [Teste("veículo vinculado a cliente; um cliente pode ter vários veículos")]
    public async Task CadastroVinculado()
    {
        var cliente = await NovoClienteAsync();
        await NovoVeiculoAsync(cliente, "ABC1D23");
        await NovoVeiculoAsync(cliente, "abc-1234");

        var detalhes = await Clientes.ObterDetalhesAsync(cliente);
        Verificar.Igual(2, detalhes!.Veiculos.Count, "Veículos do cliente");
        Verificar.Verdadeiro(detalhes.Veiculos.Any(v => v.Placa == "ABC1234"), "Placa normalizada sem hífen e em maiúsculas");
    }

    [Teste("veículo sem cliente cadastrado é recusado")]
    public async Task ClienteInexistente()
    {
        var r = await Veiculos.CriarAsync(new VeiculoFormViewModel { ClienteId = 99, Placa = "ABC1D23", Marca = "Fiat", Modelo = "Uno" });
        Verificar.Verdadeiro(r.ErrosCampos.ContainsKey("ClienteId"), "Cliente inexistente deve ser recusado");
    }

    [Teste("placa duplicada é bloqueada (mesmo com hífen ou minúsculas)")]
    public async Task PlacaDuplicada()
    {
        var cliente = await NovoClienteAsync();
        await NovoVeiculoAsync(cliente, "ABC1234");
        var r = await Veiculos.CriarAsync(new VeiculoFormViewModel { ClienteId = cliente, Placa = "abc-1234", Marca = "Fiat", Modelo = "Uno" });
        Verificar.Verdadeiro(r.ErrosCampos.ContainsKey("Placa"), "Placa duplicada deve ser recusada");

        var outro = await NovoVeiculoAsync(cliente, "XYZ9Z99");
        var edicao = await Veiculos.AtualizarAsync(outro, new VeiculoFormViewModel { ClienteId = cliente, Placa = "ABC1234", Marca = "Fiat", Modelo = "Uno" });
        Verificar.Verdadeiro(edicao.ErrosCampos.ContainsKey("Placa"), "Edição para placa existente deve ser recusada");
    }

    [Teste("Placa em formato inválido é recusada")]
    public async Task PlacaInvalida()
    {
        var cliente = await NovoClienteAsync();
        foreach (var placa in new[] { "", "AB1234", "1234ABC", "ABCD123", "ABC12345" })
        {
            var r = await Veiculos.CriarAsync(new VeiculoFormViewModel { ClienteId = cliente, Placa = placa, Marca = "Fiat", Modelo = "Uno" });
            Verificar.Verdadeiro(r.ErrosCampos.ContainsKey("Placa"), $"Placa '{placa}' deveria ser recusada");
        }
    }

    [Teste("Consulta pela placa e edição do veículo")]
    public async Task ConsultaEdicao()
    {
        var cliente = await NovoClienteAsync();
        var outroCliente = await NovoClienteAsync("Ana Luíza", "16977776666");
        var id = await NovoVeiculoAsync(cliente, "GHI4567");

        Verificar.Igual(id, (await Veiculos.ObterPorPlacaAsync("ghi-4567"))?.Id, "Busca exata pela placa");
        Verificar.Igual(1, (await Veiculos.ListarAsync("GHI")).Itens.Count, "Pesquisa parcial pela placa");

        var r = await Veiculos.AtualizarAsync(id, new VeiculoFormViewModel { ClienteId = outroCliente, Placa = "GHI4567", Marca = "Chevrolet", Modelo = "Onix", Ano = 2020 });
        Verificar.Verdadeiro(r.Sucesso, r.Mensagem ?? "");
        var v = await Veiculos.ObterAsync(id);
        Verificar.Igual("Onix", v!.Modelo, "Modelo editado");
        Verificar.Igual(outroCliente, v.ClienteId, "Proprietário alterado");
    }

    [Teste("veículo com ordens não pode ser excluído; sem ordens pode")]
    public async Task Exclusao()
    {
        var cliente = await NovoClienteAsync();
        var comOrdem = await NovoVeiculoAsync(cliente, "ABC1D23");
        var semOrdem = await NovoVeiculoAsync(cliente, "XYZ9Z99");
        await NovaOrdemAsync(comOrdem);

        Verificar.Falso((await Veiculos.ExcluirAsync(comOrdem)).Sucesso, "Veículo com histórico não pode ser excluído");
        Verificar.Verdadeiro((await Veiculos.ExcluirAsync(semOrdem)).Sucesso, "Veículo sem ordens pode ser excluído");
    }

    [Teste("Identificadores de veículo não são reutilizados após exclusão")]
    public async Task IdNaoReutilizado()
    {
        var cliente = await NovoClienteAsync();
        var primeiro = await NovoVeiculoAsync(cliente, "ABC1D23");
        await Veiculos.ExcluirAsync(primeiro);
        var segundo = await NovoVeiculoAsync(cliente, "XYZ9Z99");
        Verificar.Verdadeiro(segundo > primeiro, "Novo veículo deve receber identificador inédito");
    }
}
