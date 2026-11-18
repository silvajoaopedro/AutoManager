using AutoManager.ViewModels;

namespace AutoManager.Tests.Testes;

public class ClienteTestes : ContextoServicos
{
    [Teste("Cadastro de cliente com nome e telefone obrigatórios")]
    public async Task CadastroValido()
    {
        var r = await Clientes.CriarAsync(new ClienteFormViewModel { Nome = "  Maria   Aparecida ", Telefone = "(16) 99999-1234", Email = "MARIA@EMAIL.COM" });
        Verificar.Verdadeiro(r.Sucesso, r.Mensagem ?? "");
        Verificar.Igual("Maria Aparecida", r.Valor!.Nome, "Nome normalizado");
        Verificar.Igual("16999991234", r.Valor.Telefone, "Telefone só com dígitos");
        Verificar.Igual("maria@email.com", r.Valor.Email, "E-mail normalizado");
    }

    [Teste("Validação recusa nome e telefone inválidos")]
    public async Task ValidacaoCampos()
    {
        var semNome = await Clientes.CriarAsync(new ClienteFormViewModel { Nome = "  ", Telefone = "16999991234" });
        Verificar.Verdadeiro(semNome.ErrosCampos.ContainsKey("Nome"), "Nome vazio deve ser recusado");

        var telefoneCurto = await Clientes.CriarAsync(new ClienteFormViewModel { Nome = "João Silva", Telefone = "1234" });
        Verificar.Verdadeiro(telefoneCurto.ErrosCampos.ContainsKey("Telefone"), "Telefone curto deve ser recusado");

        var emailInvalido = await Clientes.CriarAsync(new ClienteFormViewModel { Nome = "João Silva", Telefone = "16999991234", Email = "invalido" });
        Verificar.Verdadeiro(emailInvalido.ErrosCampos.ContainsKey("Email"), "E-mail inválido deve ser recusado");

        Verificar.Igual(0, (await Clientes.ListarTodosAsync()).Count, "Nada pode ser gravado após falhas");
    }

    [Teste("Consulta, pesquisa e edição de cliente")]
    public async Task ConsultaEdicao()
    {
        var id = await NovoClienteAsync("José Carlos", "16988887777");
        await NovoVeiculoAsync(id, "FRT2B45");

        Verificar.Igual(1, (await Clientes.ListarAsync("josé")).Itens.Count, "Busca por nome");
        Verificar.Igual(1, (await Clientes.ListarAsync("98888")).Itens.Count, "Busca por telefone");
        Verificar.Igual(1, (await Clientes.ListarAsync("frt-2b45")).Itens.Count, "Busca pela placa do veículo");
        Verificar.Igual(0, (await Clientes.ListarAsync("inexistente")).Itens.Count, "Busca sem resultado");

        var r = await Clientes.AtualizarAsync(id, new ClienteFormViewModel { Nome = "José Carlos Pereira", Telefone = "1633334444" });
        Verificar.Verdadeiro(r.Sucesso, r.Mensagem ?? "");
        var atualizado = await Clientes.ObterAsync(id);
        Verificar.Igual("José Carlos Pereira", atualizado!.Nome, "Nome editado");
        Verificar.Verdadeiro(atualizado.DataAtualizacao.HasValue, "Data de atualização registrada");
    }

    [Teste("Exclusão permitida quando o cliente não possui vínculos")]
    public async Task ExclusaoPermitida()
    {
        var id = await NovoClienteAsync();
        var r = await Clientes.ExcluirAsync(id);
        Verificar.Verdadeiro(r.Sucesso, r.Mensagem ?? "");
        Verificar.Igual(null, await Clientes.ObterAsync(id), "Cliente removido");
    }

    [Teste("exclusão bloqueada quando o cliente possui veículos")]
    public async Task ExclusaoBloqueada()
    {
        var id = await NovoClienteAsync();
        await NovoVeiculoAsync(id);
        var r = await Clientes.ExcluirAsync(id);
        Verificar.Falso(r.Sucesso, "Exclusão deveria ser bloqueada");
        Verificar.Contem("veículo", r.Mensagem, "Motivo do bloqueio");
        Verificar.Verdadeiro(await Clientes.ObterAsync(id) is not null, "Cliente deve continuar cadastrado");
    }
}
