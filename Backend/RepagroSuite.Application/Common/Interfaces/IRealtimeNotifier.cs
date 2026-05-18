namespace RepagroSuite.Application.Common.Interfaces;

// Abstracción para emitir eventos en tiempo real a los clientes conectados.
// Implementación concreta vive en la capa API (SignalR).
public interface IRealtimeNotifier
{
    Task ReservationChangedAsync(Guid reservationId, Guid roomId, string changeType, CancellationToken ct = default);
    Task RoomChangedAsync(Guid roomId, string changeType, CancellationToken ct = default);
}
