using AutoManager.Infrastructure;
using AutoManager.Models;
using AutoManager.Persistencia;
using AutoManager.ViewModels;

namespace AutoManager.Services;

public class ClienteService
{
    private readonly JsonDataStore _dados;

    public ClienteService(JsonDataStore dados) => _dados = dados;

    /// <summary>Lista clientes, filtrando por nome, telefone, e-mail ou placa de veículo.</summary>
    public Task<ClienteListaViewModel> ListarAsync(string? busca) => _dados.LerAsync(s =>
    {
        var termo = busca?.Trim() ?? string.Empty;
        var termoDigitos = Formatacao.SomenteDigitos(termo);
        var termoPlaca = Formatacao.NormalizarPlaca(termo);

        var veiculosPorCliente = s.Veiculos.GroupBy(v => v.ClienteId).ToDictionary(g => g.Key, g => g.ToList());
        var ordensPorCliente = s.Ordens.GroupBy(o => o.ClienteId).ToDictionary(g => g.Key, g => g.Count());

        var itens = s.Clientes
            .Select(c =>
            {
                var veiculos = veiculosPorCliente.GetValueOrDefault(c.Id) ?? [];
                return new ClienteListaItem(c, veiculos.Count, ordensPorCliente.GetValueOrDefault(c.Id),
                    string.Join(" ", veiculos.Select(v => v.Placa)));
            })
            .Where(i => termo.Length == 0
                || i.Cliente.Nome.Contains(termo, StringComparison.CurrentCultureIgnoreCase)
                || (i.Cliente.Email?.Contains(termo, StringComparison.OrdinalIgnoreCase) ?? false)
                || (termoDigitos.Length >= 3 && i.Cliente.Telefone.Contains(termoDigitos))
                || (termoPlaca.Length >= 3 && i.Placas.Contains(termoPlaca)))
            .OrderBy(i => i.Cliente.Nome, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new ClienteListaViewModel { Busca = busca, Itens = itens, Total = s.Clientes.Count };
    });

    public Task<Cliente?> ObterAsync(int id) =>
        _dados.LerAsync(s => s.Clientes.FirstOrDefault(c => c.Id == id));

    public Task<List<Cliente>> ListarTodosAsync() =>
        _dados.LerAsync(s => s.Clientes.OrderBy(c => c.Nome, StringComparer.CurrentCultureIgnoreCase).ToList());

    public Task<ClienteDetalhesViewModel?> ObterDetalhesAsync(int id) => _dados.LerAsync(s =>
    {
        var cliente = s.Clientes.FirstOrDefault(c => c.Id == id);
        if (cliente is null) return null;

        return new ClienteDetalhesViewModel
        {
            Cliente = cliente,
            Veiculos = Projecoes.Veiculos(s, s.Veiculos.Where(v => v.ClienteId == id).OrderBy(v => v.Placa)),
            Ordens = Projecoes.Ordens(s, s.Ordens.Where(o => o.ClienteId == id).OrderByDescending(o => o.Numero)),
            BloqueioExclusao = MotivoBloqueioExclusao(s, id)
        };
    });

    public Task<ClienteExclusaoViewModel?> ObterParaExclusaoAsync(int id) => _dados.LerAsync(s =>
    {
        var cliente = s.Clientes.FirstOrDefault(c => c.Id == id);
        if (cliente is null) return null;
        return new ClienteExclusaoViewModel
        {
            Cliente = cliente,
            QuantidadeVeiculos = s.Veiculos.Count(v => v.ClienteId == id),
            QuantidadeOrdens = s.Ordens.Count(o => o.ClienteId == id),
            BloqueioExclusao = MotivoBloqueioExclusao(s, id)
        };
    });

    public Task<Resultado<Cliente>> CriarAsync(ClienteFormViewModel form) => _dados.EscreverAsync(s =>
    {
        var erro = Validar(form, out var nome, out var telefone, out var email);
        if (erro is not null) return erro;

        var cliente = new Cliente
        {
            Id = s.ProximoIdCliente(),
            Nome = nome,
            Telefone = telefone,
            Email = email,
            DataCadastro = DateTime.Now
        };
        s.Clientes.Add(cliente);
        return Resultado<Cliente>.Ok(cliente, $"Cliente {cliente.Nome} cadastrado com sucesso.");
    }, r => r.Sucesso);

    public Task<Resultado<Cliente>> AtualizarAsync(int id, ClienteFormViewModel form) => _dados.EscreverAsync(s =>
    {
        var cliente = s.Clientes.FirstOrDefault(c => c.Id == id);
        if (cliente is null) return Resultado<Cliente>.Falha("Cliente não encontrado.");

        var erro = Validar(form, out var nome, out var telefone, out var email);
        if (erro is not null) return erro;

        cliente.Nome = nome;
        cliente.Telefone = telefone;
        cliente.Email = email;
        cliente.DataAtualizacao = DateTime.Now;
        return Resultado<Cliente>.Ok(cliente, "Dados do cliente atualizados.");
    }, r => r.Sucesso);

    /// <summary>
    /// Exclui o cliente somente quando não há veículos nem ordens vinculados,
    /// preservando os registros do histórico (RN16).
    /// </summary>
    public Task<Resultado> ExcluirAsync(int id) => _dados.EscreverAsync(s =>
    {
        var cliente = s.Clientes.FirstOrDefault(c => c.Id == id);
        if (cliente is null) return Resultado.Falha("Cliente não encontrado.");

        var bloqueio = MotivoBloqueioExclusao(s, id);
        if (bloqueio is not null) return Resultado.Falha(bloqueio);

        s.Clientes.Remove(cliente);
        return Resultado.Ok($"Cliente {cliente.Nome} excluído.");
    }, r => r.Sucesso);

    private static string? MotivoBloqueioExclusao(DataSession s, int clienteId)
    {
        var veiculos = s.Veiculos.Count(v => v.ClienteId == clienteId);
        if (veiculos > 0)
            return $"Este cliente possui {veiculos} veículo(s) vinculado(s). Para preservar o histórico, exclua ou transfira os veículos antes de excluir o cliente.";

        var ordens = s.Ordens.Count(o => o.ClienteId == clienteId);
        if (ordens > 0)
            return $"Este cliente está vinculado a {ordens} ordem(ns) de serviço no histórico e não pode ser excluído.";

        return null;
    }

    private static Resultado<Cliente>? Validar(ClienteFormViewModel form, out string nome, out string telefone, out string? email)
    {
        nome = string.Join(' ', (form.Nome ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries));
        telefone = Formatacao.SomenteDigitos(form.Telefone);
        email = string.IsNullOrWhiteSpace(form.Email) ? null : form.Email.Trim().ToLowerInvariant();

        if (nome.Length < 3 || nome.Length > 120 || !nome.Any(char.IsLetter))
            return Resultado<Cliente>.FalhaCampo(nameof(form.Nome), "Informe o nome completo do cliente (3 a 120 caracteres).");

        if (telefone.Length is < 10 or > 11)
            return Resultado<Cliente>.FalhaCampo(nameof(form.Telefone), "Informe o telefone com DDD, ex.: (16) 99999-9999.");

        if (email is not null && (email.Length > 120 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email)))
            return Resultado<Cliente>.FalhaCampo(nameof(form.Email), "Informe um e-mail válido.");

        return null;
    }
}
