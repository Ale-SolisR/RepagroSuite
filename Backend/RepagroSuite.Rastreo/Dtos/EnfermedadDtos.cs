namespace Rastreo.Api.Dtos;

public record EnfermedadDto(
    int Id,
    string Codigo,
    string Nombre,
    string TipoCampo,
    int Orden,
    bool Activo
);

public record EnfermedadUpsertDto(
    string Codigo,
    string Nombre,
    string TipoCampo,
    int Orden
);
