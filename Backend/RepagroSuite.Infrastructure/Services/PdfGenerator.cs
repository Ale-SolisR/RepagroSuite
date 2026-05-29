using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RepagroSuite.Application.Common.Interfaces;

namespace RepagroSuite.Infrastructure.Services;

/// <summary>Genera el PDF de una boleta TI con QuestPDF (licencia Community).</summary>
public class PdfGenerator : IPdfGenerator
{
    private static readonly string Brand = "#0E6B4B";

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
                        row.RelativeItem().Text(t => { t.Span("Colaborador: ").SemiBold(); t.Span(m.EmployeeName ?? "—"); });
                        row.RelativeItem().Text(t => { t.Span("Responsable TI: ").SemiBold(); t.Span(m.ResponsibleName ?? "—"); });
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

    /// <summary>Acepta data URL ("data:image/png;base64,…") o base64 puro.</summary>
    private static byte[]? TryDecode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var raw = value.Contains(',') ? value[(value.IndexOf(',') + 1)..] : value;
        try { return Convert.FromBase64String(raw); }
        catch (FormatException) { return null; }
    }
}
