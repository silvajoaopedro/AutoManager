using System.Text.Json;
using AutoManager.Models;
using Microsoft.AspNetCore.Hosting;

namespace AutoManager.Services;

public class RepositorioJson
{
    private readonly string _caminho;

    private readonly SemaphoreSlim _bloqueio = new(1, 1);

    private readonly JsonSerializerOptions _opcoes = new()
    {
        WriteIndented = true
    };

    public RepositorioJson(IWebHostEnvironment ambiente)
    {
        string diretorio = Path.Combine(
            ambiente.ContentRootPath,
            "App_Data");

        Directory.CreateDirectory(diretorio);

        _caminho = Path.Combine(diretorio, "oficina.json");

        if (!File.Exists(_caminho))
        {
            GravarAtomicamente(new EstadoOficina());
        }
    }

    public EstadoOficina Ler()
    {
        _bloqueio.Wait();

        try
        {
            return LerArquivo();
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    public void ExecutarTransacao(Action<EstadoOficina> acao)
    {
        ArgumentNullException.ThrowIfNull(acao);

        _bloqueio.Wait();

        try
        {
            EstadoOficina dados = LerArquivo();

            acao(dados);

            ValidarEstado(dados);
            GravarAtomicamente(dados);
        }
        finally
        {
            _bloqueio.Release();
        }
    }

    private EstadoOficina LerArquivo()
    {
        using var arquivo = new FileStream(
            _caminho,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        EstadoOficina? dados =
            JsonSerializer.Deserialize<EstadoOficina>(
                arquivo,
                _opcoes);

        if (dados is null)
        {
            throw new InvalidOperationException(
                "Não foi possível carregar os dados da oficina.");
        }

        ValidarEstado(dados);

        return dados;
    }

    private static void ValidarEstado(EstadoOficina dados)
    {
        if (dados.Clientes is null ||
            dados.Veiculos is null ||
            dados.Ordens is null)
        {
            throw new InvalidOperationException(
                "A estrutura dos dados da oficina é inválida.");
        }

        if (dados.Clientes.Any(cliente => cliente is null) ||
            dados.Veiculos.Any(veiculo => veiculo is null) ||
            dados.Ordens.Any(ordem => ordem is null))
        {
            throw new InvalidOperationException(
                "Os dados possuem registros inválidos.");
        }

        long maiorNumero = dados.Ordens.Count == 0
            ? 0
            : dados.Ordens.Max(ordem => ordem.Numero);

        if (dados.UltimoNumeroOrdem < 0 ||
            dados.UltimoNumeroOrdem < maiorNumero)
        {
            throw new InvalidOperationException(
                "O controle de numeração das ordens é inválido.");
        }

        if (dados.Ordens.Select(ordem => ordem.Numero)
            .Distinct().Count() != dados.Ordens.Count)
        {
            throw new InvalidOperationException(
                "Existem números de ordens duplicados.");
        }
    }

    private void GravarAtomicamente(EstadoOficina dados)
    {
        string temporario =
            _caminho + "." + Guid.NewGuid().ToString("N") + ".tmp";

        try
        {
            using (var arquivo = new FileStream(
                temporario,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None))
            {
                JsonSerializer.Serialize(arquivo, dados, _opcoes);

                arquivo.Flush(flushToDisk: true);
            }

            // O temporário já está completo e fechado.
            // Substitui o destino sem excluí-lo previamente.
            File.Move(temporario, _caminho, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporario))
            {
                File.Delete(temporario);
            }
        }
    }
}