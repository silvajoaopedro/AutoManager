using System.Text.Json.Serialization;

namespace AutoManager.Models;

public class Veiculo
{
    [JsonInclude]
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid ClienteId { get; }

    [JsonInclude]
    public string Placa { get; private set; }

    [JsonInclude]
    public string Marca { get; private set; }

    [JsonInclude]
    public string Modelo { get; private set; }

    [JsonConstructor]
    public Veiculo(
        Guid clienteId,
        string placa,
        string marca,
        string modelo)
    {
        if (clienteId == Guid.Empty)
        {
            throw new ArgumentException(
                "O veículo deve estar vinculado a um cliente.");
        }

        ClienteId = clienteId;

        Placa = NormalizarPlaca(placa);
        Marca = ValidarCampo(marca, "A marca é obrigatória.");
        Modelo = ValidarCampo(modelo, "O modelo é obrigatório.");
    }

    public void Atualizar(
        string placa,
        string marca,
        string modelo)
    {
        string placaValidada = NormalizarPlaca(placa);
        string marcaValidada = ValidarCampo(
            marca, "A marca é obrigatória.");
        string modeloValidado = ValidarCampo(
            modelo, "O modelo é obrigatório.");

        Placa = placaValidada;
        Marca = marcaValidada;
        Modelo = modeloValidado;
    }

    public static string NormalizarPlaca(string placa)
    {
        if (string.IsNullOrWhiteSpace(placa))
        {
            throw new ArgumentException("A placa é obrigatória.");
        }

        string placaNormalizada = new string(
            placa.Where(c => !char.IsWhiteSpace(c) && c != '-')
                 .ToArray())
            .ToUpperInvariant();

        if (placaNormalizada.Length == 0)
        {
            throw new ArgumentException("A placa é obrigatória.");
        }

        return placaNormalizada;
    }

    private static string ValidarCampo(
        string valor,
        string mensagem)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(mensagem);
        }

        return valor.Trim();
    }
}