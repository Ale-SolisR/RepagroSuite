namespace RepagroSuite.Domain.Enums;

/// <summary>
/// Ciclo de vida del activo TI. Las transiciones válidas se definen en
/// <see cref="Entities.ItAsset.CanTransition"/>.
/// </summary>
public enum ItAssetStatus
{
    Available = 0,        // Disponible
    Assigned = 1,         // Asignado
    Loaned = 2,           // Prestado
    UnderReview = 3,      // En revisión
    UnderMaintenance = 4, // En mantenimiento
    UnderRepair = 5,      // En reparación
    Returned = 6,         // Devuelto
    Damaged = 7,          // Dañado
    Lost = 8,             // Perdido
    Stolen = 9,           // Robado
    Disposed = 10,        // Dado de baja (terminal)
    Inactive = 11         // Inactivo
}
