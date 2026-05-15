using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

public class Room : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Location { get; set; }
    public string? Floor { get; set; }
    public string? Description { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string? ImageUrl { get; set; }
    public string? Color { get; set; }

    public ICollection<RoomFeature> RoomFeatures { get; set; } = [];
    public ICollection<RoomAvailability> Availabilities { get; set; } = [];
    public ICollection<RoomBlock> Blocks { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
}
