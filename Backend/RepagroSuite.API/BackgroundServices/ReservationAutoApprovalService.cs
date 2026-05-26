using RepagroSuite.Application.Features.Reservations.Services;

namespace RepagroSuite.API.BackgroundServices;

/// <summary>
/// Revisa periódicamente las reservas pendientes y aprueba (o rechaza por conflicto) las que
/// entran en la ventana de auto-aprobación (inicio a 30 min o menos, incluidas las ya vencidas).
/// Corre cada minuto. La lógica de negocio vive en <see cref="IReservationService.AutoApproveDueAsync"/>.
/// </summary>
public class ReservationAutoApprovalService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationAutoApprovalService> _logger;

    public ReservationAutoApprovalService(IServiceScopeFactory scopeFactory, ILogger<ReservationAutoApprovalService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Pequeña espera para no competir con el arranque de la app (warm-up de EF).
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
        catch (TaskCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IReservationService>();
                var processed = await service.AutoApproveDueAsync(stoppingToken);
                if (processed > 0)
                    _logger.LogInformation("Auto-aprobación: {Count} reserva(s) procesada(s).", processed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en el ciclo de auto-aprobación de reservas.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (TaskCanceledException) { break; }
        }
    }
}
