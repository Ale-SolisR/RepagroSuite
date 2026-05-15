using RepagroSuite.Domain.Common;

namespace RepagroSuite.Domain.Entities;

public class Feature : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? IconName { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<RoomFeature> RoomFeatures { get; set; } = [];
}
