using AutoManager.Models.Enums;
using AutoManager.ViewModels;

namespace AutoManager.Tests.Testes;

public class OrdemServicoTestes : ContextoServicos
{
    [Teste("RN07/RN08/RN09: abertura com numeração automática e status Pendente")]
    public async Task Abertura()
    {
        var cliente = await NovoClienteAsync();
        var veiculo = await NovoVeiculoAsync(cliente);

        var r1 = await Ordens.AbrirAsync(new AbrirOrdemViewModel { VeiculoId = veiculo, DescricaoProblema = "Luz da injeção acesa." }, Atendente);
        var r2 = await Ordens.AbrirAsync(new AbrirOrdemViewModel { VeiculoId = veiculo, DescricaoProblema = "Troca de óleo e filtros." }, Atendente);

        Verificar.Verdadeiro(r1.Sucesso && r2.Sucesso, "Ordens devem ser abertas");
        Verificar.Igual(1, r1.Valor!.Numero, "Primeiro número");
        Verificar.Igual(2, r2.Valor!.Numero, "Segundo número");
        Verificar.Igual(StatusOrdem.Pendente, r1.Valor.Status, "Status inicial");
        Verificar.Igual(cliente, r1.Valor.ClienteId, "Cliente obtido do veículo");
        Verificar.Igual("OS-00001", r1.Valor.NumeroFormatado, "Número formatado");
    }

    [Teste("RN07: ordem sem veículo cadastrado ou sem descrição é recusada")]
    public async Task AberturaInvalida()
    {
        var semVeiculo = await Ordens.AbrirAsync(new AbrirOrdemViewModel { VeiculoId = 42, DescricaoProblema = "Barulho no motor." }, Atendente);
        Verificar.Verdadeiro(semVeiculo.ErrosCampos.ContainsKey("VeiculoId"), "Veículo inexistente");

        var veiculo = await NovoVeiculoAsync(await NovoClienteAsync());
        var curta = await Ordens.AbrirAsync(new AbrirOrdemViewModel { VeiculoId = veiculo, DescricaoProblema = "ruim" }, Atendente);
        Verificar.Verdadeiro(curta.ErrosCampos.ContainsKey("DescricaoProblema"), "Descrição muito curta");
    }

    [Teste("RN11/RN12: não é permitido pular nem retroceder etapas")]
    public async Task Transicoes()
    {
        var numero = await NovaOrdemAsync(await NovoVeiculoAsync(await NovoClienteAsync()));

        var pular = await Ordens.ConcluirAsync(numero, "Serviço executado com sucesso.", Mecanico);
        Verificar.Falso(pular.Sucesso, "Pendente → Concluída deve ser recusado");

        var voltar = await Ordens.AtualizarStatusAsync(numero, StatusOrdem.Pendente, null, Mecanico);
        Verificar.Falso(voltar.Sucesso, "Manter/voltar para Pendente deve ser recusado");

        var iniciar = await Ordens.IniciarAsync(numero, Mecanico);
        Verificar.Verdadeiro(iniciar.Sucesso, iniciar.Mensagem ?? "");
        Verificar.Igual(StatusOrdem.EmAndamento, iniciar.Valor!.Status, "Status após iniciar");
        Verificar.Igual("Mecânico Teste", iniciar.Valor.IniciadaPorNome, "Responsável pelo início");

        var retroceder = await Ordens.AtualizarStatusAsync(numero, StatusOrdem.Pendente, null, Mecanico);
        Verificar.Falso(retroceder.Sucesso, "Em andamento → Pendente deve ser recusado");
        Verificar.Falso((await Ordens.IniciarAsync(numero, Mecanico)).Sucesso, "Iniciar duas vezes deve ser recusado");
    }

    [Teste("RN10: atendente também pode atualizar o status")]
    public async Task AtendenteAtualizaStatus()
    {
        var numero = await NovaOrdemAsync(await NovoVeiculoAsync(await NovoClienteAsync()));
        Verificar.Verdadeiro((await Ordens.IniciarAsync(numero, Atendente)).Sucesso, "Atendente inicia");
        Verificar.Verdadeiro((await Ordens.ConcluirAsync(numero, "Cliente retirou o veículo após o reparo.", Atendente)).Sucesso, "Atendente conclui");
    }

    [Teste("Registro de observações associadas à ordem")]
    public async Task Observacoes()
    {
        var numero = await NovaOrdemAsync(await NovoVeiculoAsync(await NovoClienteAsync()));
        await Ordens.IniciarAsync(numero, Mecanico);
        var r = await Ordens.RegistrarObservacaoAsync(numero, "Diagnóstico: bobina de ignição com falha.", Mecanico);
        Verificar.Verdadeiro(r.Sucesso, r.Mensagem ?? "");

        var vazia = await Ordens.RegistrarObservacaoAsync(numero, "  ", Mecanico);
        Verificar.Falso(vazia.Sucesso, "Observação vazia deve ser recusada");

        var ordem = await Ordens.ObterAsync(numero);
        Verificar.Igual(1, ordem!.Observacoes.Count, "Quantidade de observações");
        Verificar.Igual(PerfilUsuario.Mecanico, ordem.Observacoes[0].UsuarioPerfil, "Perfil do autor");
    }

    [Teste("RN13: conclusão exige observação sobre o serviço realizado")]
    public async Task ConclusaoExigeObservacao()
    {
        var numero = await NovaOrdemAsync(await NovoVeiculoAsync(await NovoClienteAsync()));
        await Ordens.IniciarAsync(numero, Mecanico);

        var semTexto = await Ordens.ConcluirAsync(numero, "", Mecanico);
        Verificar.Falso(semTexto.Sucesso, "Conclusão sem observação deve ser recusada");
        Verificar.Igual(StatusOrdem.EmAndamento, (await Ordens.ObterAsync(numero))!.Status, "Status não pode mudar após falha");

        var ok = await Ordens.ConcluirAsync(numero, "Substituída a bobina de ignição e testado.", Mecanico);
        Verificar.Verdadeiro(ok.Sucesso, ok.Mensagem ?? "");
        var ordem = await Ordens.ObterAsync(numero);
        Verificar.Igual(StatusOrdem.Concluida, ordem!.Status, "Status final");
        Verificar.Verdadeiro(ordem.DataConclusao.HasValue, "Data de conclusão registrada");
        Verificar.Igual(TipoObservacao.Conclusao, ordem.Observacoes.Last().Tipo, "Observação de conclusão registrada");
    }

    [Teste("RN14: ordem concluída não pode ser reaberta nem alterada")]
    public async Task ConcluidaBloqueada()
    {
        var numero = await NovaOrdemAsync(await NovoVeiculoAsync(await NovoClienteAsync()));
        await Ordens.IniciarAsync(numero, Mecanico);
        await Ordens.ConcluirAsync(numero, "Serviço concluído e testado.", Mecanico);

        foreach (var status in Enum.GetValues<StatusOrdem>())
            Verificar.Falso((await Ordens.AtualizarStatusAsync(numero, status, "Tentativa de alteração", Atendente)).Sucesso, $"Mudar para {status} deve ser recusado");

        Verificar.Falso((await Ordens.RegistrarObservacaoAsync(numero, "Nova observação", Atendente)).Sucesso, "Nova observação deve ser recusada");
        Verificar.Igual(1, (await Ordens.ObterAsync(numero))!.Observacoes.Count, "Observações inalteradas");
    }

    [Teste("RN15: histórico do veículo reúne ordens, datas, status e observações")]
    public async Task HistoricoVeiculo()
    {
        var veiculo = await NovoVeiculoAsync(await NovoClienteAsync());
        var primeira = await NovaOrdemAsync(veiculo, "Revisão dos 40 mil km.");
        await Ordens.IniciarAsync(primeira, Mecanico);
        await Ordens.ConcluirAsync(primeira, "Revisão realizada conforme manual.", Mecanico);
        await NovaOrdemAsync(veiculo, "Ruído no freio traseiro.");

        var detalhes = await Veiculos.ObterDetalhesAsync(veiculo);
        Verificar.Igual(2, detalhes!.Historico.Count, "Ordens no histórico");
        Verificar.Igual(1, detalhes.Concluidas, "Concluídas");
        Verificar.Igual(1, detalhes.EmAberto, "Em aberto");
        Verificar.Igual(2, detalhes.Historico[0].Numero, "Mais recente primeiro");

        var lista = await Historico.ListarAsync("ABC1D23");
        Verificar.Igual(1, lista.Itens.Count, "Pesquisa do histórico pela placa");
    }

    [Teste("Dashboard: indicadores calculados com os dados reais")]
    public async Task Indicadores()
    {
        var veiculo = await NovoVeiculoAsync(await NovoClienteAsync());
        var a = await NovaOrdemAsync(veiculo);
        var b = await NovaOrdemAsync(veiculo);
        await NovaOrdemAsync(veiculo);
        await Ordens.IniciarAsync(a, Mecanico);
        await Ordens.IniciarAsync(b, Mecanico);
        await Ordens.ConcluirAsync(b, "Serviço finalizado e testado.", Mecanico);

        var d = await Dashboard.ObterAsync();
        Verificar.Igual(3, d.TotalOrdens, "Total");
        Verificar.Igual(1, d.Pendentes, "Pendentes");
        Verificar.Igual(1, d.EmAndamento, "Em andamento");
        Verificar.Igual(1, d.Concluidas, "Concluídas");
        Verificar.Igual(3, d.Recentes.Count, "Recentes");
    }
}
