# AutoManager

Sistema web de gerenciamento de oficina automotiva: cadastro de clientes e veículos, abertura e acompanhamento de ordens de serviço, atualização de status, registro de observações e histórico de serviços.

Trabalho da disciplina **Análise e Projeto de Sistemas**.

## Tecnologias

| Tecnologia | Uso |
|---|---|
| C# / .NET 10 | Linguagem e plataforma |
| ASP.NET Core MVC + Razor | Framework web e páginas |
| HTML5, CSS3, JavaScript | Interface (CSS e JS em arquivos separados) |
| Bootstrap 5.3 + Bootstrap Icons | Componentes e responsividade (servidos localmente) |
| JSON | Persistência de dados (sem banco de dados) |
| Git / GitHub | Controle de versões |

## Como executar

Pré-requisito: [.NET SDK 10](https://dotnet.microsoft.com/download).

```bash
cd AutoManager
dotnet run
```

Acesse o endereço exibido no terminal (por padrão `http://localhost:5009`).

### Usuários iniciais

Na primeira execução, o sistema cria os usuários abaixo em `AutoManager/Data/usuarios.json` (senhas gravadas com hash e salt):

| Perfil | Usuário | Senha |
|---|---|---|
| Atendente | `atendente` | `Atendente@123` |
| Mecânico | `mecanico` | `Mecanico@123` |

Os usuários e senhas iniciais podem ser alterados na seção `UsuariosIniciais` do `appsettings.json` **antes** da primeira execução.

## Testes automatizados

```bash
cd AutoManager.Tests
dotnet run
```

São 33 testes, sem dependências externas, cobrindo autenticação, clientes, veículos, ordens de serviço, regras de negócio (RN01–RN16), persistência e o fluxo completo via HTTP com a aplicação real em execução.

## Fluxo principal

**LOGIN → DASHBOARD → CLIENTE → VEÍCULO → ABERTURA DA OS → PENDENTE → EM ANDAMENTO → CONCLUÍDA → HISTÓRICO**

1. A atendente pesquisa o cliente ou a placa. Se não houver cadastro, cadastra o cliente e usa **Salvar e cadastrar veículo**.
2. O veículo é cadastrado com o cliente já selecionado. Após salvar, aparece **Continuar para abertura da ordem de serviço**.
3. A OS é aberta com cliente e veículo mantidos, número automático e status **Pendente**.
4. O mecânico vê a ordem no dashboard e clica em **Iniciar serviço** (→ **Em andamento**).
5. Durante o serviço, são registradas observações.
6. Para **concluir**, é obrigatório registrar uma observação sobre o serviço realizado. Depois de concluída, a ordem não pode ser reaberta, alterada ou excluída.
7. A ordem permanece no **histórico** do veículo.

## Perfis e permissões

| Funcionalidade | Atendente | Mecânico |
|---|:---:|:---:|
| Login, dashboard e logout | ✔ | ✔ |
| Consultar clientes, veículos, ordens e histórico | ✔ | ✔ |
| Cadastrar, editar e excluir clientes e veículos | ✔ | — |
| Abrir ordem de serviço | ✔ | — |
| Iniciar, registrar observações e concluir ordens | ✔ | ✔ |

As permissões são verificadas **no servidor** (`[Authorize(Roles = ...)]`), não apenas escondidas na interface.

## Estrutura do projeto

```text
AutoManager/                    Solution
├── AutoManager.slnx
├── AutoManager/                Aplicação web (MVC)
│   ├── Controllers/            Account, Home, Clientes, Veiculos, OrdensServico, Historico
│   ├── Models/                 Usuario, Cliente, Veiculo, OrdemServico, Observacao, enums
│   ├── ViewModels/             Modelos de formulário e de tela
│   ├── Services/               Regras de negócio (Auth, Cliente, Veiculo, OrdemServico, Historico, Dashboard)
│   ├── Persistencia/           Leitura e gravação segura dos arquivos JSON
│   ├── Infrastructure/         Formatação, claims do usuário e utilitários de controller
│   ├── Views/                  Páginas Razor
│   ├── wwwroot/                css/, js/, img/ e bibliotecas locais
│   ├── Data/                   Arquivos JSON (criados em tempo de execução)
│   ├── Program.cs
│   └── appsettings.json
├── AutoManager.Tests/          Testes automatizados
└── docs/                       Rastreabilidade das regras de negócio
```

## Persistência em JSON

Os dados ficam em `AutoManager/Data/` (fora de `wwwroot`, portanto inacessíveis pelo navegador):

| Arquivo | Conteúdo |
|---|---|
| `usuarios.json` | Usuários, perfis, hash e salt das senhas |
| `clientes.json` | Clientes |
| `veiculos.json` | Veículos (com `clienteId`) |
| `ordens.json` | Ordens de serviço e suas observações |
| `sequencias.json` | Últimos identificadores emitidos (garante que números não sejam reutilizados) |

Cuidados implementados:

- **Gravação atômica**: o conteúdo é escrito em um arquivo temporário e só então substitui o original, mantendo uma cópia `.bak`.
- **Concorrência**: um semáforo serializa as operações, de modo que verificações (ex.: placa duplicada) e gravações não se intercalem.
- **Arquivo inválido não é sobrescrito**: se um JSON estiver corrompido, a operação é interrompida com mensagem clara.
- **Integridade**: clientes com veículos/ordens e veículos com ordens não podem ser excluídos (RN16).

Os arquivos de `Data/` não são versionados (`.gitignore`), pois contêm dados de uso e os hashes de senha.

## Documentação

- [`docs/RASTREABILIDADE.md`](docs/RASTREABILIDADE.md) — onde cada regra de negócio está implementada e testada.
