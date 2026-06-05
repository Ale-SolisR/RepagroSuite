namespace Rastreo.Api.Models;

public class Granja
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Propietario { get; set; }
    public string? Ubicacion { get; set; }
    public string? Telefono { get; set; }
    public string? Correo { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public string? UsuarioCreacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? UsuarioModificacion { get; set; }
}
