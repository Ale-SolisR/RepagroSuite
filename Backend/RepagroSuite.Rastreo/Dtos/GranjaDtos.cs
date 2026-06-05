namespace Rastreo.Api.Dtos;

public record GranjaDto(
    int Id,
    string Codigo,
    string Nombre,
    string? Propietario,
    string? Ubicacion,
    string? Telefono,
    string? Correo,
    string? Observaciones,
    bool Activo,
    DateTime FechaCreacion
);

public record GranjaUpsertDto(
    string Codigo,
    string Nombre,
    string? Propietario,
    string? Ubicacion,
    string? Telefono,
    string? Correo,
    string? Observaciones
);
