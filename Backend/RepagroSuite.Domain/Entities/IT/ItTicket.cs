using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Boleta TI formal con consecutivo único e inmutable una vez Emitida (propuesta §6).
/// Agrupa activos (detalles), evidencia (fotos) y firmas. El PDF se genera tras el commit.
/// </summary>
public class ItTicket : BaseEntity
{
    public string TicketNumber { get; set; } = string.Empty;  // TI-ENT-2026-000001
    public ItTicketType TicketType { get; set; }
    public ItTicketStatus Status { get; set; } = ItTicketStatus.Borrador;
    public DateTime IssuedAt { get; set; } = BusinessClock.Now;

    public Guid? EmployeeUserId { get; set; }     // Colaborador (contraparte)
    public User? Employee { get; set; }
    public Guid? ItResponsibleUserId { get; set; } // Responsable de TI
    public User? ItResponsible { get; set; }

    public string? Notes { get; set; }

    // PDF inmutable + hash de integridad
    public string? PdfBase64 { get; set; }
    public string? PdfSha256 { get; set; }

    // Anulación (nunca edición)
    public Guid? VoidedBy { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidReason { get; set; }

    public ICollection<ItTicketDetail> Details { get; set; } = [];
    public ICollection<ItTicketPhoto> Photos { get; set; } = [];
    public ICollection<ItTicketSignature> Signatures { get; set; } = [];

    /// <summary>Prefijo de 3 letras para el consecutivo por tipo.</summary>
    public static string TypeCode(ItTicketType type) => type switch
    {
        ItTicketType.Entrega => "ENT",
        ItTicketType.Devolucion => "DEV",
        ItTicketType.Prestamo => "PRE",
        ItTicketType.Mantenimiento => "MAN",
        ItTicketType.Reparacion => "REP",
        ItTicketType.Traslado => "TRA",
        ItTicketType.CambioResponsable => "CRE",
        ItTicketType.AsignacionAccesorios => "ACC",
        ItTicketType.Baja => "BAJ",
        _ => "TIX"
    };
}
