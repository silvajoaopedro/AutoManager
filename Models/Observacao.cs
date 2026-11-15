using System.Text.Json.Serialization;

namespace AutoManager.Models;

public class Observacao
{
    public Guid Id { get; }
    public Guid AutorId { get; }
    public string Texto { get; }
    public DateTimeOffset DataRegistro { get; }

    public Observacao(string texto, Guid autorId)
        : this(
            Guid.NewGuid(),
            autorId,
            texto,
            DateTimeOffset.Now)
    {
    }

    [JsonConstructor]
    public Observacao(
        Guid id,
        Guid autorId,
        string texto,
        DateTimeOffset dataRegistro)
    {
        if (id == Guid.Empty || autorId == Guid.Empty)
        {
            throw new ArgumentException(
                "A observação deve possuir identificador e autor válidos.");
        }

        if (!ValidarTexto(texto))
        {
            throw new ArgumentException(
                "O texto da observação é obrigatório.");
        }

        if (dataRegistro == default)
        {
            throw new ArgumentException(
                "A data da observação é obrigatória.");
        }

        Id = id;
        AutorId = autorId;
        Texto = texto.Trim();
        DataRegistro = dataRegistro;
    }

    public static bool ValidarTexto(string texto)
    {
        return !string.IsNullOrWhiteSpace(texto);
    }
}