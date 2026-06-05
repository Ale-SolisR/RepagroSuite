using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreo.Api.Data;
using Rastreo.Api.Dtos;
using Rastreo.Api.Models;
using Rastreo.Api.Services;

namespace Rastreo.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Rastreo")]
[Route("api/informes")]
public class InformesController : ControllerBase
{
    private readonly RastreoDbContext _db;
    private readonly EvaluacionPulmonarService _eval;
    private readonly InformePdfService _pdf;
    private readonly EmailService _email;
    private readonly ILogger<InformesController> _log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Regex_ Email = new();

    public InformesController(
        RastreoDbContext db, EvaluacionPulmonarService eval,
        InformePdfService pdf, EmailService email, ILogger<InformesController> log)
    {
        _db = db; _eval = eval; _pdf = pdf; _email = email; _log = log;
    }

    // ===================== 1. DASHBOARD (preview en vivo) =====================
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        [FromQuery] int granjaId,
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        CancellationToken ct)
    {
        if (granjaId <= 0) return BadRequest(new { mensaje = "granjaId requerido" });
        var granja = await _db.Granjas.AsNoTracking().FirstOrDefaultAsync(g => g.Id == granjaId, ct);
        if (granja == null) return NotFound(new { mensaje = "Granja no encontrada" });
        if (hasta < desde) return BadRequest(new { mensaje = "Rango de fechas invalido" });

        // Normalizar hasta -> fin del dia
        hasta = hasta.Date.AddDays(1).AddTicks(-1);

        var resultado = await _eval.CalcularAsync(granjaId, desde.Date, hasta, ct);
        return Ok(resultado);
    }

    // ===================== 1b. CONSOLIDADO (todas las granjas) =====================
    [HttpGet("consolidado")]
    public async Task<IActionResult> Consolidado(
        [FromQuery] DateTime desde,
        [FromQuery] DateTime hasta,
        CancellationToken ct)
    {
        if (hasta < desde) return BadRequest(new { mensaje = "Rango de fechas invalido" });
        hasta = hasta.Date.AddDays(1).AddTicks(-1);
        var resultado = await _eval.CalcularConsolidadoAsync(desde.Date, hasta, ct);
        return Ok(resultado);
    }

    // ===================== 2. GENERAR INFORME =====================
    [HttpPost]
    public async Task<IActionResult> Generar([FromBody] GenerarInformeRequest req, CancellationToken ct)
    {
        var granja = await _db.Granjas.AsNoTracking().FirstOrDefaultAsync(g => g.Id == req.GranjaId, ct);
        if (granja == null) return NotFound(new { mensaje = "Granja no encontrada" });
        if (req.Hasta < req.Desde) return BadRequest(new { mensaje = "Rango de fechas invalido" });

        var desde = req.Desde.Date;
        var hasta = req.Hasta.Date.AddDays(1).AddTicks(-1);

        var resultado = await _eval.CalcularAsync(req.GranjaId, desde, hasta, ct);
        if (resultado.SinDatos || resultado.TotalEvaluados == 0)
            return BadRequest(new { mensaje = "No hay registros finalizados en el periodo seleccionado. No se puede generar un informe vacio." });

        // Overrides del MV: conclusiones/recomendaciones editadas o agregadas (notas del doctor).
        var concl = SanearLista(req.Conclusiones);
        if (concl != null) resultado.Conclusiones = concl;
        var recom = SanearLista(req.Recomendaciones);
        if (recom != null) resultado.Recomendaciones = recom;

        var consecutivo = await GenerarConsecutivoAsync(ct);
        var etiqueta = string.IsNullOrWhiteSpace(req.PeriodoEtiqueta)
            ? $"{desde:dd MMM} - {hasta:dd MMM yyyy}"
            : req.PeriodoEtiqueta;

        var informe = new InformeEvaluacion
        {
            Consecutivo = consecutivo,
            GranjaId = req.GranjaId,
            PeriodoDesde = desde,
            PeriodoHasta = hasta,
            PeriodoEtiqueta = etiqueta,
            ResponsableTecnico = req.ResponsableTecnico,
            Cliente = string.IsNullOrWhiteSpace(req.Cliente) ? granja.Propietario : req.Cliente,
            Estado = "GENERADO",
            FechaGeneracion = DateTime.UtcNow,
            UsuarioGeneracion = User.Identity?.Name,
            TotalEvaluados = resultado.TotalEvaluados,
            PrevalenciaPct = resultado.PrevalenciaPct,
            ConsolidacionPct = resultado.ConsolidacionPct,
            Idn = resultado.Idn,
            NivelRiesgo = resultado.NivelRiesgo,
            SnapshotJson = JsonSerializer.Serialize(resultado, JsonOpts),
        };

        // Generar PDF y cachear
        try
        {
            var pdfBytes = _pdf.Generar(informe, resultado, granja);
            informe.PdfBinario = pdfBytes;
            informe.PdfPesoBytes = pdfBytes.Length;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error generando PDF de informe {Consecutivo}", consecutivo);
            return StatusCode(500, new { mensaje = "Error generando el PDF del informe", detalle = ex.Message });
        }

        _db.InformesEvaluacion.Add(informe);
        await _db.SaveChangesAsync(ct);

        return Ok(MapList(informe, granja));
    }

    // ===================== 3. DESCARGAR / VER PDF =====================
    [HttpGet("{id:guid}/pdf")]
    public async Task<IActionResult> DescargarPdf(Guid id, [FromQuery] bool inline, CancellationToken ct)
    {
        var informe = await _db.InformesEvaluacion.Include(i => i.Granja).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (informe == null) return NotFound(new { mensaje = "Informe no encontrado" });

        var pdf = informe.PdfBinario;
        if (pdf == null || pdf.Length == 0)
        {
            // Regenerar bajo demanda desde snapshot
            var resultado = JsonSerializer.Deserialize<EvaluacionResultado>(informe.SnapshotJson, JsonOpts);
            if (resultado == null) return StatusCode(500, new { mensaje = "Snapshot corrupto" });
            pdf = _pdf.Generar(informe, resultado, informe.Granja!);
            informe.PdfBinario = pdf;
            informe.PdfPesoBytes = pdf.Length;
            await _db.SaveChangesAsync(ct);
        }

        var nombre = NombreArchivo(informe);
        if (inline)
        {
            // Content-Disposition: inline -> el navegador lo muestra en un <iframe> (vista previa)
            Response.Headers.ContentDisposition = $"inline; filename=\"{nombre}\"";
            return File(pdf, "application/pdf");
        }
        return File(pdf, "application/pdf", nombre);
    }

    // ===================== 4. ENVIAR POR CORREO =====================
    [HttpPost("{id:guid}/enviar")]
    public async Task<IActionResult> Enviar(Guid id, [FromBody] EnviarInformeRequest req, CancellationToken ct)
    {
        var informe = await _db.InformesEvaluacion.Include(i => i.Granja).FirstOrDefaultAsync(i => i.Id == id, ct);
        if (informe == null) return NotFound(new { mensaje = "Informe no encontrado" });

        var destinatarios = (req.Destinatarios ?? new()).Select(d => d.Trim().ToLowerInvariant())
            .Where(d => d.Length > 0).Distinct().ToList();
        if (destinatarios.Count == 0)
            return BadRequest(new { mensaje = "Agrega al menos un destinatario" });
        var invalidos = destinatarios.Where(d => !Email.EsValido(d)).ToList();
        if (invalidos.Count > 0)
            return BadRequest(new { mensaje = $"Correos invalidos: {string.Join(", ", invalidos)}" });

        // Asegurar PDF
        var pdf = informe.PdfBinario;
        if (pdf == null || pdf.Length == 0)
        {
            var resultado = JsonSerializer.Deserialize<EvaluacionResultado>(informe.SnapshotJson, JsonOpts);
            if (resultado == null) return StatusCode(500, new { mensaje = "Snapshot corrupto" });
            pdf = _pdf.Generar(informe, resultado, informe.Granja!);
            informe.PdfBinario = pdf;
            informe.PdfPesoBytes = pdf.Length;
        }

        var asunto = string.IsNullOrWhiteSpace(req.Asunto)
            ? $"Informe de Evaluación Pulmonar · {informe.Granja!.Nombre} · {informe.PeriodoEtiqueta}"
            : req.Asunto;
        var mensaje = string.IsNullOrWhiteSpace(req.Mensaje)
            ? $"Estimados,\n\nAdjunto encontrarán el informe de evaluación pulmonar correspondiente a {informe.Granja!.Nombre}, período {informe.PeriodoEtiqueta}. El documento consolida los hallazgos de inspección sobre la totalidad de animales registrados para la granja en el período.\n\nQuedamos atentos a cualquier consulta.\n\nSaludos cordiales."
            : req.Mensaje;

        var cuerpoHtml = ConstruirHtml(informe, mensaje);
        var nombre = NombreArchivo(informe);

        var envio = new InformeEnvio
        {
            InformeId = informe.Id,
            Fecha = DateTime.UtcNow,
            UsuarioEnvio = User.Identity?.Name,
            DestinatariosJson = JsonSerializer.Serialize(destinatarios),
            Asunto = asunto,
        };

        try
        {
            await _email.EnviarAsync(
                destinatarios, asunto, cuerpoHtml,
                new List<(string, byte[], string)> { (nombre, pdf, "application/pdf") }, ct);

            envio.Estado = "OK";
            informe.Estado = "ENVIADO";
            informe.FechaUltimoEnvio = DateTime.UtcNow;
            informe.CantidadEnvios += 1;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error enviando informe {Consecutivo}", informe.Consecutivo);
            envio.Estado = "ERROR";
            envio.ErrorMensaje = ex.Message;
            _db.InformeEnvios.Add(envio);
            await _db.SaveChangesAsync(ct);
            return StatusCode(502, new { mensaje = "No se pudo enviar el correo", detalle = ex.Message });
        }

        _db.InformeEnvios.Add(envio);
        await _db.SaveChangesAsync(ct);

        return Ok(new { mensaje = $"Informe enviado a {destinatarios.Count} destinatario(s)", destinatarios });
    }

    // ===================== 5. HISTORIAL =====================
    [HttpGet]
    public async Task<IActionResult> Historial(
        [FromQuery] int? granjaId = null,
        [FromQuery] string? estado = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken ct = default)
    {
        var q = _db.InformesEvaluacion.AsNoTracking().Include(i => i.Granja).AsQueryable();
        if (granjaId.HasValue) q = q.Where(i => i.GranjaId == granjaId);
        if (!string.IsNullOrWhiteSpace(estado)) q = q.Where(i => i.Estado == estado.ToUpperInvariant());

        var list = await q
            .OrderByDescending(i => i.FechaGeneracion)
            .Skip(skip).Take(Math.Clamp(take, 1, 200))
            .Select(i => new InformeListDto(
                i.Id, i.Consecutivo, i.GranjaId,
                i.Granja != null ? i.Granja.Codigo : null,
                i.Granja != null ? i.Granja.Nombre : null,
                i.PeriodoEtiqueta, i.PeriodoDesde, i.PeriodoHasta,
                i.Estado, i.NivelRiesgo, i.TotalEvaluados,
                i.PrevalenciaPct, i.ConsolidacionPct, i.Idn,
                i.FechaGeneracion, i.UsuarioGeneracion, i.FechaUltimoEnvio, i.CantidadEnvios))
            .ToListAsync(ct);
        return Ok(list);
    }

    // ===================== 6. DETALLE =====================
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detalle(Guid id, CancellationToken ct)
    {
        var informe = await _db.InformesEvaluacion.AsNoTracking()
            .Include(i => i.Granja)
            .Include(i => i.Envios)
            .FirstOrDefaultAsync(i => i.Id == id, ct);
        if (informe == null) return NotFound(new { mensaje = "Informe no encontrado" });

        var resultado = JsonSerializer.Deserialize<EvaluacionResultado>(informe.SnapshotJson, JsonOpts);
        var envios = informe.Envios.OrderByDescending(e => e.Fecha).Select(e => new InformeEnvioDto(
            e.Id, e.Fecha, e.UsuarioEnvio,
            JsonSerializer.Deserialize<List<string>>(e.DestinatariosJson) ?? new(),
            e.Asunto, e.Estado, e.ErrorMensaje)).ToList();

        return Ok(new
        {
            meta = MapList(informe, informe.Granja!),
            resultado,
            envios
        });
    }

    // ===================== 7. ANULAR =====================
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Anular(Guid id, CancellationToken ct)
    {
        var informe = await _db.InformesEvaluacion.FirstOrDefaultAsync(i => i.Id == id, ct);
        if (informe == null) return NotFound(new { mensaje = "Informe no encontrado" });
        informe.Estado = "ANULADO";
        await _db.SaveChangesAsync(ct);
        return Ok(new { mensaje = "Informe anulado" });
    }

    // ===================== HELPERS =====================
    private async Task<string> GenerarConsecutivoAsync(CancellationToken ct)
    {
        var anio = DateTime.UtcNow.Year;
        var prefijo = $"IEP-{anio}-";
        var ultimos = await _db.InformesEvaluacion
            .Where(i => i.Consecutivo.StartsWith(prefijo))
            .Select(i => i.Consecutivo)
            .ToListAsync(ct);
        var maxNum = ultimos
            .Select(c => int.TryParse(c[prefijo.Length..], out var n) ? n : 0)
            .DefaultIfEmpty(0).Max();
        return $"{prefijo}{(maxNum + 1):D5}";
    }

    /// <summary>Limpia una lista editada por el usuario: recorta, descarta vacíos y acota largo/cantidad
    /// para que el PDF no se desborde. Devuelve null si no se envió override (no toca lo autogenerado).</summary>
    private static List<string>? SanearLista(List<string>? items)
    {
        if (items == null) return null; // no hay override
        var limpios = items
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim().Length > 600 ? s.Trim()[..600] : s.Trim())
            .Take(30)
            .ToList();
        return limpios; // lista vacía = el usuario quitó todo (válido)
    }

    private static string NombreArchivo(InformeEvaluacion i)
    {
        var granja = Sanear(i.Granja?.Nombre ?? "Granja");
        var periodo = Sanear(i.PeriodoEtiqueta ?? i.PeriodoDesde.ToString("yyyy-MM"));
        return $"Informe_Evaluacion_Pulmonar_{granja}_{periodo}.pdf";
    }

    private static string Sanear(string s)
    {
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        var limpio = new string(s.Where(c => !invalid.Contains(c)).ToArray());
        limpio = limpio.Replace(' ', '_').Replace('-', '_');
        // quitar acentos basicos
        limpio = limpio
            .Replace('á', 'a').Replace('é', 'e').Replace('í', 'i').Replace('ó', 'o').Replace('ú', 'u')
            .Replace('Á', 'A').Replace('É', 'E').Replace('Í', 'I').Replace('Ó', 'O').Replace('Ú', 'U')
            .Replace('ñ', 'n').Replace('Ñ', 'N');
        while (limpio.Contains("__")) limpio = limpio.Replace("__", "_");
        return limpio.Trim('_');
    }

    private static InformeListDto MapList(InformeEvaluacion i, Granja g) => new(
        i.Id, i.Consecutivo, i.GranjaId, g.Codigo, g.Nombre,
        i.PeriodoEtiqueta, i.PeriodoDesde, i.PeriodoHasta,
        i.Estado, i.NivelRiesgo, i.TotalEvaluados,
        i.PrevalenciaPct, i.ConsolidacionPct, i.Idn,
        i.FechaGeneracion, i.UsuarioGeneracion, i.FechaUltimoEnvio, i.CantidadEnvios);

    private static string ConstruirHtml(InformeEvaluacion i, string mensaje)
    {
        var (riesgoBg, riesgoBorde, label, riesgoTxt) = i.NivelRiesgo switch
        {
            "CRITICO" => ("#b91c1c", "#7f1d1d", "CRÍTICO", "Requiere intervención inmediata del equipo técnico-sanitario."),
            "ALTO"    => ("#c2410c", "#9a3412", "ALTO",    "Atención prioritaria sobre el manejo sanitario del lote."),
            "MEDIO"   => ("#b45309", "#92400e", "MEDIO",   "Vigilancia recomendada y seguimiento durante el período."),
            _         => ("#047857", "#065f46", "BAJO",    "Condición sanitaria dentro de parámetros aceptables.")
        };

        string Enc(string? s) => System.Net.WebUtility.HtmlEncode(s ?? "—");
        var msgHtml = System.Net.WebUtility.HtmlEncode(mensaje).Replace("\n", "<br/>");
        var granja  = Enc(i.Granja?.Nombre);
        var codigo  = string.IsNullOrWhiteSpace(i.Granja?.Codigo) ? "" : $"Código {Enc(i.Granja!.Codigo)}";
        var periodo = Enc(i.PeriodoEtiqueta);
        var fecha   = i.FechaGeneracion.ToLocalTime().ToString("dd/MM/yyyy · HH:mm");

        // Tarjeta KPI reutilizable (tabla anidada, compatible Outlook)
        string Kpi(string etiqueta, string valor, string unidad, string bg, string borde, string acento) => $@"
        <td width='50%' valign='top' style='padding:6px'>
          <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background:{bg};border:1px solid {borde};border-radius:12px'>
            <tr><td style='padding:14px 16px'>
              <div style='font-family:Segoe UI,Arial,sans-serif;font-size:11px;font-weight:700;color:{acento};letter-spacing:.6px;text-transform:uppercase'>{etiqueta}</div>
              <div style='font-family:Segoe UI,Arial,sans-serif;font-size:27px;font-weight:800;color:#0f172a;line-height:1.05;margin-top:6px'>{valor}<span style='font-size:14px;font-weight:700;color:#64748b'>{unidad}</span></div>
            </td></tr>
          </table>
        </td>";

        return $@"<!DOCTYPE html>
<html lang='es'><head><meta charset='utf-8'/><meta name='viewport' content='width=device-width,initial-scale=1'/></head>
<body style='margin:0;padding:0;background:#eef2f5'>
<table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background:#eef2f5'>
<tr><td align='center' style='padding:28px 14px'>
  <table role='presentation' width='600' cellpadding='0' cellspacing='0' border='0' style='width:600px;max-width:600px;background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #e2e8f0'>

    <!-- Encabezado -->
    <tr><td bgcolor='#0f766e' style='background:#0f766e;padding:26px 28px'>
      <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'><tr>
        <td valign='middle' width='52'>
          <table role='presentation' cellpadding='0' cellspacing='0' border='0'><tr>
            <td align='center' valign='middle' width='48' height='48' bgcolor='#ffffff' style='width:48px;height:48px;background:#ffffff;border-radius:12px;font-family:Lexend,Segoe UI,Arial,sans-serif;font-size:22px;font-weight:800;color:#0f766e'>R</td>
          </tr></table>
        </td>
        <td valign='middle' style='padding-left:14px'>
          <div style='font-family:Segoe UI,Arial,sans-serif;font-size:11px;font-weight:700;color:#99f6e4;letter-spacing:1.5px'>REPAGRO &nbsp;·&nbsp; RASTREO</div>
          <div style='font-family:Lexend,Segoe UI,Arial,sans-serif;font-size:21px;font-weight:800;color:#ffffff;margin-top:3px;line-height:1.2'>Informe de Evaluación Pulmonar</div>
        </td>
      </tr></table>
    </td></tr>

    <!-- Sub-banda granja/periodo -->
    <tr><td bgcolor='#0d5d56' style='background:#0d5d56;padding:12px 28px'>
      <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'><tr>
        <td style='font-family:Segoe UI,Arial,sans-serif;font-size:14px;font-weight:700;color:#ffffff'>{granja}</td>
        <td align='right' style='font-family:Segoe UI,Arial,sans-serif;font-size:13px;font-weight:600;color:#a7f3d0'>{periodo}</td>
      </tr></table>
    </td></tr>

    <!-- Cuerpo -->
    <tr><td style='padding:26px 28px 8px'>
      <p style='margin:0 0 20px;font-family:Segoe UI,Arial,sans-serif;font-size:14px;line-height:1.65;color:#334155'>{msgHtml}</p>

      <div style='font-family:Segoe UI,Arial,sans-serif;font-size:11px;font-weight:700;color:#94a3b8;letter-spacing:.6px;text-transform:uppercase;margin:0 6px 6px'>Indicadores clave</div>
      <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'>
        <tr>
          {Kpi("Animales evaluados", i.TotalEvaluados.ToString("N0"), "", "#f8fafc", "#e2e8f0", "#475569")}
          {Kpi("Prevalencia", i.PrevalenciaPct.ToString("F1"), "%", "#f0fdfa", "#ccfbf1", "#0f766e")}
        </tr>
        <tr>
          {Kpi("Consolidación", i.ConsolidacionPct.ToString("F1"), "%", "#f0fdfa", "#ccfbf1", "#0f766e")}
          {Kpi("Índice IDN", i.Idn.ToString("F2"), "", "#f8fafc", "#e2e8f0", "#475569")}
        </tr>
      </table>

      <!-- Banner de riesgo -->
      <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='margin:14px 0 8px'>
        <tr><td bgcolor='{riesgoBg}' style='background:{riesgoBg};border:1px solid {riesgoBorde};border-radius:12px;padding:16px 18px'>
          <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0'><tr>
            <td style='font-family:Segoe UI,Arial,sans-serif;font-size:11px;font-weight:700;color:#ffffff;letter-spacing:1px;text-transform:uppercase'>Nivel de riesgo sanitario</td>
            <td align='right' style='font-family:Lexend,Segoe UI,Arial,sans-serif;font-size:20px;font-weight:800;color:#ffffff'>{label}</td>
          </tr></table>
          <div style='font-family:Segoe UI,Arial,sans-serif;font-size:12.5px;color:#f1f5f9;margin-top:6px;line-height:1.5'>{riesgoTxt}</div>
        </td></tr>
      </table>
    </td></tr>

    <!-- Detalle del documento -->
    <tr><td style='padding:8px 28px 4px'>
      <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='border:1px solid #e2e8f0;border-radius:12px'>
        <tr>
          <td style='padding:12px 16px;font-family:Segoe UI,Arial,sans-serif;font-size:12.5px;color:#64748b;border-bottom:1px solid #f1f5f9'>Granja</td>
          <td align='right' style='padding:12px 16px;font-family:Segoe UI,Arial,sans-serif;font-size:12.5px;font-weight:600;color:#0f172a;border-bottom:1px solid #f1f5f9'>{granja}{(codigo.Length > 0 ? $" <span style='color:#94a3b8;font-weight:400'>· {codigo}</span>" : "")}</td>
        </tr>
        <tr>
          <td style='padding:12px 16px;font-family:Segoe UI,Arial,sans-serif;font-size:12.5px;color:#64748b;border-bottom:1px solid #f1f5f9'>Período</td>
          <td align='right' style='padding:12px 16px;font-family:Segoe UI,Arial,sans-serif;font-size:12.5px;font-weight:600;color:#0f172a;border-bottom:1px solid #f1f5f9'>{periodo}</td>
        </tr>
        <tr>
          <td style='padding:12px 16px;font-family:Segoe UI,Arial,sans-serif;font-size:12.5px;color:#64748b;border-bottom:1px solid #f1f5f9'>Consecutivo</td>
          <td align='right' style='padding:12px 16px;font-family:Segoe UI,Arial,sans-serif;font-size:12.5px;font-weight:700;color:#0f766e;border-bottom:1px solid #f1f5f9'>{i.Consecutivo}</td>
        </tr>
        <tr>
          <td style='padding:12px 16px;font-family:Segoe UI,Arial,sans-serif;font-size:12.5px;color:#64748b'>Generado</td>
          <td align='right' style='padding:12px 16px;font-family:Segoe UI,Arial,sans-serif;font-size:12.5px;font-weight:600;color:#0f172a'>{fecha}</td>
        </tr>
      </table>
    </td></tr>

    <!-- Adjunto -->
    <tr><td style='padding:16px 28px 22px'>
      <table role='presentation' width='100%' cellpadding='0' cellspacing='0' border='0' style='background:#f0fdfa;border:1px dashed #5eead4;border-radius:12px'>
        <tr>
          <td valign='middle' width='46' style='padding:14px 0 14px 16px'>
            <table role='presentation' cellpadding='0' cellspacing='0' border='0'><tr>
              <td align='center' valign='middle' width='38' height='38' bgcolor='#0f766e' style='width:38px;height:38px;background:#0f766e;border-radius:9px;font-family:Segoe UI,Arial,sans-serif;font-size:10px;font-weight:800;color:#ffffff'>PDF</td>
            </tr></table>
          </td>
          <td valign='middle' style='padding:14px 16px'>
            <div style='font-family:Segoe UI,Arial,sans-serif;font-size:13px;font-weight:700;color:#0f172a'>Informe completo adjunto</div>
            <div style='font-family:Segoe UI,Arial,sans-serif;font-size:12px;color:#64748b;margin-top:2px'>Documento PDF con gráficos, análisis e interpretación clínica detallada.</div>
          </td>
        </tr>
      </table>
    </td></tr>

    <!-- Pie -->
    <tr><td bgcolor='#0f172a' style='background:#0f172a;padding:18px 28px'>
      <div style='font-family:Segoe UI,Arial,sans-serif;font-size:12px;font-weight:700;color:#ffffff'>REPAGRO · RASTREO</div>
      <div style='font-family:Segoe UI,Arial,sans-serif;font-size:11px;color:#94a3b8;margin-top:4px;line-height:1.6'>Documento generado automáticamente por el sistema de evaluación pulmonar. Este correo y su archivo adjunto contienen información confidencial de uso interno.</div>
    </td></tr>

  </table>
</td></tr>
</table>
</body></html>";
    }
}

/// <summary>Validador de correo simple sin regex estatica costosa.</summary>
internal sealed class Regex_
{
    private static readonly System.Text.RegularExpressions.Regex Re =
        new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", System.Text.RegularExpressions.RegexOptions.Compiled);
    public bool EsValido(string s) => Re.IsMatch(s);
}
