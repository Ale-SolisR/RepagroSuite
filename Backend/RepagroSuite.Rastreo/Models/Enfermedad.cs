namespace Rastreo.Api.Models;

public class Enfermedad
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string TipoCampo { get; set; } = string.Empty; // SCORE_0_4 | BOOL | AGUDA_CRONICA
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
