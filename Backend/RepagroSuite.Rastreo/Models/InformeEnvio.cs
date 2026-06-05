namespace Rastreo.Api.Models;

/// <summary>
/// Auditoria de cada envio por correo de un informe.
/// </summary>
public class InformeEnvio
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid InformeId { get; set; }
    public InformeEvaluacion? Informe { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string? UsuarioEnvio { get; set; }

    /// <summary>Destinatarios separados por ; (legible) + tambien serializados a JSON</summary>
    public string DestinatariosJson { get; set; } = "[]";

    public string? Asunto { get; set; }

    /// <summary>OK, ERROR</summary>
    public string Estado { get; set; } = "OK";
    public string? ErrorMensaje { get; set; }
}
