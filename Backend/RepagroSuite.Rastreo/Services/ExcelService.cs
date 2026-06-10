using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Rastreo.Api.Models;

namespace Rastreo.Api.Services;

public class ExcelService
{
    private static readonly XLColor HeaderBg = XLColor.FromHtml("#1e3a8a");
    private static readonly XLColor HeaderFg = XLColor.White;
    private static readonly XLColor ZebraBg = XLColor.FromHtml("#f1f5f9");
    private static readonly XLColor InfoBg = XLColor.FromHtml("#dbeafe");

    public byte[] GenerarRegistro(Registro registro, IReadOnlyList<Enfermedad>? enfermedadesCatalogo = null)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add(LimpiarNombreHoja(registro.Granja?.Nombre ?? "SIN GRANJA"));
        var detalles = registro.Detalles.OrderBy(d => d.NumeroLinea).ToList();
        var enfermedades = ObtenerEnfermedades(detalles, enfermedadesCatalogo);
        var totalColumns = 8 + enfermedades.Count + 3;
        var fotoStartCol = 9 + enfermedades.Count;

        ws.Range(1, 1, 1, totalColumns).Merge();
        ws.Cell(1, 1).Value = "REVISION DE MATANZA - RASTREO PULMONAR";
        ws.Cell(1, 1).Style.Font.SetBold(true).Font.SetFontSize(16).Font.SetFontColor(HeaderFg)
            .Fill.SetBackgroundColor(HeaderBg)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        ws.Row(1).Height = 28;

        var granjaTxt = registro.Granja != null
            ? $"{registro.Granja.Codigo} - {registro.Granja.Nombre}"
            : "Sin asignar";
        FilaInfo(ws, 2, totalColumns,
            ("FECHA:", registro.FechaCreacion.ToLocalTime().ToString("dd-MM-yyyy HH:mm")),
            ("GRANJA:", granjaTxt),
            ("LOTE:", registro.Lote ?? "-"));
        FilaInfo(ws, 3, totalColumns,
            ("ESTADO:", registro.Estado),
            ("TOTAL CERDOS:", detalles.Count.ToString()),
            ("OBS/VACUNAS:", $"{(registro.Observaciones ?? "-")} | Vacunas: {VacunasTexto(registro)}"));
        ws.Row(4).Height = 8;

        const int headerRow = 5;
        var headers = new List<string> { "#", "AL", "CL", "DL", "AD", "CD", "DD", "L" };
        headers.AddRange(enfermedades.Select(e => e.Nombre.ToUpperInvariant()));
        headers.AddRange(new[] { "FOTO 1", "FOTO 2", "FOTO 3" });
        for (int i = 0; i < headers.Count; i++)
        {
            var c = ws.Cell(headerRow, i + 1);
            c.Value = headers[i];
            c.Style.Font.SetBold(true).Font.SetFontColor(HeaderFg).Font.SetFontSize(10)
                .Fill.SetBackgroundColor(HeaderBg)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                .Alignment.SetWrapText(true)
                .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                .Border.SetOutsideBorderColor(XLColor.White);
        }
        ws.Row(headerRow).Height = 42;

        var firstDataRow = headerRow + 1;
        for (int i = 0; i < detalles.Count; i++)
        {
            var d = detalles[i];
            var row = firstDataRow + i;
            var values = new List<object?>
            {
                d.NumeroLinea,
                d.ApicalIzquierdo, d.CardiacoIzquierdo, d.DiafragmaticoIzquierdo,
                d.ApicalDerecho, d.CardiacoDerecho, d.DiafragmaticoDerecho, d.Accesorio
            };
            values.AddRange(enfermedades.Select(e => EnfermedadValorHelper.Texto(d, e)));
            for (int col = 0; col < values.Count; col++)
                ws.Cell(row, col + 1).Value = XLCellValue.FromObject(values[col]);

            for (int c = 1; c <= totalColumns; c++)
            {
                var cell = ws.Cell(row, c);
                cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Border.SetOutsideBorderColor(XLColor.FromHtml("#cbd5e1"))
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center)
                    .Alignment.SetVertical(XLAlignmentVerticalValues.Center)
                    .Font.SetFontSize(10);
                if (i % 2 == 1) cell.Style.Fill.SetBackgroundColor(ZebraBg);
            }
            ws.Row(row).Height = 65;

            var fotos = d.Fotos.OrderBy(f => f.Orden).Take(3).ToList();
            for (int idx = 0; idx < 3; idx++)
            {
                var col = fotoStartCol + idx;
                var foto = fotos.FirstOrDefault(f => f.Orden == idx + 1);
                if (foto?.FotoBinario.Length > 0)
                {
                    try
                    {
                        using var ms = new MemoryStream(foto.FotoBinario);
                        var pic = ws.AddPicture(ms, $"foto_{d.NumeroLinea}_{idx + 1}");
                        pic.MoveTo(ws.Cell(row, col), 4, 4);
                        pic.Placement = XLPicturePlacement.Move;
                        pic.WithSize(105, 75);
                    }
                    catch
                    {
                        ws.Cell(row, col).Value = "Foto invalida";
                    }
                }
                else
                {
                    ws.Cell(row, col).Value = "-";
                    ws.Cell(row, col).Style.Font.SetItalic(true).Font.SetFontColor(XLColor.FromHtml("#94a3b8"));
                }
            }
        }

        for (int c = 1; c <= totalColumns; c++)
            ws.Column(c).Width = c <= 8 ? 7 : c >= fotoStartCol ? 16 : 14;
        ws.SheetView.FreezeRows(headerRow);

        var leyendaRow = firstDataRow + detalles.Count + 2;
        ws.Range(leyendaRow, 1, leyendaRow, totalColumns).Merge();
        ws.Cell(leyendaRow, 1).Value = "LEYENDA";
        ws.Cell(leyendaRow, 1).Style.Font.SetBold(true).Font.SetFontColor(HeaderFg)
            .Fill.SetBackgroundColor(HeaderBg)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        var leyenda = new[]
        {
            ("Lobulos (AL, CL, DL, AD, CD, DD, L):", "0 = sin seleccion; 1, 2, 3, 4 = zona seleccionada"),
            ("Enfermedades SCORE_0_4:", "0 a 4"),
            ("Enfermedades Si/No:", "1 = SI; 0 = NO"),
            ("Vacunas:", "Se muestran desde el catalogo dinamico y se asocian a nivel de registro/lote"),
            ("FOTO 1/2/3:", "Hasta 3 fotografias por cerdo")
        };
        for (int i = 0; i < leyenda.Length; i++)
        {
            var r = leyendaRow + 1 + i;
            ws.Cell(r, 1).Value = leyenda[i].Item1;
            ws.Cell(r, 1).Style.Font.SetBold(true);
            ws.Range(r, 1, r, Math.Min(4, totalColumns)).Merge();
            ws.Cell(r, Math.Min(5, totalColumns)).Value = leyenda[i].Item2;
            if (totalColumns >= 5) ws.Range(r, 5, r, totalColumns).Merge();
            ws.Range(r, 1, r, totalColumns).Style.Alignment.SetWrapText(true).Font.SetFontSize(9);
            ws.Row(r).Height = 22;
        }

        using var stream = new MemoryStream();
        wb.SaveAs(stream);
        return stream.ToArray();
    }

    private static void FilaInfo(IXLWorksheet ws, int row, int totalColumns, params (string label, string value)[] items)
    {
        var spans = new[]
        {
            (label: 1, value: 2, end: 4),
            (label: 5, value: 6, end: Math.Max(6, totalColumns - 3)),
            (label: Math.Max(7, totalColumns - 2), value: Math.Max(8, totalColumns - 1), end: totalColumns)
        };
        for (int i = 0; i < items.Length; i++)
        {
            var s = spans[i];
            ws.Cell(row, s.label).Value = items[i].label;
            ws.Cell(row, s.value).Value = items[i].value;
            if (s.end > s.value) ws.Range(row, s.value, row, s.end).Merge();
            ws.Cell(row, s.label).Style.Font.SetBold(true).Fill.SetBackgroundColor(InfoBg);
            ws.Cell(row, s.value).Style.Fill.SetBackgroundColor(InfoBg);
        }
        ws.Range(row, 1, row, totalColumns).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Thin)
            .Alignment.SetVertical(XLAlignmentVerticalValues.Center);
        ws.Row(row).Height = 24;
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
            .Where(e => e.Activo)
            .OrderBy(e => e.Orden).ThenBy(e => e.Nombre)
            .ToList();
    }

    private static string LimpiarNombreHoja(string nombre)
    {
        var s = nombre.Trim();
        foreach (var ch in new[] { ':', '\\', '/', '?', '*', '[', ']' })
            s = s.Replace(ch, ' ');
        return s.Length > 31 ? s[..31] : s;
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
}
