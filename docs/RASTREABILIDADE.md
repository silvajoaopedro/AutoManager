# Rastreabilidade: regras de negócio → implementação → testes

As regras abaixo seguem a consolidação do escopo do projeto. Caso a documentação acadêmica oficial use outros identificadores ou outra redação, ela prevalece. Basta ajustar a primeira coluna.

| Regra | Descrição | Implementação | Teste automatizado |
|---|---|---|---|
| RN01 | Apenas usuários autenticados acessam o sistema | `Program.cs` (filtro global `AuthorizeFilter`, cookie de autenticação) | `FluxoHttpTestes.BloqueioNaoAutenticado`, `LoginLogout` |
| RN02 | Cada usuário possui um perfil | `Models/Enums/PerfilUsuario.cs`, `Infrastructure/UsuarioClaims.cs` (perfil como *role*) | `AutenticacaoTestes.LoginValidoEInvalido` |
| RN03 | Só a atendente cadastra e mantém clientes e veículos | `[Authorize(Roles = Perfis.Atendente)]` em `ClientesController` e `VeiculosController` | `FluxoHttpTestes.RestricoesMecanico` |
| RN04 | Um cliente pode ter vários veículos | `Veiculo.ClienteId`, `ClienteService.ObterDetalhesAsync` | `VeiculoTestes.CadastroVinculado` |
| RN05 | Todo veículo vinculado a um cliente cadastrado | `VeiculoService.Validar` | `VeiculoTestes.ClienteInexistente` |
| RN06 | Placa única | `VeiculoService.Validar` (placa normalizada) | `VeiculoTestes.PlacaDuplicada` |
| RN07 | OS vinculada a veículo cadastrado | `OrdemServicoService.AbrirAsync` | `OrdemServicoTestes.AberturaInvalida` |
| RN08 | Identificador único da OS | `DataSession.ProximoNumeroOrdem` + `sequencias.json` | `OrdemServicoTestes.Abertura`, `PersistenciaTestes.PersisteAposReiniciar` |
| RN09 | Status inicial Pendente | `OrdemServicoService.AbrirAsync` | `OrdemServicoTestes.Abertura` |
| RN10 | Atendente e mecânico atualizam o status | `[Authorize(Roles = Perfis.Todos)]` em `Iniciar`, `Observacao`, `Concluir` | `OrdemServicoTestes.AtendenteAtualizaStatus`, `FluxoHttpTestes.FluxoCompleto` |
| RN11 | Sequência Pendente → Em andamento → Concluída | `OrdemServicoService.AtualizarStatusAsync`, `StatusOrdem.Proximo()` | `OrdemServicoTestes.Transicoes` |
| RN12 | Proibido retroceder ou pular etapas | `OrdemServicoService.AtualizarStatusAsync` | `OrdemServicoTestes.Transicoes` |
| RN13 | Conclusão exige observação | `OrdemServicoService.AtualizarStatusAsync` (caso `Concluida`) | `OrdemServicoTestes.ConclusaoExigeObservacao` |
| RN14 | Ordem concluída não pode ser reaberta, alterada ou excluída | `AtualizarStatusAsync` e `RegistrarObservacaoAsync`; não existe exclusão de OS | `OrdemServicoTestes.ConcluidaBloqueada` |
| RN15 | Histórico permanece disponível | `HistoricoService`, `VeiculoService.ObterDetalhesAsync`, `_HistoricoVeiculo.cshtml` | `OrdemServicoTestes.HistoricoVeiculo` |
| RN16 | Exclusões respeitam os vínculos e preservam o histórico | `ClienteService.MotivoBloqueioExclusao`, `VeiculoService.MotivoBloqueioExclusao` | `ClienteTestes.ExclusaoBloqueada`, `VeiculoTestes.Exclusao` |

## Requisitos de persistência

| Requisito | Implementação | Teste |
|---|---|---|
| Persistência após reiniciar | Dados lidos do disco a cada operação (`DataSession`) | `PersistenciaTestes.PersisteAposReiniciar` |
| Evitar perda de dados | Gravação atômica com `.bak` (`JsonFile.Gravar`) e recusa de arquivo inválido | `GravacaoAtomica`, `ArquivoCorrompido` |
| Operações concorrentes | Semáforo único em `JsonDataStore` | `PersistenciaTestes.Concorrencia` |
| Dados fora de `wwwroot` | Pasta `Data/` na raiz do projeto (configurável em `Persistencia:Pasta`) | — |
| Senhas com hash e salt | `SenhaHasher` (PBKDF2-SHA256, 100.000 iterações, salt de 128 bits) | `AutenticacaoTestes.SenhaComHashESalt` |
