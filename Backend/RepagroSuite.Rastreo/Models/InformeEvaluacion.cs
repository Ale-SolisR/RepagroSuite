namespace Rastreo.Api.Models;

/// <summary>
/// Informe ejecutivo de evaluacion pulmonar consolidado de un periodo + granja.
/// Persiste snapshot (JSON) del calculo + PDF binario para auditoria y reenvio.
/// </summary>
public class InformeEvaluacion
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Consecutivo legible: IEP-2026-00001</summary>
    public string Consecutivo { get; set; } = string.Empty;

    public int GranjaId { get; set; }
    public Granja? Granja { get; set; }

    /// <summary>Periodo evaluado (inclusivo)</summary>
    public DateTime PeriodoDesde { get; set; }
    public DateTime PeriodoHasta { get; set; }

    /// <summary>Etiqueta legible: "Marzo 2026" o "17 mar 2026"</summary>
    public string? PeriodoEtiqueta { get; set; }

    public string? ResponsableTecnico { get; set; }
    public string? Cliente { get; set; }

    /// <summary>BORRADOR, GENERADO, ENVIADO, ERROR, ANULADO</summary>
    public string Estado { get; set; } = "GENERADO";

    /// <summary>Snapshot JSON serializado del resultado completo (KPIs + datos)</summary>
    public string SnapshotJson { get; set; } = "{}";

    /// <summary>PDF generado (cacheado para descarga / reenvio sin recomputar)</summary>
    public byte[]? PdfBinario { get; set; }
    public int? PdfPesoBytes { get; set; }

    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;
    public string? UsuarioGeneracion { get; set; }

    public DateTime? FechaUltimoEnvio { get; set; }
    public int CantidadEnvios { get; set; }

    /// <summary>Metricas top-level cacheadas para listar sin parsear JSON</summary>
    public int TotalEvaluados { get; set; }
    public double PrevalenciaPct { get; set; }
    public double ConsolidacionPct { get; set; }
    public double Idn { get; set; }
    /// <summary>BAJO, MEDIO, ALTO, CRITICO</summary>
    public string NivelRiesgo { get; set; } = "BAJO";

    public List<InformeEnvio> Envios { get; set; } = new();
}
