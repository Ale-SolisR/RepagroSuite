using RepagroSuite.Application.Features.ITAssets.DTOs;

namespace RepagroSuite.Application.Common.Interfaces;

/// <summary>Genera el libro Excel profesional del inventario TI (portada/resumen + detalle).</summary>
public interface IItExcelExporter
{
    byte[] ExportInventory(IReadOnlyList<ItAssetExportRow> rows, ItDashboardDto dashboard);
}
