using AutoManager.Models;

namespace AutoManager.Services;

public class OficinaService
{
    private readonly RepositorioJson _repositorio;

    public OficinaService(RepositorioJson repositorio)
    {
        _repositorio = repositorio;
    }

    // CLIENTES

    public Cliente CadastrarCliente(string nome, string telefone)
    {
        var cliente = new Cliente(nome, telefone);

        _repositorio.ExecutarTransacao(dados =>
        {
            dados.Clientes.Add(cliente);
        });

        return cliente;
    }

    public List<Cliente> ConsultarClientes()
    {
        return _repositorio.Ler().Clientes
            .OrderBy(cliente => cliente.Nome)
            .ToList();
    }

    public void EditarCliente(
        Guid id,
        string nome,
        string telefone)
    {
        _repositorio.ExecutarTransacao(dados =>
        {
            Cliente cliente = EncontrarCliente(dados, id);
            cliente.Atualizar(nome, telefone);
        });
    }

    public void ExcluirCliente(Guid id, bool confirmado)
    {
        if (!confirmado)
        {
            throw new InvalidOperationException(
                "A exclusão do cliente exige confirmação.");
        }

        _repositorio.ExecutarTransacao(dados =>
        {
            Cliente cliente = EncontrarCliente(dados, id);

            if (dados.Veiculos.Any(veiculo => veiculo.ClienteId == id))
            {
                throw new InvalidOperationException(
                    "O cliente possui veículos vinculados e não pode ser excluído.");
            }

            dados.Clientes.Remove(cliente);
        });
    }

    // VEÍCULOS

    public Veiculo CadastrarVeiculo(
        Guid clienteId,
        string placa,
        string marca,
        string modelo)
    {
        var veiculo = new Veiculo(clienteId, placa, marca, modelo);

        _repositorio.ExecutarTransacao(dados =>
        {
            EncontrarCliente(dados, clienteId);
            GarantirPlacaUnica(dados, veiculo.Placa);

            dados.Veiculos.Add(veiculo);
        });

        return veiculo;
    }

    public List<Veiculo> ConsultarVeiculos()
    {
        return _repositorio.Ler().Veiculos
            .OrderBy(veiculo => veiculo.Placa)
            .ToList();
    }

    public void EditarVeiculo(
        Guid id,
        string placa,
        string marca,
        string modelo)
    {
        string placaNormalizada = Veiculo.NormalizarPlaca(placa);

        _repositorio.ExecutarTransacao(dados =>
        {
            Veiculo veiculo = EncontrarVeiculo(dados, id);

            GarantirPlacaUnica(dados, placaNormalizada, id);

            // O proprietário permanece o mesmo.
            veiculo.Atualizar(placaNormalizada, marca, modelo);
        });
    }

    public void ExcluirVeiculo(Guid id, bool confirmado)
    {
        if (!confirmado)
        {
            throw new InvalidOperationException(
                "A exclusão do veículo exige confirmação.");
        }

        _repositorio.ExecutarTransacao(dados =>
        {
            Veiculo veiculo = EncontrarVeiculo(dados, id);

            if (dados.Ordens.Any(ordem => ordem.VeiculoId == id))
            {
                throw new InvalidOperationException(
                    "O veículo possui ordens de serviço e não pode ser excluído.");
            }

            dados.Veiculos.Remove(veiculo);
        });
    }

    // ORDENS DE SERVIÇO

    public OrdemServico AbrirOrdem(
        Guid veiculoId,
        string problema)
    {
        OrdemServico? novaOrdem = null;

        _repositorio.ExecutarTransacao(dados =>
        {
            EncontrarVeiculo(dados, veiculoId);

            long numero = checked(dados.UltimoNumeroOrdem + 1);

            novaOrdem = new OrdemServico(
                numero,
                veiculoId,
                problema);

            dados.Ordens.Add(novaOrdem);
            dados.UltimoNumeroOrdem = numero;
        });

        return novaOrdem!;
    }

    public List<OrdemServico> ConsultarOrdens()
    {
        return _repositorio.Ler().Ordens
            .OrderByDescending(ordem => ordem.Numero)
            .ToList();
    }

    public OrdemServico? ConsultarOrdem(long numero)
    {
        return _repositorio.Ler().Ordens
            .FirstOrDefault(ordem => ordem.Numero == numero);
    }

    public void AtualizarStatus(
        long numero,
        StatusOrdem destino,
        string texto,
        Guid autorId)
    {
        if (autorId == Guid.Empty)
        {
            throw new ArgumentException(
                "O usuário responsável é obrigatório.");
        }

        if (!Enum.IsDefined(typeof(StatusOrdem), destino))
        {
            throw new ArgumentException("Status inválido.");
        }

        _repositorio.ExecutarTransacao(dados =>
        {
            OrdemServico ordem = EncontrarOrdem(dados, numero);

            if (ordem.Status == StatusOrdem.Pendente &&
                destino == StatusOrdem.EmAndamento)
            {
                ordem.Iniciar();
            }
            else if (ordem.Status == StatusOrdem.EmAndamento &&
                     destino == StatusOrdem.Concluida)
            {
                ordem.Concluir(texto, autorId);
            }
            else
            {
                throw new InvalidOperationException(
                    "Transição não permitida. Siga a sequência: " +
                    "Pendente, Em andamento e Concluída.");
            }
        });
    }

    public void RegistrarObservacao(
        long numero,
        string texto,
        Guid autorId)
    {
        _repositorio.ExecutarTransacao(dados =>
        {
            OrdemServico ordem = EncontrarOrdem(dados, numero);

            ordem.RegistrarObservacao(texto, autorId);
        });
    }

    public List<OrdemServico> ConsultarHistorico(Guid veiculoId)
    {
        EstadoOficina dados = _repositorio.Ler();

        EncontrarVeiculo(dados, veiculoId);

        return dados.Ordens
            .Where(ordem => ordem.VeiculoId == veiculoId)
            .OrderByDescending(ordem => ordem.Numero)
            .ToList();
    }

    public Dictionary<StatusOrdem, int> ContarPorStatus()
    {
        EstadoOficina dados = _repositorio.Ler();

        return Enum.GetValues<StatusOrdem>()
            .ToDictionary(
                status => status,
                status => dados.Ordens.Count(
                    ordem => ordem.Status == status));
    }

    // VALIDAÇÕES INTERNAS

    private static Cliente EncontrarCliente(
        EstadoOficina dados,
        Guid id)
    {
        return dados.Clientes.FirstOrDefault(cliente => cliente.Id == id)
            ?? throw new InvalidOperationException(
                "Cliente não encontrado.");
    }

    private static Veiculo EncontrarVeiculo(
        EstadoOficina dados,
        Guid id)
    {
        return dados.Veiculos.FirstOrDefault(veiculo => veiculo.Id == id)
            ?? throw new InvalidOperationException(
                "Veículo não encontrado.");
    }

    private static OrdemServico EncontrarOrdem(
        EstadoOficina dados,
        long numero)
    {
        return dados.Ordens.FirstOrDefault(ordem => ordem.Numero == numero)
            ?? throw new InvalidOperationException(
                "Ordem de serviço não encontrada.");
    }

    private static void GarantirPlacaUnica(
        EstadoOficina dados,
        string placa,
        Guid ignorarId = default)
    {
        string placaNormalizada = Veiculo.NormalizarPlaca(placa);

        bool duplicada = dados.Veiculos.Any(veiculo =>
            veiculo.Id != ignorarId &&
            Veiculo.NormalizarPlaca(veiculo.Placa) == placaNormalizada);

        if (duplicada)
        {
            throw new InvalidOperationException(
                "Já existe um veículo cadastrado com essa placa.");
        }
    }
}