using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.ITAssets.DTOs;

public class ItEmployeeDto
{
    public Guid Id { get; set; }
    public string IdentificationNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}

public class CreateItEmployeeDto
{
    /// <summary>Cédula. Se normaliza y se valida que no exista otro colaborador con la misma.</summary>
    public string IdentificationNumber { get; set; } = string.Empty;
    /// <summary>Nombre (autocompletado en el front vía /identifications/lookup; el usuario puede ajustarlo).</summary>
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateItEmployeeDto
{
    public string IdentificationNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Department { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

// ─── Historial / auditoría centrada en el colaborador (propuesta §12) ─────────────

public class ItEmployeeAssignmentDto
{
    public Guid Id { get; set; }
    public Guid AssetId { get; set; }
    public string AssetCode { get; set; } = string.Empty;
    public string? AssetTypeName { get; set; }
    public string? AssetModel { get; set; }
    public ItAssetStatus AssetStatus { get; set; }
    public string AssetStatusName { get; set; } = string.Empty;
    public bool IsActive { get; set; }                 // asignación vigente
    public DateTime AssignedAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public string? ConditionOutName { get; set; }
    public string? ConditionInName { get; set; }
    public string? ClosedReason { get; set; }          // Devolucion | Desasignacion | Deterioro | PerdidaRobo
    public string? AssignedTicketNumber { get; set; }
    public Guid? AssignedTicketId { get; set; }
    public string? ClosingTicketNumber { get; set; }
    public Guid? ClosingTicketId { get; set; }
    public string? Notes { get; set; }
    /// <summary>Miniaturas del activo como data URLs (para mostrarlas en la tarjeta sin otra petición).</summary>
    public List<string> AssetPhotos { get; set; } = [];
}

public class ItEmployeeTicketDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public ItTicketType TicketType { get; set; }
    public string TicketTypeName { get; set; } = string.Empty;
    public ItTicketStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime IssuedAt { get; set; }
    public int AssetCount { get; set; }
    public bool HasPdf { get; set; }
}

public class ItEmployeeHistoryDto
{
    public ItEmployeeDto Employee { get; set; } = new();
    public int CurrentAssetsCount { get; set; }
    public int PastAssetsCount { get; set; }
    public List<ItEmployeeAssignmentDto> CurrentAssignments { get; set; } = [];
    public List<ItEmployeeAssignmentDto> PastAssignments { get; set; } = [];
    public List<ItEmployeeTicketDto> Tickets { get; set; } = [];
}
