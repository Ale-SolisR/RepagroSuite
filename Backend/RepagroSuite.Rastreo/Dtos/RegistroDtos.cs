namespace Rastreo.Api.Dtos;

public record RegistroListDto(
    Guid Id,
    DateTime FechaCreacion,
    int? GranjaId,
    string? GranjaCodigo,
    string? GranjaNombre,
    string? Lote,
    string Estado,
    int CantidadDetalles,
    DateTime? FechaUltimaModificacion,
    int? UsuarioId = null,
    string? UsuarioNombre = null
);

public record FotoMetaDto(
    Guid Id,
    byte Orden,
    string MimeType,
    int PesoBytes,
    string? Nombre,
    string Url,
    DateTime FechaCreacion
);

public record RegistroVacunaDto(
    int Id,
    string Codigo,
    string Nombre,
    string? Laboratorio,
    int Orden
);

public record RegistroDetalleEnfermedadValorDto(
    int EnfermedadId,
    string Codigo,
    string Nombre,
    string TipoCampo,
    int Orden,
    byte? ValorNumero,
    bool? ValorBooleano,
    string? ValorTexto
);

public record DetalleEnfermedadValorUpsertDto(
    int EnfermedadId,
    byte? ValorNumero,
    bool? ValorBooleano,
    string? ValorTexto
);

public record RegistroDetalleDto(
    Guid Id,
    Guid? ClienteId,
    int NumeroLinea,
    byte ApicalIzquierdo,
    byte CardiacoIzquierdo,
    byte DiafragmaticoIzquierdo,
    byte ApicalDerecho,
    byte CardiacoDerecho,
    byte DiafragmaticoDerecho,
    byte Accesorio,
    byte SPES,
    bool AbscesoNodulo,
    bool PericardioEngrosado,
    bool Pericarditis,
    string? AgudaCronica,
    bool Moco,
    List<FotoMetaDto> Fotos,
    string Estado,
    string? PasoActual,
    DateTime FechaCreacion,
    DateTime? FechaUltimaModificacion,
    List<RegistroDetalleEnfermedadValorDto> EnfermedadesValores
);

public record RegistroDetailDto(
    Guid Id,
    DateTime FechaCreacion,
    DateTime FechaInicio,
    DateTime? FechaFinalizacion,
    int? GranjaId,
    string? GranjaCodigo,
    string? GranjaNombre,
    string? Lote,
    string? Observaciones,
    bool UsaVacunas,
    List<RegistroVacunaDto> Vacunas,
    string Estado,
    string? PasoActual,
    Guid? DetalleActualId,
    DateTime? FechaUltimaModificacion,
    List<RegistroDetalleDto> Detalles,
    int? UsuarioId = null,
    string? UsuarioNombre = null
);

/// <summary>Upsert idempotente de un registro (el cliente envía el Id).</summary>
public record RegistroUpsertDto(
    int? GranjaId,
    string? Lote,
    string? Observaciones,
    bool UsaVacunas,
    List<int>? VacunaIds,
    string? PasoActual,
    Guid? DetalleActualId,
    DateTime? FechaCreacion // opcional: si el cliente lo creó offline en t1
);

/// <summary>Upsert idempotente de un detalle (el cliente envía el Id).</summary>
public record DetalleUpsertDto(
    int? NumeroLinea,
    byte ApicalIzquierdo,
    byte CardiacoIzquierdo,
    byte DiafragmaticoIzquierdo,
    byte ApicalDerecho,
    byte CardiacoDerecho,
    byte DiafragmaticoDerecho,
    byte Accesorio,
    byte SPES,
    bool AbscesoNodulo,
    bool PericardioEngrosado,
    bool Pericarditis,
    string? AgudaCronica,
    bool Moco,
    string? PasoActual,
    string? Estado,
    DateTime? FechaCreacion,
    List<DetalleEnfermedadValorUpsertDto>? EnfermedadesValores
);

public record EnviarCorreoDto(
    List<string> Destinatarios,
    bool IncluirExcel,
    bool IncluirPdf,
    string? Asunto,
    string? Mensaje
);
