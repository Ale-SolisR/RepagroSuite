namespace RepagroSuite.Application.Features.Dashboard.DTOs;

public class DashboardStatsDto
{
    public int TotalRooms { get; set; }
    public int ActiveRooms { get; set; }
    public int PendingReservations { get; set; }
    public int TodayReservations { get; set; }
    public int ThisMonthReservations { get; set; }
    public int PendingUsers { get; set; }
    public IEnumerable<ReservationTrendDto> Trend { get; set; } = [];
    public IEnumerable<RoomUsageDto> RoomUsage { get; set; } = [];
}

public class ReservationTrendDto
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RoomUsageDto
{
    public string RoomId { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public int TotalReservations { get; set; }
    public int ApprovedReservations { get; set; }
}
