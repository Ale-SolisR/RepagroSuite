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

public class ItTicketDto : ItTicketListDto
{
    public string? Notes { get; set; }
    public string? PdfSha256 { get; set; }
    public bool HasPdf { get; set; }
    public string? VoidReason { get; set; }
    public DateTime? VoidedAt { get; set; }
    public List<ItTicketLineDto> Lines { get; set; } = [];
    public List<ItTicketPhotoDto> Photos { get; set; } = [];
    public List<ItTicketSignatureDto> Signatures { get; set; } = [];
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
