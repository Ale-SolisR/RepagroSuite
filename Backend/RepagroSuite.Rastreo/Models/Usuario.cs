namespace Rastreo.Api.Models;

public class Usuario
{
    public int Id { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>ADMIN ve y filtra todos los registros; USUARIO solo los suyos.</summary>
    public string Rol { get; set; } = "USUARIO";

    /// <summary>Sesión válida actual. Sólo un dispositivo puede tener este token a la vez.</summary>
    public Guid? SesionToken { get; set; }
    public DateTime? SesionExpira { get; set; }
}
