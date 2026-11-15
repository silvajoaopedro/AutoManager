using AutoManager.Models;
using AutoManager.Services;
using Microsoft.AspNetCore.Builder;

namespace AutoManager.Verificacoes;

public static class VerificacaoOficina
{
    public static void Executar()
    {
        string pasta = Path.Combine(
            Path.GetTempPath(),
            "AutoManagerRegras-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(pasta);

        try
        {
            var builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    ContentRootPath = pasta,
                    EnvironmentName = "Development"
                });

            var repositorio = new RepositorioJson(builder.Environment);
            var oficina = new OficinaService(repositorio);

            Guid autorId = Guid.NewGuid();

            var cliente = oficina.CadastrarCliente(
                "Cliente de teste",
                "16999999999");

            var veiculo = oficina.CadastrarVeiculo(
                cliente.Id,
                "abc-1d23",
                "Marca",
                "Modelo");

            EsperarFalha(() => oficina.CadastrarVeiculo(
                cliente.Id,
                "ABC 1D23",
                "Outra marca",
                "Outro modelo"));

            EsperarFalha(() => oficina.ExcluirCliente(
                cliente.Id,
                confirmado: true));

            // Editar o próprio veículo mantendo a placa deve funcionar.
            oficina.EditarVeiculo(
                veiculo.Id,
                "abc-1d23",
                "Marca atualizada",
                "Modelo atualizado");

            var ordem = oficina.AbrirOrdem(
                veiculo.Id,
                "Problema de teste");

            Conferir(
                ordem.Numero == 1 &&
                ordem.Status == StatusOrdem.Pendente,
                "A ordem inicial foi criada incorretamente.");

            EsperarFalha(() => oficina.ExcluirVeiculo(
                veiculo.Id,
                confirmado: true));

            // Não pode saltar diretamente para Concluída.
            EsperarFalha(() => oficina.AtualizarStatus(
                ordem.Numero,
                StatusOrdem.Concluida,
                "Tentativa de salto.",
                autorId));

            oficina.AtualizarStatus(
                ordem.Numero,
                StatusOrdem.EmAndamento,
                "",
                autorId);

            EsperarFalha(() => oficina.AtualizarStatus(
                ordem.Numero,
                StatusOrdem.Concluida,
                "",
                autorId));

            Conferir(
                oficina.ConsultarOrdem(ordem.Numero)?.Status ==
                    StatusOrdem.EmAndamento,
                "A conclusão recusada alterou o status.");

            oficina.AtualizarStatus(
                ordem.Numero,
                StatusOrdem.Concluida,
                "Serviço realizado.",
                autorId);

            EsperarFalha(() => oficina.RegistrarObservacao(
                ordem.Numero,
                "Tentativa após conclusão.",
                autorId));

            EsperarFalha(() => oficina.AtualizarStatus(
                ordem.Numero,
                StatusOrdem.EmAndamento,
                "",
                autorId));

            // A numeração precisa continuar após recriar o repositório.
            var oficinaReaberta = new OficinaService(
                new RepositorioJson(builder.Environment));

            var segundaOrdem = oficinaReaberta.AbrirOrdem(
                veiculo.Id,
                "Segundo problema");

            Conferir(
                segundaOrdem.Numero == 2,
                "A sequência de numeração não foi preservada.");

            Conferir(
                oficinaReaberta.ConsultarHistorico(veiculo.Id).Count == 2,
                "O histórico não contém todas as ordens.");

            var totais = oficinaReaberta.ContarPorStatus();

            Conferir(
                totais[StatusOrdem.Pendente] == 1 &&
                totais[StatusOrdem.EmAndamento] == 0 &&
                totais[StatusOrdem.Concluida] == 1,
                "Os totais do painel estão incorretos.");

            // Verifica também uma exclusão permitida.
            var clienteSemVeiculo = oficinaReaberta.CadastrarCliente(
                "Cliente sem veículo",
                "16988888888");

            EsperarFalha(() => oficinaReaberta.ExcluirCliente(
                clienteSemVeiculo.Id,
                confirmado: false));

            oficinaReaberta.ExcluirCliente(
                clienteSemVeiculo.Id,
                confirmado: true);

            Conferir(
                oficinaReaberta.ConsultarClientes()
                    .All(item => item.Id != clienteSemVeiculo.Id),
                "O cliente sem vínculos não foi excluído.");

            Console.WriteLine(
                "SUCESSO: placa única, exclusões, transições, " +
                "conclusão, numeração, histórico e painel verificados.");
        }
        finally
        {
            Directory.Delete(pasta, recursive: true);
        }
    }

    private static void EsperarFalha(Action acao)
    {
        try
        {
            acao();
        }
        catch (Exception erro) when (
            erro is InvalidOperationException or ArgumentException)
        {
            return;
        }

        throw new InvalidOperationException(
            "Uma operação que deveria ser bloqueada foi aceita.");
    }

    private static void Conferir(bool condicao, string mensagem)
    {
        if (!condicao)
        {
            throw new InvalidOperationException(mensagem);
        }
    }
}