using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AutoManager.Persistencia;

/// <summary>
/// Leitura e gravação segura de um arquivo JSON.
/// A gravação é atômica: o conteúdo é escrito em um arquivo temporário,
/// enviado ao disco e só então substitui o original (mantendo uma cópia .bak).
/// Assim, uma falha no meio da gravação nunca deixa o arquivo corrompido.
/// </summary>
public static class JsonFile
{
    public static readonly JsonSerializerOptions Opcoes = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static T Ler<T>(string caminho) where T : class, new()
    {
        if (!File.Exists(caminho))
            return new T();

        var conteudo = File.ReadAllText(caminho, Encoding.UTF8);
        try
        {
            return JsonSerializer.Deserialize<T>(conteudo, Opcoes)
                ?? throw new JsonException("O arquivo não pode conter um valor nulo.");
        }
        catch (JsonException ex)
        {
            // Não sobrescrevemos um arquivo ilegível: isso causaria perda de dados.
            throw new InvalidDataException(
                $"O arquivo de dados '{Path.GetFileName(caminho)}' está com conteúdo inválido. " +
                $"Verifique o arquivo ou restaure a cópia '{Path.GetFileName(caminho)}.bak'.", ex);
        }
    }

    public static void Gravar<T>(string caminho, T dados)
    {
        var json = JsonSerializer.Serialize(dados, Opcoes);
        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json);
        var temporario = caminho + ".tmp";

        using (var fs = new FileStream(temporario, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            fs.Write(bytes);
            fs.Flush(flushToDisk: true);
        }

        if (File.Exists(caminho))
            File.Replace(temporario, caminho, caminho + ".bak", ignoreMetadataErrors: true);
        else
            File.Move(temporario, caminho);
    }
}
