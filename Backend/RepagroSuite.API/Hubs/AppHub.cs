using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RepagroSuite.Application.Common.Interfaces;

namespace RepagroSuite.API.Hubs;

// Hub único para notificaciones en tiempo real.
// Autenticado: el JWT viaja por la querystring (?access_token=...) — ver Program.cs JwtBearerEvents.
//
// Eventos que emite el servidor:
//   - reservation.changed   { reservationId, roomId, changeType }
//   - room.changed          { roomId, changeType }
//
// Los clientes invalidan sus queries de TanStack al recibir cualquiera de los dos.
[Authorize]
public class AppHub : Hub
{
    // Vacío: por ahora el cliente sólo escucha; no necesita métodos invocables.
}

// Implementación de IRealtimeNotifier que usa el HubContext para emitir.
public class SignalRNotifier : IRealtimeNotifier
{
    private readonly IHubContext<AppHub> _hub;

    public SignalRNotifier(IHubContext<AppHub> hub) => _hub = hub;

    public Task ReservationChangedAsync(Guid reservationId, Guid roomId, string changeType, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync("reservation.changed",
            new { reservationId, roomId, changeType }, ct);

    public Task RoomChangedAsync(Guid roomId, string changeType, CancellationToken ct = default)
        => _hub.Clients.All.SendAsync("room.changed",
            new { roomId, changeType }, ct);
}
