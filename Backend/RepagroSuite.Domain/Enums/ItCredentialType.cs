namespace RepagroSuite.Domain.Enums;

/// <summary>Tipo de credencial guardada por activo (solo para ícono/clasificación en la UI).</summary>
public enum ItCredentialType
{
    AnyDesk = 0,
    Windows = 1,
    Microsoft365 = 2,
    Email = 3,
    Bios = 4,
    Network = 5,       // Router / red
    Application = 6,
    Other = 99,
}
