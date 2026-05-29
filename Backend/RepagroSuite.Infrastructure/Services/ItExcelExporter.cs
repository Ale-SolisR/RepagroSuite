using ClosedXML.Excel;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Features.ITAssets.DTOs;

namespace RepagroSuite.Infrastructure.Services;

/// <summary>Exporta el inventario TI a un libro Excel profesional con ClosedXML:
/// hoja "Resumen" (KPIs + distribuciones) y hoja "Inventario" (detalle con filtros,
/// paneles congelados, formato de moneda/fecha y colores por estado).</summary>
public class ItExcelExporter : IItExcelExporter
{
    private static readonly XLColor Brand = XLColor.FromHtml("#0E6B4B");
    private static readonly XLColor BrandDark = XLColor.FromHtml("#0A5239");
    private static readonly XLColor Ink = XLColor.FromHtml("#13211C");
    private static readonly XLColor Muted = XLColor.FromHtml("#4A5750");
    private static readonly XLColor Header = XLColor.FromHtml("#F1F5F2");
    private static readonly XLColor Zebra = XLColor.FromHtml("#F7FAF8");

    public byte[] ExportInventory(IReadOnlyList<ItAssetExportRow> rows, ItDashboardDto d)
    {
        using var wb = new XLWorkbook();
        wb.Properties.Author = "RepagroSuite · TI";
        wb.Properties.Title = "Inventario Tecnológico";
        wb.Properties.Company = "REPAGRO S.A.";

        BuildSummarySheet(wb, d, rows.Count);
        BuildInventorySheet(wb, rows);

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    // ─── Hoja Resumen ────────────────────────────────────────────────────────────
    private static void BuildSummarySheet(XLWorkbook wb, ItDashboardDto d, int total)
    {
        var ws = wb.Worksheets.Add("Resumen");
        ws.ShowGridLines = false;
        ws.Column(1).Width = 3;
        for (int c = 2; c <= 7; c++) ws.Column(c).Width = 18;

        // Banner
        var banner = ws.Range("B2:G3").Merge();
        banner.Value = "REPAGRO · INVENTARIO TECNOLÓGICO";
        banner.Style.Fill.BackgroundColor = Brand;
        banner.Style.Font.FontColor = XLColor.White;
        banner.Style.Font.Bold = true;
        banner.Style.Font.FontSize = 18;
        banner.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        banner.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        var sub = ws.Range("B4:G4").Merge();
        sub.Value = $"Reporte ejecutivo generado el {d.GeneratedAt} · {total} activos en inventario";
        sub.Style.Font.FontColor = Muted;
        sub.Style.Font.FontSize = 10;
        sub.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        int row = 6;
        row = SectionTitle(ws, row, "Indicadores clave");

        // KPI cards (2 columnas de pares etiqueta/valor)
        var kpis = new (string Label, string Value)[]
        {
            ("Total de activos", total.ToString()),
            ("Asignados", $"{d.Assigned}  ({d.AssignmentRatePct:0.#}%)"),
            ("Disponibles", $"{d.Available}  ({d.AvailabilityRatePct:0.#}%)"),
            ("En taller (rep.+mant.)", (d.UnderRepair + d.UnderMaintenance).ToString()),
            ("Incidencias", $"{d.Damaged + d.Lost + d.Stolen}  ({d.IncidentRatePct:0.#}%)"),
            ("Dados de baja", d.Disposed.ToString()),
            ("Calidad de datos", $"{d.DataQualityPct:0.#}%"),
            ("Antigüedad promedio", $"{d.AvgAgeMonths:0.#} meses"),
            ("Garantías vigentes", d.WarrantyActive.ToString()),
            ("Garantías por vencer (≤60d)", d.WarrantyExpiringSoon.ToString()),
            ("Inversión registrada (CRC)", d.TotalCostCrc.ToString("#,##0")),
            ("Inversión registrada (USD)", d.TotalCostUsd.ToString("#,##0")),
        };
        foreach (var (label, value) in kpis)
        {
            ws.Cell(row, 2).Value = label;
            ws.Cell(row, 2).Style.Font.FontColor = Muted;
            ws.Cell(row, 2).Style.Font.FontSize = 10;
            var v = ws.Range(row, 3, row, 4).Merge();
            v.Value = value;
            v.Style.Font.Bold = true;
            v.Style.Font.FontColor = Ink;
            v.Style.Font.FontSize = 11;
            ws.Cell(row, 2).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
            v.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
            row++;
        }

        row += 1;
        row = DistributionTable(ws, row, "Distribución por estado", d.ByStatus);
        row += 1;
        row = DistributionTable(ws, row, "Distribución por tipo", d.ByType);
        row += 1;
        row = DistributionTable(ws, row, "Distribución por departamento", d.ByDepartment);

        // Pie de página
        row += 1;
        var foot = ws.Range(row, 2, row, 7).Merge();
        foot.Value = "Documento generado por RepagroSuite. No incluye contraseñas por política de seguridad.";
        foot.Style.Font.FontSize = 8;
        foot.Style.Font.FontColor = Muted;
        foot.Style.Font.Italic = true;
    }

    private static int SectionTitle(IXLWorksheet ws, int row, string text)
    {
        var t = ws.Range(row, 2, row, 7).Merge();
        t.Value = text;
        t.Style.Font.Bold = true;
        t.Style.Font.FontColor = BrandDark;
        t.Style.Font.FontSize = 12;
        ws.Range(row + 1, 2, row + 1, 7).Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        ws.Range(row + 1, 2, row + 1, 7).Style.Border.BottomBorderColor = Brand;
        return row + 2;
    }

    private static int DistributionTable(IXLWorksheet ws, int row, string title, IEnumerable<ItCountByLabelDto> data)
    {
        row = SectionTitle(ws, row, title);
        var list = data.ToList();
        int totalCount = list.Sum(x => x.Count);

        ws.Cell(row, 2).Value = "Categoría";
        ws.Cell(row, 3).Value = "Cantidad";
        ws.Cell(row, 4).Value = "%";
        var head = ws.Range(row, 2, row, 4);
        head.Style.Fill.BackgroundColor = Header;
        head.Style.Font.Bold = true;
        head.Style.Font.FontColor = Ink;
        row++;

        foreach (var item in list)
        {
            ws.Cell(row, 2).Value = item.Label;
            ws.Cell(row, 3).Value = item.Count;
            ws.Cell(row, 4).Value = totalCount == 0 ? 0 : (double)item.Count / totalCount;
            ws.Cell(row, 4).Style.NumberFormat.Format = "0.0%";
            if ((row % 2) == 0) ws.Range(row, 2, row, 4).Style.Fill.BackgroundColor = Zebra;
            row++;
        }
        return row;
    }

    // ─── Hoja Inventario ─────────────────────────────────────────────────────────
    private static void BuildInventorySheet(XLWorkbook wb, IReadOnlyList<ItAssetExportRow> rows)
    {
        var ws = wb.Worksheets.Add("Inventario");

        string[] headers =
        {
            "Código", "Tipo", "Marca", "Modelo", "Serie", "Placa", "Estado", "Condición",
            "Ubicación", "Detalle ubic.", "Departamento", "Responsable",
            "F. compra", "Proveedor", "Costo", "Moneda", "Garantía", "Fin garantía",
            "S.O.", "Procesador", "RAM (GB)", "Disco (GB)", "IP", "AnyDesk", "M365", "Observaciones"
        };

        for (int i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        var headRange = ws.Range(1, 1, 1, headers.Length);
        headRange.Style.Fill.BackgroundColor = Brand;
        headRange.Style.Font.FontColor = XLColor.White;
        headRange.Style.Font.Bold = true;
        headRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        ws.Row(1).Height = 24;

        int r = 2;
        foreach (var a in rows)
        {
            int c = 1;
            ws.Cell(r, c++).Value = a.InternalCode;
            ws.Cell(r, c++).Value = a.AssetType;
            ws.Cell(r, c++).Value = a.Brand ?? "";
            ws.Cell(r, c++).Value = a.Model ?? "";
            ws.Cell(r, c++).Value = a.SerialNumber ?? "";
            ws.Cell(r, c++).Value = a.AssetTag ?? "";

            var statusCell = ws.Cell(r, c++);
            statusCell.Value = a.Status;
            statusCell.Style.Fill.BackgroundColor = StatusColor(a.Status);
            statusCell.Style.Font.FontColor = StatusFont(a.Status);
            statusCell.Style.Font.Bold = true;
            statusCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.Cell(r, c++).Value = a.Condition;
            ws.Cell(r, c++).Value = a.Location ?? "";
            ws.Cell(r, c++).Value = a.LocationDetail ?? "";
            ws.Cell(r, c++).Value = a.Department ?? "";
            ws.Cell(r, c++).Value = a.Holder ?? "";

            var purchase = ws.Cell(r, c++);
            if (a.PurchaseDate.HasValue) { purchase.Value = a.PurchaseDate.Value; purchase.Style.NumberFormat.Format = "dd/mm/yyyy"; }

            ws.Cell(r, c++).Value = a.Supplier ?? "";

            var cost = ws.Cell(r, c++);
            if (a.Cost.HasValue) { cost.Value = a.Cost.Value; cost.Style.NumberFormat.Format = "#,##0.00"; }

            ws.Cell(r, c++).Value = a.Currency ?? "";
            ws.Cell(r, c++).Value = a.Warranty;

            var wEnd = ws.Cell(r, c++);
            if (a.WarrantyEndDate.HasValue) { wEnd.Value = a.WarrantyEndDate.Value; wEnd.Style.NumberFormat.Format = "dd/mm/yyyy"; }

            ws.Cell(r, c++).Value = a.OperatingSystem ?? "";
            ws.Cell(r, c++).Value = a.Processor ?? "";
            if (a.RamGb.HasValue) ws.Cell(r, c).Value = a.RamGb.Value; c++;
            if (a.DiskGb.HasValue) ws.Cell(r, c).Value = a.DiskGb.Value; c++;
            ws.Cell(r, c++).Value = a.IpAddress ?? "";
            ws.Cell(r, c++).Value = a.AnyDeskId ?? "";
            ws.Cell(r, c++).Value = a.Microsoft365User ?? "";
            ws.Cell(r, c++).Value = a.Notes ?? "";

            if ((r % 2) == 0)
                for (int col = 1; col <= headers.Length; col++)
                    if (col != 7 && ws.Cell(r, col).Style.Fill.BackgroundColor == XLColor.NoColor)
                        ws.Cell(r, col).Style.Fill.BackgroundColor = Zebra;

            r++;
        }

        var used = ws.Range(1, 1, Math.Max(1, r - 1), headers.Length);
        used.Style.Font.FontSize = 10;
        used.Style.Border.InsideBorder = XLBorderStyleValues.Hair;
        used.Style.Border.InsideBorderColor = XLColor.FromHtml("#E2E8E4");
        used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

        ws.SheetView.FreezeRows(1);
        ws.Range(1, 1, Math.Max(1, r - 1), headers.Length).SetAutoFilter();
        ws.Columns(1, headers.Length).AdjustToContents(1, Math.Max(1, r - 1), 8, 42);
        ws.Column(26).Width = 40; // Observaciones
    }

    private static XLColor StatusColor(string status) => status switch
    {
        "Disponible" => XLColor.FromHtml("#ECFDF5"),
        "Asignado" => XLColor.FromHtml("#EFF6FF"),
        "Prestado" => XLColor.FromHtml("#F5F3FF"),
        "En reparación" or "En mantenimiento" or "En revisión" => XLColor.FromHtml("#FFFBEB"),
        "Dañado" or "Perdido" or "Robado" => XLColor.FromHtml("#FEF2F2"),
        "Dado de baja" or "Inactivo" => XLColor.FromHtml("#F1F5F9"),
        _ => XLColor.NoColor
    };

    private static XLColor StatusFont(string status) => status switch
    {
        "Disponible" => XLColor.FromHtml("#065F46"),
        "Asignado" => XLColor.FromHtml("#1D4ED8"),
        "Prestado" => XLColor.FromHtml("#6D28D9"),
        "En reparación" or "En mantenimiento" or "En revisión" => XLColor.FromHtml("#92400E"),
        "Dañado" or "Perdido" or "Robado" => XLColor.FromHtml("#991B1B"),
        _ => XLColor.FromHtml("#475569")
    };
}
