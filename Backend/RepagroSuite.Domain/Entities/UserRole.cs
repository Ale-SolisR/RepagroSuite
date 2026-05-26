using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public DateTime AssignedAt { get; set; } = BusinessClock.Now;
    public Guid? AssignedBy { get; set; }
}
