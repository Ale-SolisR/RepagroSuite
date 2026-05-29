namespace RepagroSuite.Domain.Enums;

/// <summary>Estado físico del activo, independiente del estado administrativo (ciclo de vida).</summary>
public enum PhysicalCondition
{
    New = 0,      // Nuevo
    Good = 1,     // Bueno
    Fair = 2,     // Regular
    Poor = 3,     // Malo
    Unusable = 4  // Inservible
}
