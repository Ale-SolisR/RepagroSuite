namespace Rastreo.Api.Models;

public class RegistroDetalleEnfermedadValor
{
    public int Id { get; set; }
    public Guid RegistroDetalleId { get; set; }
    public RegistroDetalle? RegistroDetalle { get; set; }
    public int EnfermedadId { get; set; }
    public Enfermedad? Enfermedad { get; set; }

    public byte? ValorNumero { get; set; }
    public bool? ValorBooleano { get; set; }
    public string? ValorTexto { get; set; }
}
