namespace RepagroSuite.Domain.Common;

/// <summary>
/// Reloj de negocio del sistema: SIEMPRE en hora de Costa Rica (UTC-6, sin horario de verano),
/// independiente de la zona horaria del servidor donde se ejecute (IIS de Site4Now, etc.).
///
/// Toda fecha/hora de negocio o de auditoría (CreatedAt, UpdatedAt, ApprovedAt, timestamps de
/// auditoría, "hoy" del dashboard, validaciones de "fecha futura", etc.) debe usar este reloj.
///
/// Los valores resultantes tienen Kind=Unspecified, por lo que se serializan SIN sufijo 'Z' y el
/// frontend los interpreta como hora-de-pared de Costa Rica tal cual, sin reconvertir por la zona
/// del navegador. Las reservas ya se almacenan así, de modo que todo el sistema queda consistente.
///
/// Excepción: las marcas de tiempo de seguridad ligadas al estándar JWT (expiración del access token)
/// permanecen en UTC porque la librería de validación de tokens compara contra UTC.
/// </summary>
public static class BusinessClock
{
    private static readonly TimeZoneInfo Tz = Resolve();

    private static TimeZoneInfo Resolve()
    {
        // IANA en Linux, Windows id como respaldo, y zona fija UTC-6 si ninguna existe.
        foreach (var id in new[] { "America/Costa_Rica", "Central America Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { /* probar siguiente id */ }
        }
        return TimeZoneInfo.CreateCustomTimeZone("CR", TimeSpan.FromHours(-6), "Costa Rica", "Costa Rica");
    }

    /// <summary>Fecha y hora actuales en Costa Rica (Kind=Unspecified).</summary>
    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Tz);

    /// <summary>Fecha actual en Costa Rica (medianoche, Kind=Unspecified).</summary>
    public static DateTime Today => Now.Date;

    /// <summary>Zona horaria de negocio (Costa Rica).</summary>
    public static TimeZoneInfo TimeZone => Tz;
}
