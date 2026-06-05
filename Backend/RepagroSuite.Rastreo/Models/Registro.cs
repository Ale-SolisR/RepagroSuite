using System.ComponentModel.DataAnnotations.Schema;

namespace Rastreo.Api.Models;

public class Registro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ClienteId { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaInicio { get; set; } = DateTime.UtcNow;
    public DateTime? FechaFinalizacion { get; set; }
    public int? GranjaId { get; set; }
    public Granja? Granja { get; set; }
    public string? Lote { get; set; }
    public string? Observaciones { get; set; }
    public string Estado { get; set; } = "BORRADOR";
    public string? PasoActual { get; set; }
    public Guid? DetalleActualId { get; set; }
    public string? UsuarioCreacion { get; set; }
    public DateTime? FechaUltimaModificacion { get; set; }
    public string? UsuarioUltimaModificacion { get; set; }

    /// <summary>Dueño del registro (aislamiento por usuario).</summary>
    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public bool UsaVacunas { get; set; }
    public List<RegistroVacuna> Vacunas { get; set; } = new();
    public List<RegistroDetalle> Detalles { get; set; } = new();
}
