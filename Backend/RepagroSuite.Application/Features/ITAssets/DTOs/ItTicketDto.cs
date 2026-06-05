using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.ITAssets.DTOs;

// ─── Lectura ────────────────────────────────────────────────────────────────────

public class ItTicketListDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public ItTicketType TicketType { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public ItTicketStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public string? EmployeeName { get; set; }
    public string? ItResponsibleName { get; set; }
    public int AssetCount { get; set; }
}

public class ItTicketLineDto
{
    public Guid? AssetId { get; set; }
    public string LineType { get; set; } = "ASSET";
    public string? InternalCode { get; set; }
    public string? TypeName { get; set; }
    public string? Description { get; set; }
    public string? SerialNumber { get; set; }
    public string? Condition { get; set; }
}

public class ItTicketSignatureDto
{
    public string SignerType { get; set; } = string.Empty;
    public string? SignerName { get; set; }
    public string ImageBase64 { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
}

public class ItTicketPhotoDto
{
    public Guid Id { get; set; }
    public string ImageBase64 { get; set; } = string.Empty;
}

/// <summary>
/// Eslabón de la cadena de trazabilidad de una boleta: la entrega que la origina
/// (Relation = "Origen") o la boleta que la cierra (Relation = "Cierre"). Enlaza por activo.
/// </summary>
public class ItTicketChainLinkDto
{
    public Guid TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public ItTicketType TicketType { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public ItTicketStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public string Relation { get; set; } = string.Empty;   // "Origen" | "Cierre"
    public string? AssetCode { get; set; }
}

public class ItTicketDto : ItTicketListDto
{
    public string? EmployeeIdentification { get; set; }      // cédula del colaborador
    public string? ItResponsibleIdentification { get; set; } // cédula del responsable de TI
    public string? Notes { get; set; }
    public string? PdfSha256 { get; set; }
    public bool HasPdf { get; set; }
    public string? VoidReason { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidedByName { get; set; }   // usuario que anuló la boleta (auditoría)
    public List<ItTicketLineDto> Lines { get; set; } = [];
    public List<ItTicketPhotoDto> Photos { get; set; } = [];
    public List<ItTicketSignatureDto> Signatures { get; set; } = [];
    public List<ItTicketChainLinkDto> Chain { get; set; } = [];
}

// ─── Escritura ──────────────────────────────────────────────────────────────────

public class SignatureInputDto
{
    public string SignerType { get; set; } = "Colaborador";   // Colaborador | ResponsableTI
    public string? SignerName { get; set; }
    public string ImageBase64 { get; set; } = string.Empty;
}

public class CreateAssignmentDto
{
    public Guid EmployeeId { get; set; }
    public List<Guid> AssetIds { get; set; } = [];
    public PhysicalCondition ConditionOut { get; set; } = PhysicalCondition.Good;
    public string? Accessories { get; set; }
    public string? Notes { get; set; }
    public List<string> Photos { get; set; } = [];          // data URLs
    public List<SignatureInputDto> Signatures { get; set; } = [];
}

public class CreateReturnDto
{
    public Guid AssetId { get; set; }
    public PhysicalCondition ConditionIn { get; set; } = PhysicalCondition.Good;
    public ItAssetStatus ResultingStatus { get; set; } = ItAssetStatus.Available;
    public string? ReturnNotes { get; set; }
    public List<string> Photos { get; set; } = [];
    public List<SignatureInputDto> Signatures { get; set; } = [];
}

/// <summary>Boleta genérica: mantenimiento, reparación, traslado, cambio de responsable o baja.</summary>
public class CreateGenericTicketDto
{
    public ItTicketType TicketType { get; set; }
    public List<Guid> AssetIds { get; set; } = [];
    public Guid? EmployeeId { get; set; }
    public string? Notes { get; set; }
    /// <summary>Si se indica, cambia el estado de los activos (validado por la máquina de estados).</summary>
    public ItAssetStatus? NewAssetStatus { get; set; }
    public string? StatusReason { get; set; }
    public List<string> Photos { get; set; } = [];
    public List<SignatureInputDto> Signatures { get; set; } = [];
}

public class VoidTicketDto
{
    public string Reason { get; set; } = string.Empty;
}

// ─── Movimientos canónicos (propuesta aprobada §4) ───────────────────────────────

/// <summary>Desasignación: cierre administrativo del vínculo. El activo vuelve a Disponible.</summary>
public class CreateDeassignmentDto
{
    public Guid AssetId { get; set; }
    public string? Notes { get; set; }
    public List<string> Photos { get; set; } = [];
    public List<SignatureInputDto> Signatures { get; set; } = [];
}

/// <summary>
/// Incidente: Deterioro (→Damaged) o Pérdida/Robo (→Lost/Stolen). El activo queda inactivo
/// (no asignable). Si estaba asignado, se cierra la asignación. Motivo obligatorio.
/// Firma del responsable TI obligatoria; la del colaborador es opcional (puede no estar presente).
/// </summary>
public class CreateIncidentDto
{
    public Guid AssetId { get; set; }
    /// <summary>Damaged (deterioro) | Lost | Stolen (pérdida/robo).</summary>
    public ItAssetStatus TargetStatus { get; set; }
    public string Reason { get; set; } = string.Empty;
    /// <summary>Estado físico resultante (opcional, típico en deterioro).</summary>
    public PhysicalCondition? Condition { get; set; }
    public List<string> Photos { get; set; } = [];
    public List<SignatureInputDto> Signatures { get; set; } = [];
}

/// <summary>Reactivación de un activo inactivo (Dañado/Perdido/Robado/Inactivo) → Disponible. Solo admin.</summary>
public class ReactivateAssetDto
{
    public string Reason { get; set; } = string.Empty;
}
