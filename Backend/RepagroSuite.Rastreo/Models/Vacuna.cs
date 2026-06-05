namespace Rastreo.Api.Models;

public class Vacuna
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Laboratorio { get; set; }
    public string? Observaciones { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
