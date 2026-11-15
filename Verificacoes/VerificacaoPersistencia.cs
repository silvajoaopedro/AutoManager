using AutoManager.Models;
using AutoManager.Services;
using Microsoft.AspNetCore.Builder;

namespace AutoManager.Verificacoes;

public static class VerificacaoPersistencia
{
    public static void Executar()
    {
        string pastaTemporaria = Path.Combine(
            Path.GetTempPath(),
            "AutoManagerTeste-" + Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(pastaTemporaria);

        try
        {
            var builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    ContentRootPath = pastaTemporaria,
                    EnvironmentName = "Development"
                });

            var repositorio = new RepositorioJson(
                builder.Environment);

            var cliente = new Cliente(
                "Cliente de teste",
                "16999999999");

            var veiculo = new Veiculo(
                cliente.Id,
                "abc-1d23",
                "Marca de teste",
                "Modelo de teste");

            Guid autorId = Guid.NewGuid();

            repositorio.ExecutarTransacao(dados =>
            {
                dados.Clientes.Add(cliente);
                dados.Veiculos.Add(veiculo);

                dados.UltimoNumeroOrdem++;

                var ordem = new OrdemServico(
                    dados.UltimoNumeroOrdem,
                    veiculo.Id,
                    "Problema de teste");

                ordem.Iniciar();

                ordem.RegistrarObservacao(
                    "Serviço iniciado.",
                    autorId);

                dados.Ordens.Add(ordem);
            });

            var novoRepositorio = new RepositorioJson(
                builder.Environment);

            EstadoOficina recuperado = novoRepositorio.Ler();

            Conferir(
                recuperado.Clientes.Single().Id == cliente.Id,
                "O identificador do cliente não foi preservado.");

            Conferir(
                recuperado.Veiculos.Single().ClienteId == cliente.Id,
                "O vínculo com o cliente não foi preservado.");

            Conferir(
                recuperado.Veiculos.Single().Placa == "ABC1D23",
                "A placa normalizada não foi preservada.");

            var ordemRecuperada = recuperado.Ordens.Single();

            Conferir(
                ordemRecuperada.Status == StatusOrdem.EmAndamento,
                "O status não foi recuperado corretamente.");

            Conferir(
                ordemRecuperada.Observacoes.Single().AutorId == autorId,
                "A observação não foi recuperada corretamente.");

            Conferir(
                recuperado.UltimoNumeroOrdem == 1,
                "O contador não foi preservado.");

            novoRepositorio.ExecutarTransacao(dados =>
            {
                dados.Ordens.Single().Concluir(
                    "Serviço concluído.",
                    autorId);
            });

            var concluida = novoRepositorio.Ler().Ordens.Single();

            Conferir(
                concluida.Status == StatusOrdem.Concluida &&
                concluida.Observacoes.Count == 2,
                "A conclusão e sua observação não foram preservadas.");

            string caminhoArquivo = Path.Combine(
                pastaTemporaria,
                "App_Data",
                "oficina.json");

            string antes = File.ReadAllText(caminhoArquivo);

            bool alteracaoBloqueada = false;

            try
            {
                novoRepositorio.ExecutarTransacao(dados =>
                {
                    dados.Ordens.Single().RegistrarObservacao(
                        "Tentativa após conclusão.",
                        autorId);
                });
            }
            catch (InvalidOperationException)
            {
                alteracaoBloqueada = true;
            }

            Conferir(
                alteracaoBloqueada,
                "A ordem concluída aceitou uma alteração.");

            Conferir(
                File.ReadAllText(caminhoArquivo) == antes,
                "A operação recusada alterou o arquivo.");

            Console.WriteLine(
                "SUCESSO: gravação, recuperação, vínculos, contador, " +
                "observações e bloqueio após conclusão verificados.");
        }
        finally
        {
            Directory.Delete(pastaTemporaria, recursive: true);
        }
    }

    private static void Conferir(
        bool condicao,
        string mensagem)
    {
        if (!condicao)
        {
            throw new InvalidOperationException(mensagem);
        }
    }
}