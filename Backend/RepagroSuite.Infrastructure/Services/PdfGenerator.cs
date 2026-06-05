using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Features.ITAssets.DTOs;

namespace RepagroSuite.Infrastructure.Services;

/// <summary>Genera el PDF de una boleta TI con QuestPDF (licencia Community).</summary>
public class PdfGenerator : IPdfGenerator
{
    private static readonly string Brand = "#0E6B4B";
    private static readonly string[] Palette =
        { "#0E6B4B", "#2563EB", "#F59E0B", "#B42318", "#6B7280", "#0891B2", "#7C3AED", "#475569" };

    static PdfGenerator()
    {
        // Community: gratuita para empresas con ingresos < USD 1M (caso Repagro).
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerateTicketPdf(TicketPdfModel m)
    {
        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(36);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor("#13211C"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("REPAGRO · TI").Bold().FontSize(14).FontColor(Brand);
                            c.Item().Text("Boleta de inventario tecnológico").FontSize(9).FontColor("#4A5750");
                        });
                        row.ConstantItem(180).AlignRight().Column(c =>
                        {
                            c.Item().Text(m.TicketNumber).Bold().FontSize(13);
                            c.Item().Text(m.TypeName).FontSize(9).FontColor("#4A5750");
                            c.Item().Text(m.IssuedAt).FontSize(9).FontColor("#4A5750");
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Brand);
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(t => { t.Span("Colaborador: ").SemiBold(); t.Span(m.EmployeeName ?? "—"); });
                            c.Item().Text(t => { t.Span("Cédula: ").SemiBold().FontSize(9).FontColor("#4A5750"); t.Span(m.EmployeeIdentification ?? "—").FontSize(9).FontColor("#4A5750"); });
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(t => { t.Span("Responsable TI: ").SemiBold(); t.Span(m.ResponsibleName ?? "—"); });
                            c.Item().Text(t => { t.Span("Cédula: ").SemiBold().FontSize(9).FontColor("#4A5750"); t.Span(m.ResponsibleIdentification ?? "—").FontSize(9).FontColor("#4A5750"); });
                        });
                    });

                    if (m.Lines.Count > 0)
                    {
                        col.Item().Text("Activos incluidos").SemiBold().FontColor(Brand);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); c.RelativeColumn(2); });
                            void H(string s) => table.Cell().Background("#F1F5F2").Padding(4).Text(s).SemiBold().FontSize(9);
                            H("Código"); H("Tipo"); H("Descripción"); H("Serie"); H("Condición");
                            foreach (var l in m.Lines)
                            {
                                void C(string? s) => table.Cell().BorderBottom(0.5f).BorderColor("#E2E8E4").Padding(4).Text(s ?? "—").FontSize(9);
                                C(l.InternalCode); C(l.TypeName); C(l.Description); C(l.SerialNumber); C(l.Condition);
                            }
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(m.Accessories))
                        col.Item().Text(t => { t.Span("Accesorios: ").SemiBold(); t.Span(m.Accessories); });

                    if (!string.IsNullOrWhiteSpace(m.Notes))
                        col.Item().Text(t => { t.Span("Observaciones: ").SemiBold(); t.Span(m.Notes); });

                    var photos = m.PhotosBase64.Select(TryDecode).Where(b => b is not null).Cast<byte[]>().ToList();
                    if (photos.Count > 0)
                    {
                        col.Item().PaddingTop(4).Text("Evidencia fotográfica").SemiBold().FontColor(Brand);
                        col.Item().Row(row =>
                        {
                            foreach (var p in photos.Take(3))
                                row.RelativeItem().Padding(2).Height(120).Image(p).FitArea();
                        });
                    }

                    if (m.Signatures.Count > 0)
                    {
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            foreach (var s in m.Signatures)
                            {
                                row.RelativeItem().Padding(6).Column(c =>
                                {
                                    var bytes = TryDecode(s.ImageBase64);
                                    if (bytes is not null) c.Item().Height(70).Image(bytes).FitHeight();
                                    c.Item().LineHorizontal(0.5f).LineColor("#94A3A0");
                                    c.Item().Text(s.Label).SemiBold().FontSize(9);
                                    c.Item().Text(s.SignerName ?? "—").FontSize(9);
                                    c.Item().Text($"Cédula: {(string.IsNullOrWhiteSpace(s.SignerIdentification) ? "—" : s.SignerIdentification)}").FontSize(8).FontColor("#4A5750");
                                    c.Item().Text(s.SignedAt).FontSize(8).FontColor("#4A5750");
                                });
                            }
                        });
                    }
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor("#E2E8E4");
                    col.Item().PaddingTop(4).Text(
                        "Firma electrónica de evidencia, no certificada legalmente (Ley 8454 CR). " +
                        "Documento generado por RepagroSuite — boleta inmutable, validar por su consecutivo y hash.")
                        .FontSize(7).FontColor("#94A3A0");
                });
            });
        }).GeneratePdf();
    }

    // ─── Dashboard ejecutivo ──────────────────────────────────────────────────────
    public byte[] GenerateDashboardPdf(ItDashboardDto d)
    {
        var fmt = new System.Globalization.NumberFormatInfo { NumberGroupSeparator = ",", NumberDecimalSeparator = "." };
        string Money(decimal v) => v.ToString("#,##0", fmt);

        return Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(34);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(t => t.FontSize(9).FontColor("#13211C"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("REPAGRO · TI").Bold().FontSize(15).FontColor(Brand);
                            c.Item().Text("Reporte ejecutivo · Inventario tecnológico").FontSize(9).FontColor("#4A5750");
                        });
                        row.ConstantItem(170).AlignRight().Column(c =>
                        {
                            c.Item().Text($"{d.TotalAssets} activos").Bold().FontSize(13);
                            c.Item().Text($"Generado: {d.GeneratedAt}").FontSize(8).FontColor("#4A5750");
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor(Brand);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(12);

                    // KPIs
                    col.Item().Text("Indicadores clave").SemiBold().FontSize(11).FontColor(Brand);
                    col.Item().Row(row =>
                    {
                        row.Spacing(6);
                        Kpi(row, "Asignados", $"{d.Assigned}", $"{d.AssignmentRatePct:0.#}% de operables", "#2563EB");
                        Kpi(row, "Disponibles", $"{d.Available}", $"{d.AvailabilityRatePct:0.#}% disponibilidad", "#0E6B4B");
                        Kpi(row, "En taller", $"{d.UnderRepair + d.UnderMaintenance}", $"{d.UnderRepair} rep · {d.UnderMaintenance} mant", "#F59E0B");
                        Kpi(row, "Incidencias", $"{d.Damaged + d.Lost + d.Stolen}", $"{d.IncidentRatePct:0.#}% de la flota", "#B42318");
                    });
                    col.Item().Row(row =>
                    {
                        row.Spacing(6);
                        Kpi(row, "Calidad de datos", $"{d.DataQualityPct:0.#}%", "serie · placa · responsable", "#0891B2");
                        Kpi(row, "Antigüedad prom.", $"{d.AvgAgeMonths:0.#}", "meses de uso", "#7C3AED");
                        Kpi(row, "Garantías vigentes", $"{d.WarrantyActive}", $"{d.WarrantyExpiringSoon} por vencer ≤60d", "#0E6B4B");
                        Kpi(row, "Inversión", $"₡{Money(d.TotalCostCrc)}", $"+ ${Money(d.TotalCostUsd)} USD", "#475569");
                    });

                    // Insight global
                    col.Item().Background("#F1F5F2").Padding(8).Text(GlobalInsight(d)).FontSize(9).FontColor("#13211C");

                    // Gráficos en dos columnas
                    col.Item().Row(row =>
                    {
                        row.Spacing(14);
                        row.RelativeItem().Element(e => Chart(e, "Por estado", d.ByStatus, d.TotalAssets));
                        row.RelativeItem().Element(e => Chart(e, "Por tipo de equipo", d.ByType, d.TotalAssets));
                    });
                    col.Item().Row(row =>
                    {
                        row.Spacing(14);
                        row.RelativeItem().Element(e => Chart(e, "Por departamento", d.ByDepartment, d.TotalAssets));
                        row.RelativeItem().Element(e => Chart(e, "Por marca", d.ByBrand, d.TotalAssets));
                    });
                    col.Item().Row(row =>
                    {
                        row.Spacing(14);
                        row.RelativeItem().Element(e => Chart(e, "Antigüedad de la flota", d.ByAgeBucket, Math.Max(1, d.AssetsWithPurchaseDate)));
                        row.RelativeItem().Element(e => Chart(e, "Adquisiciones (12 meses)",
                            d.AcquisitionTrend.Select(t => new ItCountByLabelDto { Label = t.Label, Count = t.Count }), 0));
                    });

                    // Valor de inventario por colaborador
                    var valueList = d.ValueByHolder.ToList();
                    if (valueList.Count > 0)
                    {
                        col.Item().Element(e => e.Border(0.8f).BorderColor("#E2E8E4").Padding(10).Column(c =>
                        {
                            c.Item().Text("Valor de inventario por colaborador").SemiBold().FontSize(10).FontColor("#13211C");
                            var topV = valueList[0];
                            c.Item().PaddingBottom(4).Text(
                                $"{topV.Label} custodia el mayor valor asignado ({topV.AssetCount} equipo(s): ₡{Money(topV.Crc)}" +
                                (topV.Usd > 0 ? $" + ${Money(topV.Usd)}" : "") + ").").FontSize(7.5f).FontColor("#4A5750");

                            c.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cd => { cd.RelativeColumn(4); cd.RelativeColumn(1); cd.RelativeColumn(2); cd.RelativeColumn(2); });
                                void H(string s, bool right = false) { var cell = table.Cell().Background("#F1F5F2").Padding(4); (right ? cell.AlignRight() : cell).Text(s).SemiBold().FontSize(8); }
                                H("Colaborador"); H("Equipos", true); H("Valor CRC", true); H("Valor USD", true);
                                foreach (var v in valueList)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor("#E2E8E4").Padding(4).Text(v.Label).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#E2E8E4").Padding(4).AlignRight().Text(v.AssetCount.ToString()).FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#E2E8E4").Padding(4).AlignRight().Text(v.Crc > 0 ? $"₡{Money(v.Crc)}" : "—").FontSize(8);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#E2E8E4").Padding(4).AlignRight().Text(v.Usd > 0 ? $"${Money(v.Usd)}" : "—").FontSize(8);
                                }
                            });
                        }));
                    }
                });

                page.Footer().Column(col =>
                {
                    col.Item().LineHorizontal(0.5f).LineColor("#E2E8E4");
                    col.Item().PaddingTop(4).Text(
                        "Reporte generado por RepagroSuite. Cifras en tiempo real al momento de la exportación. " +
                        "No incluye contraseñas por política de seguridad.")
                        .FontSize(7).FontColor("#94A3A0");
                });
            });
        }).GeneratePdf();
    }

    private static void Kpi(RowDescriptor row, string label, string value, string sub, string color)
    {
        row.RelativeItem().Border(0.8f).BorderColor("#E2E8E4").Background("#FFFFFF").Padding(8).Column(c =>
        {
            c.Item().Text(label.ToUpperInvariant()).FontSize(7).FontColor("#4A5750").LetterSpacing(0.3f);
            c.Item().PaddingTop(2).Text(value).Bold().FontSize(17).FontColor(color);
            c.Item().Text(sub).FontSize(7).FontColor("#4A5750");
        });
    }

    private static void Chart(IContainer container, string title, IEnumerable<ItCountByLabelDto> data, int total)
    {
        var list = data.ToList();
        int max = list.Count == 0 ? 1 : Math.Max(1, list.Max(x => x.Count));

        container.Border(0.8f).BorderColor("#E2E8E4").Padding(10).Column(col =>
        {
            col.Item().Text(title).SemiBold().FontSize(10).FontColor("#13211C");
            col.Item().PaddingBottom(4).Text(SeriesInsight(title, list, total)).FontSize(7.5f).FontColor("#4A5750");

            if (list.Count == 0)
            {
                col.Item().PaddingVertical(10).Text("Sin datos").FontSize(8).FontColor("#94A3A0");
                return;
            }

            col.Spacing(3);
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                float barPx = 6 + (float)item.Count / max * 150f;
                string color = Palette[i % Palette.Length];
                col.Item().Row(r =>
                {
                    r.ConstantItem(86).Text(item.Label).FontSize(7.5f).FontColor("#13211C");
                    r.RelativeItem().Row(br =>
                    {
                        br.AutoItem().Height(11).Width(barPx).Background(color);
                        br.AutoItem().PaddingLeft(4).AlignMiddle().Text($"{item.Count}").FontSize(7.5f).SemiBold();
                    });
                });
            }
        });
    }

    private static string GlobalInsight(ItDashboardDto d)
    {
        int taller = d.UnderRepair + d.UnderMaintenance;
        int incid = d.Damaged + d.Lost + d.Stolen;
        var parts = new List<string>
        {
            $"De {d.TotalAssets} activos, {d.Assigned} están asignados ({d.AssignmentRatePct:0.#}%) y {d.Available} disponibles para entrega."
        };
        if (taller > 0) parts.Add($"{taller} en taller.");
        if (incid > 0) parts.Add($"{incid} con incidencias ({d.IncidentRatePct:0.#}%).");
        if (d.WarrantyExpiringSoon > 0) parts.Add($"{d.WarrantyExpiringSoon} garantía(s) vencen en ≤60 días.");
        parts.Add($"Calidad de datos {d.DataQualityPct:0.#}%.");
        return string.Join(" ", parts);
    }

    private static string SeriesInsight(string title, List<ItCountByLabelDto> list, int total)
    {
        if (list.Count == 0) return "Sin información disponible.";
        if (title.Contains("Adquisiciones"))
        {
            int sum = list.Sum(x => x.Count);
            var top = list.OrderByDescending(x => x.Count).First();
            return sum == 0 ? "Sin adquisiciones registradas con fecha en el periodo."
                : $"{sum} equipo(s) ingresados; pico en {top.Label} ({top.Count}).";
        }
        var first = list.OrderByDescending(x => x.Count).First();
        double pct = total > 0 ? (double)first.Count / total * 100 : 0;
        return total > 0
            ? $"Predomina «{first.Label}» con {first.Count} ({pct:0.#}%) de {total}."
            : $"Predomina «{first.Label}» con {first.Count}.";
    }

    /// <summary>Acepta data URL ("data:image/png;base64,…") o base64 puro.</summary>
    private static byte[]? TryDecode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var raw = value.Contains(',') ? value[(value.IndexOf(',') + 1)..] : value;
        try { return Convert.FromBase64String(raw); }
        catch (FormatException) { return null; }
    }
}
