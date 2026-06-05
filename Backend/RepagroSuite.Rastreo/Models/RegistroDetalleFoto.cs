namespace Rastreo.Api.Models;

public class RegistroDetalleFoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RegistroDetalleId { get; set; }
    public RegistroDetalle? RegistroDetalle { get; set; }
    public byte Orden { get; set; } // 1, 2 o 3
    public byte[] FotoBinario { get; set; } = Array.Empty<byte>();
    public string FotoMimeType { get; set; } = "image/jpeg";
    public int FotoPesoBytes { get; set; }
    public string? FotoNombre { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaUltimaModificacion { get; set; }
}
