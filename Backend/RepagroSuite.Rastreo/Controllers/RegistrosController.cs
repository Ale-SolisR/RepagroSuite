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
[Route("api/registros")]
public class RegistrosController : ControllerBase
{
    private readonly RastreoDbContext _db;
    private readonly ExcelService _excel;
    private readonly PdfService _pdf;
    private readonly EmailService _email;
    private readonly ILogger<RegistrosController> _log;

    public RegistrosController(RastreoDbContext db, ExcelService excel, PdfService pdf, EmailService email, ILogger<RegistrosController> log)
    {
        _db = db;
        _excel = excel;
        _pdf = pdf;
        _email = email;
        _log = log;
    }

    // ===================== AISLAMIENTO POR USUARIO =====================
    private int CurrentUserId => int.TryParse(User.FindFirst("uid")?.Value, out var id) ? id : 0;
    private bool EsAdmin => User.FindFirst("rol")?.Value == "ADMIN";
    /// <summary>Admin accede a todo; el resto solo a lo suyo.</summary>
    private bool PuedeAcceder(Registro r) => EsAdmin || r.UsuarioId == CurrentUserId;

    // ===================== LISTAR / OBTENER =====================

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? estado = null,
        [FromQuery] int? granjaId = null,
        [FromQuery] string? lote = null,
        [FromQuery] DateTime? desde = null,
        [FromQuery] DateTime? hasta = null,
        [FromQuery] int? usuarioId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50)
    {
        var query = _db.Registros.AsNoTracking().Include(r => r.Granja).Include(r => r.Usuario).AsQueryable();

        // Aislamiento: no-admin solo ve lo suyo; admin puede filtrar por usuario.
        if (!EsAdmin) query = query.Where(r => r.UsuarioId == CurrentUserId);
        else if (usuarioId.HasValue) query = query.Where(r => r.UsuarioId == usuarioId);

        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(r => r.Estado == estado.ToUpperInvariant());
        if (granjaId.HasValue) query = query.Where(r => r.GranjaId == granjaId);
        if (!string.IsNullOrWhiteSpace(lote)) query = query.Where(r => r.Lote != null && EF.Functions.Like(r.Lote, $"%{lote}%"));
        if (desde.HasValue) query = query.Where(r => r.FechaCreacion >= desde);
        if (hasta.HasValue) query = query.Where(r => r.FechaCreacion <= hasta);

        var list = await query
            .OrderByDescending(r => r.FechaCreacion)
            .Skip(skip).Take(Math.Clamp(take, 1, 200))
            .Select(r => new RegistroListDto(
                r.Id, r.FechaCreacion, r.GranjaId,
                r.Granja != null ? r.Granja.Codigo : null,
                r.Granja != null ? r.Granja.Nombre : null,
                r.Lote, r.Estado, r.Detalles.Count, r.FechaUltimaModificacion,
                r.UsuarioId, r.Usuario != null ? r.Usuario.Nombre : r.UsuarioCreacion))
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("pendientes")]
    public async Task<IActionResult> Pendientes()
    {
        var query = _db.Registros.AsNoTracking().Include(r => r.Granja).Include(r => r.Usuario)
            .Where(r => r.Estado == "BORRADOR" || r.Estado == "EN_PROCESO");
        if (!EsAdmin) query = query.Where(r => r.UsuarioId == CurrentUserId);

        var list = await query
            .OrderByDescending(r => r.FechaUltimaModificacion ?? r.FechaCreacion)
            .Select(r => new RegistroListDto(
                r.Id, r.FechaCreacion, r.GranjaId,
                r.Granja != null ? r.Granja.Codigo : null,
                r.Granja != null ? r.Granja.Nombre : null,
                r.Lote, r.Estado, r.Detalles.Count, r.FechaUltimaModificacion,
                r.UsuarioId, r.Usuario != null ? r.Usuario.Nombre : r.UsuarioCreacion))
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var r = await CargarCompleto(id, default);
        if (r is null || !PuedeAcceder(r)) return NotFound();
        return Ok(MapToDetail(r));
    }

    // ===================== UPSERT IDEMPOTENTES =====================

    /// <summary>Upsert registro por Id (UUID generado por el cliente). Crea si no existe; actualiza si existe.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpsertRegistro(Guid id, [FromBody] RegistroUpsertDto dto, CancellationToken ct)
    {
        var r = await _db.Registros
            .Include(x => x.Vacunas)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        var ahora = DateTime.UtcNow;
        var esNuevo = false;

        if (r is null)
        {
            esNuevo = true;
            r = new Registro
            {
                Id = id,
                FechaCreacion = dto.FechaCreacion ?? ahora,
                FechaInicio = dto.FechaCreacion ?? ahora,
                Estado = "BORRADOR",
                UsuarioCreacion = User.Identity?.Name,
                UsuarioId = CurrentUserId
            };
            _db.Registros.Add(r);
        }
        else if (!PuedeAcceder(r))
        {
            return NotFound();
        }
        if (r.Estado == "FINALIZADO" || r.Estado == "ANULADO")
            return Conflict(new { mensaje = "Registro ya finalizado/anulado" });

        if (dto.GranjaId.HasValue) r.GranjaId = dto.GranjaId;
        if (dto.Lote != null) r.Lote = dto.Lote;
        if (dto.Observaciones != null) r.Observaciones = dto.Observaciones;
        r.UsaVacunas = dto.UsaVacunas;
        await AplicarVacunasRegistro(r, dto.VacunaIds, ct);
        if (dto.PasoActual != null) r.PasoActual = dto.PasoActual;
        if (dto.DetalleActualId.HasValue) r.DetalleActualId = dto.DetalleActualId;

        if (r.Estado == "BORRADOR" && r.GranjaId.HasValue) r.Estado = "EN_PROCESO";

        r.FechaUltimaModificacion = ahora;
        r.UsuarioUltimaModificacion = User.Identity?.Name;
        await _db.SaveChangesAsync(ct);

        var reg = await CargarCompleto(id, ct);
        return esNuevo ? CreatedAtAction(nameof(Get), new { id }, MapToDetail(reg!)) : Ok(MapToDetail(reg!));
    }

    /// <summary>Upsert detalle por Id (UUID del cliente).</summary>
    [HttpPut("{registroId:guid}/detalles/{detalleId:guid}")]
    public async Task<IActionResult> UpsertDetalle(Guid registroId, Guid detalleId,
        [FromBody] DetalleUpsertDto dto, CancellationToken ct)
    {
        var registro = await _db.Registros
            .Include(r => r.Detalles)
                .ThenInclude(d => d.EnfermedadesValores)
            .FirstOrDefaultAsync(r => r.Id == registroId, ct);
        if (registro is null || !PuedeAcceder(registro)) return NotFound(new { mensaje = "Registro no existe" });
        if (registro.Estado == "FINALIZADO" || registro.Estado == "ANULADO")
            return Conflict(new { mensaje = "Registro no editable" });

        var d = registro.Detalles.FirstOrDefault(x => x.Id == detalleId);
        var esNuevo = false;
        if (d is null)
        {
            esNuevo = true;
            d = new RegistroDetalle
            {
                Id = detalleId,
                RegistroId = registroId,
                NumeroLinea = dto.NumeroLinea ??
                              (registro.Detalles.Count == 0 ? 1 : registro.Detalles.Max(x => x.NumeroLinea) + 1),
                FechaCreacion = dto.FechaCreacion ?? DateTime.UtcNow
            };
            _db.RegistroDetalles.Add(d);
        }

        if (dto.NumeroLinea.HasValue) d.NumeroLinea = dto.NumeroLinea.Value;
        d.ApicalIzquierdo = Clamp(dto.ApicalIzquierdo);
        d.CardiacoIzquierdo = Clamp(dto.CardiacoIzquierdo);
        d.DiafragmaticoIzquierdo = Clamp(dto.DiafragmaticoIzquierdo);
        d.ApicalDerecho = Clamp(dto.ApicalDerecho);
        d.CardiacoDerecho = Clamp(dto.CardiacoDerecho);
        d.DiafragmaticoDerecho = Clamp(dto.DiafragmaticoDerecho);
        d.Accesorio = Clamp(dto.Accesorio);
        d.SPES = Clamp(dto.SPES);
        d.AbscesoNodulo = dto.AbscesoNodulo;
        d.PericardioEngrosado = dto.PericardioEngrosado;
        d.Pericarditis = dto.Pericarditis;
        d.AgudaCronica = NormalizarAC(dto.AgudaCronica);
        d.Moco = dto.Moco;
        await AplicarEnfermedadesDinamicas(d, dto.EnfermedadesValores, ct);
        if (dto.PasoActual != null) d.PasoActual = dto.PasoActual;
        if (dto.Estado != null) d.Estado = dto.Estado;
        d.FechaUltimaModificacion = DateTime.UtcNow;

        registro.FechaUltimaModificacion = DateTime.UtcNow;
        registro.UsuarioUltimaModificacion = User.Identity?.Name;
        if (registro.Estado == "BORRADOR") registro.Estado = "EN_PROCESO";

        await _db.SaveChangesAsync(ct);

        // Recargar con fotos
        var reloaded = await _db.RegistroDetalles
            .Include(x => x.Fotos)
            .Include(x => x.EnfermedadesValores)
                .ThenInclude(v => v.Enfermedad)
            .AsNoTracking()
            .FirstAsync(x => x.Id == detalleId, ct);
        return esNuevo ? StatusCode(201, MapDetalle(reloaded)) : Ok(MapDetalle(reloaded));
    }

    // ===================== TRANSICIONES DE ESTADO =====================

    [HttpPatch("{id:guid}/finalizar")]
    public async Task<IActionResult> Finalizar(Guid id, CancellationToken ct)
    {
        var r = await _db.Registros.Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null || !PuedeAcceder(r)) return NotFound();
        if (r.GranjaId is null) return BadRequest(new { mensaje = "Falta seleccionar granja" });
        if (r.Detalles.Count == 0) return BadRequest(new { mensaje = "Debe agregar al menos una línea" });
        r.Estado = "FINALIZADO";
        r.FechaFinalizacion = DateTime.UtcNow;
        r.FechaUltimaModificacion = DateTime.UtcNow;
        r.UsuarioUltimaModificacion = User.Identity?.Name;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/anular")]
    public async Task<IActionResult> Anular(Guid id, CancellationToken ct)
    {
        var r = await _db.Registros.FindAsync(new object?[] { id }, ct);
        if (r is null || !PuedeAcceder(r)) return NotFound();
        r.Estado = "ANULADO";
        r.FechaUltimaModificacion = DateTime.UtcNow;
        r.UsuarioUltimaModificacion = User.Identity?.Name;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var r = await _db.Registros.Include(x => x.Detalles).FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null || !PuedeAcceder(r)) return NotFound();
        if (r.Estado == "FINALIZADO")
            return Conflict(new { mensaje = "No se pueden eliminar registros finalizados" });
        _db.Registros.Remove(r);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{registroId:guid}/detalles/{detalleId:guid}")]
    public async Task<IActionResult> EliminarDetalle(Guid registroId, Guid detalleId, CancellationToken ct)
    {
        var d = await _db.RegistroDetalles.Include(x => x.Registro)
            .FirstOrDefaultAsync(x => x.Id == detalleId && x.RegistroId == registroId, ct);
        if (d is null || d.Registro is null || !PuedeAcceder(d.Registro)) return NotFound();
        _db.RegistroDetalles.Remove(d);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // ===================== EXPORTAR =====================

    [HttpGet("{id:guid}/export/excel")]
    public async Task<IActionResult> ExportExcel(Guid id, CancellationToken ct)
    {
        var r = await CargarCompleto(id, ct);
        if (r is null || !PuedeAcceder(r)) return NotFound();
        var enfermedades = await EnfermedadesReporte(ct);
        var bytes = _excel.GenerarRegistro(r, enfermedades);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", NombreArchivo(r, "xlsx"));
    }

    [HttpGet("{id:guid}/export/pdf")]
    public async Task<IActionResult> ExportPdf(Guid id, CancellationToken ct)
    {
        var r = await CargarCompleto(id, ct);
        if (r is null || !PuedeAcceder(r)) return NotFound();
        var enfermedades = await EnfermedadesReporte(ct);
        var bytes = _pdf.GenerarRegistro(r, enfermedades);
        return File(bytes, "application/pdf", NombreArchivo(r, "pdf"));
    }

    [HttpPost("{id:guid}/email")]
    public async Task<IActionResult> EnviarPorCorreo(Guid id, [FromBody] EnviarCorreoDto dto, CancellationToken ct)
    {
        if (dto.Destinatarios == null || dto.Destinatarios.Count == 0)
            return BadRequest(new { mensaje = "Debe indicar al menos un destinatario" });
        if (!dto.IncluirExcel && !dto.IncluirPdf)
            return BadRequest(new { mensaje = "Debe incluir al menos Excel o PDF" });
        if (!_email.Configurado)
            return StatusCode(503, new { mensaje = "El envío de correo no está habilitado en el servidor (falta la contraseña de aplicación de Gmail). Descarga el Excel/PDF y compártelo manualmente." });

        var r = await CargarCompleto(id, ct);
        if (r is null || !PuedeAcceder(r)) return NotFound();

        var adjuntos = new List<(string nombre, byte[] contenido, string mime)>();
        if (dto.IncluirExcel)
        {
            var enfermedades = await EnfermedadesReporte(ct);
            adjuntos.Add((NombreArchivo(r, "xlsx"), _excel.GenerarRegistro(r, enfermedades),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
        }
        if (dto.IncluirPdf)
        {
            var enfermedades = await EnfermedadesReporte(ct);
            adjuntos.Add((NombreArchivo(r, "pdf"), _pdf.GenerarRegistro(r, enfermedades), "application/pdf"));
        }

        var asunto = dto.Asunto ?? $"Revisión de matanza - {r.Granja?.Nombre ?? "Sin granja"} - {r.FechaCreacion:dd-MM-yyyy}";
        var cuerpo = $@"
<p>{System.Net.WebUtility.HtmlEncode(dto.Mensaje ?? "Adjunto registro de revisión de matanza.")}</p>
<hr/>
<p><b>Fecha:</b> {r.FechaCreacion.ToLocalTime():dd-MM-yyyy HH:mm}<br/>
<b>Granja:</b> {System.Net.WebUtility.HtmlEncode(r.Granja?.Nombre ?? "Sin granja")}<br/>
<b>Lote:</b> {System.Net.WebUtility.HtmlEncode(r.Lote ?? "-")}<br/>
<b>Total cerdos:</b> {r.Detalles.Count}<br/>
<b>Estado:</b> {r.Estado}</p>";

        try
        {
            await _email.EnviarAsync(dto.Destinatarios, asunto, cuerpo, adjuntos, ct);
        }
        catch (Exception ex)
        {
            // No filtrar el detalle SMTP al cliente; registrar y devolver un mensaje accionable.
            _log.LogError(ex, "Fallo al enviar correo del registro {Id}", id);
            return StatusCode(502, new { mensaje = "No se pudo enviar el correo (el servidor de correo rechazó el envío). Verifica los destinatarios o descarga el Excel/PDF y compártelo manualmente." });
        }
        return Ok(new { mensaje = "Correo enviado", destinatarios = dto.Destinatarios });
    }

    // ===================== HELPERS =====================

    private async Task<Registro?> CargarCompleto(Guid id, CancellationToken ct) =>
        await _db.Registros.AsNoTracking()
            .Include(r => r.Granja)
            .Include(r => r.Usuario)
            .Include(r => r.Vacunas)
                .ThenInclude(rv => rv.Vacuna)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.Fotos)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.EnfermedadesValores)
                    .ThenInclude(v => v.Enfermedad)
            .AsSplitQuery()
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    private async Task<List<Enfermedad>> EnfermedadesReporte(CancellationToken ct) =>
        await _db.Enfermedades.AsNoTracking()
            .Where(e => e.Activo)
            .OrderBy(e => e.Orden).ThenBy(e => e.Nombre)
            .ToListAsync(ct);

    private static string NombreArchivo(Registro r, string ext)
    {
        var granja = (r.Granja?.Codigo ?? "SINGRANJA").Replace(' ', '_');
        var fecha = r.FechaCreacion.ToLocalTime().ToString("yyyyMMdd_HHmm");
        return $"rastreo_{granja}_{fecha}.{ext}";
    }

    private static byte Clamp(byte v) => v > 4 ? (byte)4 : v;

    private static string? NormalizarAC(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        var s = v.Trim().ToUpperInvariant();
        return s is "A" or "C" or "AC" ? s : null;
    }

    private async Task AplicarVacunasRegistro(Registro r, List<int>? vacunaIds, CancellationToken ct)
    {
        if (vacunaIds == null) return;

        var ids = r.UsaVacunas ? vacunaIds.Distinct().ToHashSet() : new HashSet<int>();
        if (ids.Count > 0)
        {
            var validas = await _db.Vacunas
                .Where(v => ids.Contains(v.Id) && v.Activo)
                .Select(v => v.Id)
                .ToListAsync(ct);
            ids = validas.ToHashSet();
        }

        r.Vacunas.RemoveAll(rv => !ids.Contains(rv.VacunaId));
        foreach (var id in ids)
        {
            if (!r.Vacunas.Any(rv => rv.VacunaId == id))
                r.Vacunas.Add(new RegistroVacuna { RegistroId = r.Id, VacunaId = id });
        }
    }

    private async Task AplicarEnfermedadesDinamicas(
        RegistroDetalle d,
        List<DetalleEnfermedadValorUpsertDto>? valores,
        CancellationToken ct)
    {
        if (valores == null) return;

        var ids = valores.Select(v => v.EnfermedadId).Distinct().ToList();
        var enfermedades = await _db.Enfermedades
            .Where(e => ids.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, ct);

        foreach (var dto in valores)
        {
            if (!enfermedades.TryGetValue(dto.EnfermedadId, out var enf)) continue;
            var existente = d.EnfermedadesValores.FirstOrDefault(v => v.EnfermedadId == dto.EnfermedadId);
            if (existente == null)
            {
                existente = new RegistroDetalleEnfermedadValor
                {
                    RegistroDetalleId = d.Id,
                    EnfermedadId = dto.EnfermedadId
                };
                d.EnfermedadesValores.Add(existente);
            }

            existente.ValorNumero = enf.TipoCampo == "SCORE_0_4"
                ? (byte?)Clamp(dto.ValorNumero ?? 0)
                : null;
            existente.ValorBooleano = enf.TipoCampo == "BOOL"
                ? (dto.ValorBooleano ?? false)
                : null;
            existente.ValorTexto = enf.TipoCampo == "AGUDA_CRONICA"
                ? NormalizarAC(dto.ValorTexto)
                : null;

            AplicarCompatibilidadColumnas(d, enf.Codigo, existente);
        }
    }

    private static void AplicarCompatibilidadColumnas(
        RegistroDetalle d,
        string codigo,
        RegistroDetalleEnfermedadValor valor)
    {
        switch (codigo)
        {
            case "SPES": d.SPES = Clamp(valor.ValorNumero ?? 0); break;
            case "ABSCESO_NODULO": d.AbscesoNodulo = valor.ValorBooleano ?? false; break;
            case "PERICARDIO_ENGROSADO": d.PericardioEngrosado = valor.ValorBooleano ?? false; break;
            case "PERICARDITIS": d.Pericarditis = valor.ValorBooleano ?? false; break;
            case "AGUDA_CRONICA": d.AgudaCronica = NormalizarAC(valor.ValorTexto); break;
            case "MOCO": d.Moco = valor.ValorBooleano ?? false; break;
        }
    }

    private static RegistroDetailDto MapToDetail(Registro r) => new(
        r.Id, r.FechaCreacion, r.FechaInicio, r.FechaFinalizacion,
        r.GranjaId, r.Granja?.Codigo, r.Granja?.Nombre, r.Lote, r.Observaciones,
        r.UsaVacunas,
        r.Vacunas
            .Where(rv => rv.Vacuna != null)
            .OrderBy(rv => rv.Vacuna!.Orden).ThenBy(rv => rv.Vacuna!.Nombre)
            .Select(rv => new RegistroVacunaDto(
                rv.VacunaId,
                rv.Vacuna!.Codigo,
                rv.Vacuna!.Nombre,
                rv.Vacuna!.Laboratorio,
                rv.Vacuna!.Orden
            )).ToList(),
        r.Estado, r.PasoActual, r.DetalleActualId, r.FechaUltimaModificacion,
        r.Detalles.OrderBy(d => d.NumeroLinea).Select(MapDetalle).ToList(),
        r.UsuarioId, r.Usuario?.Nombre ?? r.UsuarioCreacion);

    private static RegistroDetalleDto MapDetalle(RegistroDetalle d) => new(
        d.Id, d.ClienteId, d.NumeroLinea,
        d.ApicalIzquierdo, d.CardiacoIzquierdo, d.DiafragmaticoIzquierdo,
        d.ApicalDerecho, d.CardiacoDerecho, d.DiafragmaticoDerecho, d.Accesorio,
        d.SPES, d.AbscesoNodulo, d.PericardioEngrosado, d.Pericarditis,
        d.AgudaCronica, d.Moco,
        d.Fotos.OrderBy(f => f.Orden).Select(f => new FotoMetaDto(
            f.Id, f.Orden, f.FotoMimeType, f.FotoPesoBytes, f.FotoNombre,
            $"/api/detalles/{d.Id}/fotos/{f.Id}/binario", f.FechaCreacion
        )).ToList(),
        d.Estado, d.PasoActual, d.FechaCreacion, d.FechaUltimaModificacion,
        d.EnfermedadesValores
            .Where(v => v.Enfermedad != null)
            .OrderBy(v => v.Enfermedad!.Orden).ThenBy(v => v.Enfermedad!.Nombre)
            .Select(v => new RegistroDetalleEnfermedadValorDto(
                v.EnfermedadId,
                v.Enfermedad!.Codigo,
                v.Enfermedad!.Nombre,
                v.Enfermedad!.TipoCampo,
                v.Enfermedad!.Orden,
                v.ValorNumero,
                v.ValorBooleano,
                v.ValorTexto
            )).ToList());
}
