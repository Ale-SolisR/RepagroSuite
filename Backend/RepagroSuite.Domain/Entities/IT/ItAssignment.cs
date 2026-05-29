using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

/// <summary>
/// Asignación de un activo a un colaborador. Sólo puede existir UNA activa por activo
/// (índice único filtrado, propuesta §18.1). Se cierra con la devolución.
/// </summary>
public class ItAssignment : BaseEntity
{
    public Guid AssetId { get; set; }
    public ItAsset? Asset { get; set; }

    public Guid EmployeeId { get; set; }
    public ItEmployee? Employee { get; set; }

    public Guid AssignedTicketId { get; set; }
    public ItTicket? AssignedTicket { get; set; }
    public Guid? ReturnTicketId { get; set; }
    public ItTicket? ReturnTicket { get; set; }

    public DateTime AssignedAt { get; set; } = BusinessClock.Now;
    public DateTime? ReturnedAt { get; set; }

    public PhysicalCondition ConditionOut { get; set; }
    public PhysicalCondition? ConditionIn { get; set; }

    public AssignmentStatus Status { get; set; } = AssignmentStatus.Activa;
    public string? Accessories { get; set; }
    public string? ReturnNotes { get; set; }
}
