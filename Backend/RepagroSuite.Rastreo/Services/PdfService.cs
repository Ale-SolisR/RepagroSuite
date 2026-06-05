using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Rastreo.Api.Models;

namespace Rastreo.Api.Services;

public class PdfService
{
    private static readonly Color HeaderBg = Color.FromHex("#1e3a8a");
    private static readonly Color InfoBg = Color.FromHex("#dbeafe");
    private static readonly Color ZebraBg = Color.FromHex("#f1f5f9");
    private static readonly Color BorderColor = Color.FromHex("#cbd5e1");
    private static readonly Color Slate400 = Color.FromHex("#94a3b8");

    public byte[] GenerarRegistro(Registro registro, IReadOnlyList<Enfermedad>? enfermedadesCatalogo = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var detalles = registro.Detalles.OrderBy(d => d.NumeroLinea).ToList();
        var enfermedades = ObtenerEnfermedades(detalles, enfermedadesCatalogo);
        var granjaTxt = registro.Granja != null
            ? $"{registro.Granja.Codigo} - {registro.Granja.Nombre}"
            : "Sin asignar";

        var doc = Document.Create(c =>
        {
            c.Page(p =>
            {
                p.Size(PageSizes.A4.Landscape());
                p.Margin(15);
                p.DefaultTextStyle(t => t.FontSize(7).FontColor(Colors.Black));

                p.Header().Column(col =>
                {
                    // Logo arriba a la derecha.
                    var logo = BrandAssets.LogoRepagro();
                    if (logo != null)
                        col.Item().PaddingBottom(4).AlignRight().Height(24).Image(logo).FitArea();
                    col.Item().Background(HeaderBg).Padding(8)
                        .Text("REVISION DE MATANZA - RASTREO PULMONAR")
                        .FontSize(13).Bold().FontColor(Colors.White).AlignCenter();
                    col.Item().PaddingTop(3).Background(InfoBg).Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Fecha: ").Bold(); t.Span(registro.FechaCreacion.ToLocalTime().ToString("dd-MM-yyyy HH:mm")); });
                        r.RelativeItem(2).Text(t => { t.Span("Granja: ").Bold(); t.Span(granjaTxt); });
                        r.RelativeItem().Text(t => { t.Span("Lote: ").Bold(); t.Span(string.IsNullOrEmpty(registro.Lote) ? "-" : registro.Lote); });
                        r.RelativeItem().Text(t => { t.Span("Total: ").Bold(); t.Span(detalles.Count.ToString()); });
                    });
                    col.Item().PaddingTop(3).Text(t =>
                    {
                        t.Span("Vacunas: ").Bold();
                        t.Span(VacunasTexto(registro));
                    });
                    if (!string.IsNullOrWhiteSpace(registro.Observaciones))
                    {
                        col.Item().PaddingTop(3).Text(t =>
                        {
                            t.Span("Observaciones: ").Bold();
                            t.Span(registro.Observaciones);
                        });
                    }
                });

                p.Content().PaddingTop(6).Table(tbl =>
                {
                    tbl.ColumnsDefinition(cd =>
                    {
                        cd.ConstantColumn(20);
                        for (int i = 0; i < 7; i++) cd.ConstantColumn(20);
                        foreach (var _ in enfermedades) cd.RelativeColumn(0.8f);
                        cd.RelativeColumn(1.5f);
                        cd.RelativeColumn(1.5f);
                        cd.RelativeColumn(1.5f);
                    });

                    foreach (var h in new[] { "#", "AL", "CL", "DL", "AD", "CD", "DD", "L" })
                        Header(tbl, h);
                    foreach (var e in enfermedades)
                        Header(tbl, Abreviar(e.Nombre));
                    Header(tbl, "FOTO 1");
                    Header(tbl, "FOTO 2");
                    Header(tbl, "FOTO 3");

                    for (int i = 0; i < detalles.Count; i++)
                    {
                        var d = detalles[i];
                        var bg = i % 2 == 1 ? ZebraBg : Colors.White;
                        Cell(tbl, d.NumeroLinea.ToString(), bg, bold: true);
                        Cell(tbl, d.ApicalIzquierdo.ToString(), bg);
                        Cell(tbl, d.CardiacoIzquierdo.ToString(), bg);
                        Cell(tbl, d.DiafragmaticoIzquierdo.ToString(), bg);
                        Cell(tbl, d.ApicalDerecho.ToString(), bg);
                        Cell(tbl, d.CardiacoDerecho.ToString(), bg);
                        Cell(tbl, d.DiafragmaticoDerecho.ToString(), bg);
                        Cell(tbl, d.Accesorio.ToString(), bg);
                        foreach (var e in enfermedades)
                            Cell(tbl, string.IsNullOrEmpty(EnfermedadValorHelper.Texto(d, e)) ? "-" : EnfermedadValorHelper.Texto(d, e), bg);

                        for (int idx = 1; idx <= 3; idx++)
                        {
                            var foto = d.Fotos.FirstOrDefault(f => f.Orden == idx);
                            if (foto?.FotoBinario.Length > 0)
                            {
                                tbl.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(2)
                                    .AlignCenter().AlignMiddle().Height(75)
                                    .Image(foto.FotoBinario).FitArea();
                            }
                            else
                            {
                                tbl.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(3)
                                    .AlignCenter().AlignMiddle().Text("-").FontColor(Slate400).Italic();
                            }
                        }
                    }
                });

                p.Footer().Column(col =>
                {
                    col.Item().PaddingTop(6).Background(InfoBg).Padding(4).Text(t =>
                    {
                        t.DefaultTextStyle(s => s.FontSize(7));
                        t.Line("Lobulos: 0 a 4. Enfermedades SCORE_0_4: 0 a 4. Enfermedades Si/No: 1 = SI; 0 = NO. Vacunas: catalogo dinamico asociado al registro/lote.");
                        t.Line("Las columnas de enfermedades se generan desde el catalogo activo.");
                    });
                    col.Item().PaddingTop(3).AlignRight().Text(t =>
                    {
                        t.Span("Pagina ").FontSize(7);
                        t.CurrentPageNumber().FontSize(7);
                        t.Span(" / ").FontSize(7);
                        t.TotalPages().FontSize(7);
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }

    private static void Header(TableDescriptor tbl, string v) =>
        tbl.Cell().Background(HeaderBg).Padding(3).AlignCenter().AlignMiddle()
            .Text(v).Bold().FontSize(6).FontColor(Colors.White);

    private static void Cell(TableDescriptor tbl, string v, Color bg, bool bold = false)
    {
        tbl.Cell().Background(bg).Border(0.5f).BorderColor(BorderColor).Padding(3)
            .AlignCenter().AlignMiddle().Text(t =>
            {
                var span = t.Span(v).FontSize(7);
                if (bold) span.Bold();
            });
    }

    private static string Abreviar(string value)
    {
        var s = value.Replace(" / ", "/");
        return s.Length <= 16 ? s.ToUpperInvariant() : s[..16].ToUpperInvariant();
    }

    private static string VacunasTexto(Registro registro)
    {
        if (!registro.UsaVacunas) return "NO";
        var nombres = registro.Vacunas
            .Where(rv => rv.Vacuna != null)
            .OrderBy(rv => rv.Vacuna!.Orden).ThenBy(rv => rv.Vacuna!.Nombre)
            .Select(rv => rv.Vacuna!.Nombre)
            .ToList();
        return nombres.Count == 0 ? "SI, sin vacuna especificada" : string.Join(", ", nombres);
    }

    private static List<Enfermedad> ObtenerEnfermedades(
        List<RegistroDetalle> detalles,
        IReadOnlyList<Enfermedad>? catalogo)
    {
        var enfermedades = catalogo?.Count > 0
            ? catalogo.ToList()
            : detalles.SelectMany(d => d.EnfermedadesValores)
                .Where(v => v.Enfermedad != null && v.Enfermedad.Activo)
                .Select(v => v.Enfermedad!)
                .GroupBy(e => e.Id)
                .Select(g => g.First())
                .ToList();
        return enfermedades
            .Where(e => e.TipoCampo != "AGUDA_CRONICA")
            .OrderBy(e => e.Orden).ThenBy(e => e.Nombre)
            .ToList();
    }
}
