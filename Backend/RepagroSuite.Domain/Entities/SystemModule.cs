using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

public class SystemModule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public string? RoutePrefix { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsCore { get; set; } = false;
    public string? Version { get; set; }
}
