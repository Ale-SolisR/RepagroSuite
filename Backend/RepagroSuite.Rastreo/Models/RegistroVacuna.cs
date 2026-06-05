namespace Rastreo.Api.Models;

public class RegistroVacuna
{
    public Guid RegistroId { get; set; }
    public Registro? Registro { get; set; }
    public int VacunaId { get; set; }
    public Vacuna? Vacuna { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
