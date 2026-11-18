# Rastreabilidade do AutoManager

| Regra oficial | Comportamento | Implementação |
|---|---|---|
| RN-01 | Acesso autenticado | Filtro global em `Program.cs` e `AccountController` |
| RN-02 | Perfis Atendente e Mecânico | `PerfilUsuario`, `UsuarioClaims` |
| RN-03 | Manutenção de clientes pelo Atendente | `ClientesController`, ações protegidas por perfil |
| RN-04 | Manutenção de veículos pelo Atendente | `VeiculosController`, ações protegidas por perfil |
| RN-05 | Abertura de OS pelo Atendente | `OrdensServicoController.Create` |
| RN-06 | Consultas compartilhadas | Controllers de clientes, veículos, ordens, histórico e painel |
| RN-07 | Atualização de status e observações pelos dois perfis | `OrdensServicoController` e `OrdemServicoService` |
| RN-08 | Nome e telefone obrigatórios | `ClienteService` |
| RN-09 | Placa, marca e modelo obrigatórios | `VeiculoService` |
| RN-10 | Proprietário válido e preservado na edição | `VeiculoService` |
| RN-11 | Placa única normalizada | `VeiculoService`, `Formatacao.NormalizarPlaca` |
| RN-12 | Exclusão de cliente somente sem veículos | `ClienteService` |
| RN-13 | Exclusão de veículo somente sem ordens | `VeiculoService` |
| RN-14 | Confirmação antes de excluir | Views `Delete.cshtml` e ações POST correspondentes |
| RN-15 | Vínculo da OS com veículo cadastrado | `OrdemServicoService.AbrirAsync` |
| RN-16 | Número único e persistido | `DataSession.ProximoNumeroOrdem`, `JsonFile` |
| RN-17 | Descrição obrigatória | `OrdemServicoService.AbrirAsync` |
| RN-18 | Data automática de abertura | `OrdemServicoService.AbrirAsync` |
| RN-19 | Status inicial Pendente | `OrdemServicoService.AbrirAsync` |
| RN-20 | Status permitidos | Enum `StatusOrdem` |
| RN-21 | Transições sem salto ou retrocesso | `OrdemServicoService.AtualizarStatusAsync` |
| RN-22 | Observação independente do status | `OrdemServicoService.RegistrarObservacaoAsync` |
| RN-23 | Observação obrigatória na conclusão | `OrdemServicoService.ConcluirAsync` |
| RN-24 | OS concluída sem reabertura ou alteração de status | `OrdemServicoService.AtualizarStatusAsync` |
| RN-25 | Observações bloqueadas após conclusão | `OrdemServicoService.RegistrarObservacaoAsync` |
| RN-26 | Preservação do histórico | `HistoricoService`, `VeiculoService` |
| RN-27 | Contagem de ordens por status | `DashboardService` |

Os testes ficam em `AutoManager.Tests/Testes`, organizados em autenticação, clientes, veículos, ordens, persistência e fluxo HTTP. A cobertura geral da suíte inclui permissões, transições, dados persistidos após reinício e falha de gravação na abertura.
