namespace RepagroSuite.Application.Features.ITAssets.DTOs;

/// <summary>KPIs e indicadores del dashboard de TI (propuesta §13). Enriquecido con métricas
/// de nivel empresarial: tasas operativas, salud de garantías, antigüedad de flota,
/// distribuciones por marca/ubicación/condición, tendencia de adquisiciones y top responsables.</summary>
public class ItDashboardDto
{
    // ─── Totales por estado ──────────────────────────────────────────────────────
    public int TotalAssets { get; set; }
    public int Assigned { get; set; }
    public int Available { get; set; }
    public int Loaned { get; set; }
    public int UnderRepair { get; set; }
    public int UnderMaintenance { get; set; }
    public int Damaged { get; set; }
    public int Lost { get; set; }
    public int Stolen { get; set; }
    public int Disposed { get; set; }

    // ─── Valor / inversión ───────────────────────────────────────────────────────
    public decimal TotalCostCrc { get; set; }
    public decimal TotalCostUsd { get; set; }
    public int AssetsWithCost { get; set; }

    // ─── Tasas (0–100) ───────────────────────────────────────────────────────────
    /// <summary>% de activos operables que están asignados (Asignados / (Asignados+Disponibles+Prestados)).</summary>
    public double AssignmentRatePct { get; set; }
    /// <summary>% de activos listos para entregar (Disponibles / activos no dados de baja).</summary>
    public double AvailabilityRatePct { get; set; }
    /// <summary>Índice de calidad de datos: % de campos clave (serie, placa, responsable) completos.</summary>
    public double DataQualityPct { get; set; }
    /// <summary>% de la flota con incidencias (taller, dañado, perdido, robado).</summary>
    public double IncidentRatePct { get; set; }

    // ─── Alertas / calidad de datos ──────────────────────────────────────────────
    public int WithoutSerial { get; set; }
    public int WithoutTag { get; set; }
    public int WithoutHolder { get; set; }
    public int WarrantyExpiringSoon { get; set; }   // ≤ 60 días

    // ─── Salud de garantías ──────────────────────────────────────────────────────
    public int WarrantyActive { get; set; }
    public int WarrantyExpired { get; set; }
    public int WithoutWarranty { get; set; }

    // ─── Antigüedad de la flota ──────────────────────────────────────────────────
    /// <summary>Antigüedad promedio en meses (solo activos con fecha de compra).</summary>
    public double AvgAgeMonths { get; set; }
    public int AssetsWithPurchaseDate { get; set; }

    // ─── Distribuciones ──────────────────────────────────────────────────────────
    public IEnumerable<ItCountByLabelDto> ByType { get; set; } = [];
    public IEnumerable<ItCountByLabelDto> ByStatus { get; set; } = [];
    public IEnumerable<ItCountByLabelDto> ByDepartment { get; set; } = [];
    public IEnumerable<ItCountByLabelDto> ByBrand { get; set; } = [];
    public IEnumerable<ItCountByLabelDto> ByLocation { get; set; } = [];
    public IEnumerable<ItCountByLabelDto> ByCondition { get; set; } = [];
    public IEnumerable<ItCountByLabelDto> ByAgeBucket { get; set; } = [];
    public IEnumerable<ItCountByLabelDto> TopHolders { get; set; } = [];

    /// <summary>Valor de inventario asignado por colaborador (top, desc). Permite saber quién custodia más dinero.</summary>
    public IEnumerable<ItValueByLabelDto> ValueByHolder { get; set; } = [];

    /// <summary>Adquisiciones por mes (últimos 12 meses, cronológico).</summary>
    public IEnumerable<ItTrendPointDto> AcquisitionTrend { get; set; } = [];

    /// <summary>Marca de tiempo de generación (hora Costa Rica) para reportes/exportación.</summary>
    public string GeneratedAt { get; set; } = string.Empty;
}

public class ItCountByLabelDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ItTrendPointDto
{
    public string Label { get; set; } = string.Empty;   // "ene 26"
    public int Count { get; set; }
}

public class ItValueByLabelDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Crc { get; set; }
    public decimal Usd { get; set; }
    public int AssetCount { get; set; }
    /// <summary>Desglose por tipo de equipo (cantidad + valor) para el tooltip.</summary>
    public IEnumerable<ItValueBreakdownDto> Breakdown { get; set; } = [];
}

public class ItValueBreakdownDto
{
    public string Label { get; set; } = string.Empty;   // tipo de equipo
    public int Count { get; set; }
    public decimal Crc { get; set; }
    public decimal Usd { get; set; }
}
