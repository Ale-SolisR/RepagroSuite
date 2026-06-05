namespace Rastreo.Api.Models;

public class RegistroDetalle
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? ClienteId { get; set; }
    public Guid RegistroId { get; set; }
    public Registro? Registro { get; set; }
    public int NumeroLinea { get; set; }

    public byte ApicalIzquierdo { get; set; }
    public byte CardiacoIzquierdo { get; set; }
    public byte DiafragmaticoIzquierdo { get; set; }
    public byte ApicalDerecho { get; set; }
    public byte CardiacoDerecho { get; set; }
    public byte DiafragmaticoDerecho { get; set; }
    public byte Accesorio { get; set; }

    public byte SPES { get; set; }
    public bool AbscesoNodulo { get; set; }
    public bool PericardioEngrosado { get; set; }
    public bool Pericarditis { get; set; }
    public string? AgudaCronica { get; set; } // A | C | AC | null
    public bool Moco { get; set; }

    public string Estado { get; set; } = "BORRADOR";
    public string? PasoActual { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public DateTime? FechaUltimaModificacion { get; set; }

    public List<RegistroDetalleFoto> Fotos { get; set; } = new();
    public List<RegistroDetalleEnfermedadValor> EnfermedadesValores { get; set; } = new();
}
