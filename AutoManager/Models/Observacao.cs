using AutoManager.Models.Enums;

namespace AutoManager.Models;

public class Observacao
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Texto { get; set; } = string.Empty;

    public TipoObservacao Tipo { get; set; } = TipoObservacao.Registro;

    public DateTime DataHora { get; set; }

    public int UsuarioId { get; set; }

    public string UsuarioNome { get; set; } = string.Empty;

    public PerfilUsuario UsuarioPerfil { get; set; }
}
