namespace Rastreo.Api.Dtos;

public record VacunaDto(
    int Id,
    string Codigo,
    string Nombre,
    string? Laboratorio,
    string? Observaciones,
    int Orden,
    bool Activo
);

public record VacunaUpsertDto(
    string Codigo,
    string Nombre,
    string? Laboratorio,
    string? Observaciones,
    int Orden
);
