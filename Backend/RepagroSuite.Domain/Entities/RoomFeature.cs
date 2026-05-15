namespace RepagroSuite.Domain.Entities;

public class RoomFeature
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public Guid FeatureId { get; set; }
    public Feature Feature { get; set; } = null!;
}
