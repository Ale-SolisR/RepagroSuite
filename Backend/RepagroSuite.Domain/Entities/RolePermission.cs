using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

public class RolePermission
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
    public DateTime AssignedAt { get; set; } = BusinessClock.Now;
    public Guid? AssignedBy { get; set; }
}
