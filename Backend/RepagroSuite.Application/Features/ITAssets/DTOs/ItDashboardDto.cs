namespace RepagroSuite.Application.Features.ITAssets.DTOs;

/// <summary>KPIs e indicadores del dashboard de TI (propuesta §13).</summary>
public class ItDashboardDto
{
    public int TotalAssets { get; set; }
    public int Assigned { get; set; }
    public int Available { get; set; }
    public int UnderRepair { get; set; }
    public int UnderMaintenance { get; set; }
    public int Disposed { get; set; }
    public decimal TotalCostCrc { get; set; }
    public decimal TotalCostUsd { get; set; }

    // Alertas
    public int WithoutSerial { get; set; }
    public int WithoutTag { get; set; }
    public int WithoutHolder { get; set; }
    public int WarrantyExpiringSoon { get; set; }   // ≤ 60 días

    public IEnumerable<ItCountByLabelDto> ByType { get; set; } = [];
    public IEnumerable<ItCountByLabelDto> ByStatus { get; set; } = [];
    public IEnumerable<ItCountByLabelDto> ByDepartment { get; set; } = [];
}

public class ItCountByLabelDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}
