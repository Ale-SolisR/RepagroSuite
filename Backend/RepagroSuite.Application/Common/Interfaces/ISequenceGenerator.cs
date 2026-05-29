namespace RepagroSuite.Application.Common.Interfaces;

/// <summary>
/// Genera consecutivos de boleta con bloqueo transaccional (propuesta §10).
/// DEBE invocarse dentro de la transacción que también persiste la boleta, para que el
/// número no se "queme" si la operación falla. Formato: TI-{CODE}-{año}-{000000}.
/// </summary>
public interface ISequenceGenerator
{
    Task<string> NextTicketNumberAsync(string typeCode, CancellationToken cancellationToken = default);
}
