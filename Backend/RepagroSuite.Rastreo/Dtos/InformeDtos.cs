namespace Rastreo.Api.Dtos;

/// <summary>Request para generar un informe.</summary>
public record GenerarInformeRequest(
    int GranjaId,
    DateTime Desde,
    DateTime Hasta,
    string? PeriodoEtiqueta,
    string? ResponsableTecnico,
    string? Cliente,
    // Overrides opcionales: si vienen, reemplazan las conclusiones/recomendaciones autogeneradas
    // (permite que el MV edite o agregue notas del doctor antes de generar el PDF).
    List<string>? Conclusiones = null,
    List<string>? Recomendaciones = null
);

/// <summary>Request para enviar un informe por correo.</summary>
public record EnviarInformeRequest(
    List<string> Destinatarios,
    string? Asunto,
    string? Mensaje
);

/// <summary>Item de listado de historial.</summary>
public record InformeListDto(
    Guid Id,
    string Consecutivo,
    int GranjaId,
    string? GranjaCodigo,
    string? GranjaNombre,
    string? PeriodoEtiqueta,
    DateTime PeriodoDesde,
    DateTime PeriodoHasta,
    string Estado,
    string NivelRiesgo,
    int TotalEvaluados,
    double PrevalenciaPct,
    double ConsolidacionPct,
    double Idn,
    DateTime FechaGeneracion,
    string? UsuarioGeneracion,
    DateTime? FechaUltimoEnvio,
    int CantidadEnvios
);

/// <summary>Detalle de envio para auditoria.</summary>
public record InformeEnvioDto(
    Guid Id,
    DateTime Fecha,
    string? UsuarioEnvio,
    List<string> Destinatarios,
    string? Asunto,
    string Estado,
    string? ErrorMensaje
);
