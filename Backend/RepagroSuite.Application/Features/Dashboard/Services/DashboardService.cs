using RepagroSuite.Application.Features.Dashboard.DTOs;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.Dashboard.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _uow;

    public DashboardService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1);

        var totalRooms = await _uow.Rooms.CountAsync(cancellationToken: cancellationToken);
        var activeRooms = await _uow.Rooms.CountAsync(r => r.Status == RoomStatus.Available, cancellationToken);
        var pendingReservations = await _uow.Reservations.CountAsync(r => r.Status == ReservationStatus.Pending, cancellationToken);
        var todayReservations = await _uow.Reservations.CountAsync(r => r.StartDateTime.Date == today, cancellationToken);
        var thisMonthReservations = await _uow.Reservations.CountAsync(r => r.StartDateTime >= firstOfMonth, cancellationToken);
        var pendingUsers = await _uow.Users.CountAsync(u => u.Status == UserStatus.Pending, cancellationToken);

        // Trend: last 7 days
        var trend = new List<ReservationTrendDto>();
        for (int i = 6; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var count = await _uow.Reservations.CountAsync(r => r.StartDateTime.Date == day, cancellationToken);
            trend.Add(new ReservationTrendDto { Date = day.ToString("yyyy-MM-dd"), Count = count });
        }

        // Room usage: last 30 days
        var thirtyDaysAgo = today.AddDays(-30);
        var rooms = await _uow.Rooms.GetAllAsync(cancellationToken);
        var roomUsage = new List<RoomUsageDto>();
        foreach (var room in rooms.OrderBy(r => r.Name))
        {
            var total = await _uow.Reservations.CountAsync(
                r => r.RoomId == room.Id && r.StartDateTime.Date >= thirtyDaysAgo, cancellationToken);
            var approved = await _uow.Reservations.CountAsync(
                r => r.RoomId == room.Id && r.StartDateTime.Date >= thirtyDaysAgo && r.Status == ReservationStatus.Approved, cancellationToken);
            roomUsage.Add(new RoomUsageDto
            {
                RoomId = room.Id.ToString(),
                RoomName = room.Name,
                TotalReservations = total,
                ApprovedReservations = approved
            });
        }

        return new DashboardStatsDto
        {
            TotalRooms = totalRooms,
            ActiveRooms = activeRooms,
            PendingReservations = pendingReservations,
            TodayReservations = todayReservations,
            ThisMonthReservations = thisMonthReservations,
            PendingUsers = pendingUsers,
            Trend = trend,
            RoomUsage = roomUsage.Where(r => r.TotalReservations > 0).OrderByDescending(r => r.TotalReservations)
        };
    }
}
