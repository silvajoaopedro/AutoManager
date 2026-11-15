using System.Text.Json.Serialization;

namespace AutoManager.Models;

public class Cliente
{
    [JsonInclude]
    public Guid Id { get; private set; } = Guid.NewGuid();

    [JsonInclude]
    public string Nome { get; private set; }

    [JsonInclude]
    public string Telefone { get; private set; }

    [JsonConstructor]
    public Cliente(string nome, string telefone)
    {
        Nome = ValidarCampo(nome, "O nome é obrigatório.");
        Telefone = ValidarCampo(telefone, "O telefone é obrigatório.");
    }

    public void Atualizar(string nome, string telefone)
    {
        // Valida ambos antes de modificar o objeto.
        string nomeValidado = ValidarCampo(
            nome, "O nome é obrigatório.");

        string telefoneValidado = ValidarCampo(
            telefone, "O telefone é obrigatório.");

        Nome = nomeValidado;
        Telefone = telefoneValidado;
    }

    private static string ValidarCampo(string valor, string mensagem)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(mensagem);
        }

        return valor.Trim();
    }
}