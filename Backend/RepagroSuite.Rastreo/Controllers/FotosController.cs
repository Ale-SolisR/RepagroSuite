using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Rastreo.Api.Data;
using Rastreo.Api.Dtos;
using Rastreo.Api.Models;
using Rastreo.Api.Services;

namespace Rastreo.Api.Controllers;

public class FotoUpsertForm
{
    public IFormFile File { get; set; } = default!;
    public byte Orden { get; set; }
}

[ApiController]
[Authorize(AuthenticationSchemes = "Rastreo")]
[Route("api/detalles/{detalleId:guid}/fotos")]
public class FotosController : ControllerBase
{
    private readonly RastreoDbContext _db;
    private readonly ImageService _img;

    public FotosController(RastreoDbContext db, ImageService img) { _db = db; _img = img; }

    private int CurrentUserId => int.TryParse(User.FindFirst("uid")?.Value, out var id) ? id : 0;
    private bool EsAdmin => User.FindFirst("rol")?.Value == "ADMIN";
    /// <summary>El detalle debe pertenecer a un registro del usuario actual (admin ve todo).</summary>
    private async Task<bool> PuedeAccederDetalle(Guid detalleId, CancellationToken ct)
    {
        if (EsAdmin) return true;
        var ownerId = await _db.RegistroDetalles.AsNoTracking()
            .Where(d => d.Id == detalleId)
            .Select(d => d.Registro!.UsuarioId)
            .FirstOrDefaultAsync(ct);
        return ownerId == CurrentUserId;
    }

    /// <summary>Lista metadata de las fotos del detalle (sin binario).</summary>
    [HttpGet]
    public async Task<IActionResult> List(Guid detalleId, CancellationToken ct)
    {
        if (!await PuedeAccederDetalle(detalleId, ct)) return NotFound();
        var fotos = await _db.RegistroDetalleFotos.AsNoTracking()
            .Where(f => f.RegistroDetalleId == detalleId)
            .OrderBy(f => f.Orden)
            .Select(f => new FotoMetaDto(
                f.Id, f.Orden, f.FotoMimeType, f.FotoPesoBytes, f.FotoNombre,
                $"/api/detalles/{detalleId}/fotos/{f.Id}/binario", f.FechaCreacion))
            .ToListAsync(ct);
        return Ok(fotos);
    }

    /// <summary>
    /// Upsert idempotente de una foto. El cliente envía fotoId (UUID) y orden (1–3).
    /// Si una foto con ese orden ya existe (y es otra), se reemplaza por la nueva (deja una sola por orden).
    /// </summary>
    [HttpPut("{fotoId:guid}")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20_000_000)]
    public async Task<IActionResult> Upsert(
        Guid detalleId,
        Guid fotoId,
        [FromForm] FotoUpsertForm form,
        CancellationToken ct)
    {
        var file = form.File;
        var orden = form.Orden;
        if (file == null || file.Length == 0) return BadRequest(new { mensaje = "Archivo vacío" });
        if (orden < 1 || orden > 3) return BadRequest(new { mensaje = "Orden debe ser 1, 2 o 3" });
        if (!await PuedeAccederDetalle(detalleId, ct)) return NotFound(new { mensaje = "Detalle no existe" });

        var detalle = await _db.RegistroDetalles.AsNoTracking().FirstOrDefaultAsync(d => d.Id == detalleId, ct);
        if (detalle is null) return NotFound(new { mensaje = "Detalle no existe" });

        using var stream = file.OpenReadStream();
        var (bytes, _) = await _img.ComprimirAsync(stream, ct);

        if (bytes.Length > 1_048_576)
            return BadRequest(new { mensaje = "No se pudo comprimir bajo 1MB" });

        // Buscar si ya existe foto con este Id (upsert por Id)
        var foto = await _db.RegistroDetalleFotos.FirstOrDefaultAsync(f => f.Id == fotoId, ct);
        var esNueva = false;

        if (foto is null)
        {
            esNueva = true;
            // Si hay OTRA foto con el mismo orden, reemplazarla (UPSERT por orden también)
            var existente = await _db.RegistroDetalleFotos
                .FirstOrDefaultAsync(f => f.RegistroDetalleId == detalleId && f.Orden == orden, ct);
            if (existente != null) _db.RegistroDetalleFotos.Remove(existente);

            foto = new RegistroDetalleFoto
            {
                Id = fotoId,
                RegistroDetalleId = detalleId,
                Orden = orden,
                FechaCreacion = DateTime.UtcNow
            };
            _db.RegistroDetalleFotos.Add(foto);
        }
        else
        {
            // Foto existente con ese Id — solo permitir si pertenece al mismo detalle
            if (foto.RegistroDetalleId != detalleId)
                return Conflict(new { mensaje = "La foto pertenece a otro detalle" });
            foto.Orden = orden;
        }

        foto.FotoBinario = bytes;
        foto.FotoMimeType = "image/jpeg";
        foto.FotoPesoBytes = bytes.Length;
        foto.FotoNombre = file.FileName.Length > 200 ? null : Path.GetFileName(file.FileName);
        foto.FechaUltimaModificacion = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        var meta = new FotoMetaDto(
            foto.Id, foto.Orden, foto.FotoMimeType, foto.FotoPesoBytes, foto.FotoNombre,
            $"/api/detalles/{detalleId}/fotos/{foto.Id}/binario", foto.FechaCreacion);

        return esNueva ? StatusCode(201, meta) : Ok(meta);
    }

    /// <summary>Sirve los bytes; acepta JWT por header o ?token=. ?download=true para attachment.</summary>
    [HttpGet("{fotoId:guid}/binario")]
    public async Task<IActionResult> GetBinario(
        Guid detalleId, Guid fotoId,
        [FromQuery] bool download = false,
        CancellationToken ct = default)
    {
        if (!await PuedeAccederDetalle(detalleId, ct)) return NotFound();
        var f = await _db.RegistroDetalleFotos.AsNoTracking()
            .Where(x => x.Id == fotoId && x.RegistroDetalleId == detalleId)
            .Select(x => new { x.FotoBinario, x.FotoMimeType, x.FotoNombre, x.FechaUltimaModificacion, x.FechaCreacion })
            .FirstOrDefaultAsync(ct);

        if (f is null) return NotFound();

        var mime = string.IsNullOrEmpty(f.FotoMimeType) ? "image/jpeg" : f.FotoMimeType;
        var nombre = string.IsNullOrEmpty(f.FotoNombre) ? $"foto-{fotoId}.jpg" : f.FotoNombre;
        var ts = (f.FechaUltimaModificacion ?? f.FechaCreacion).Ticks;

        Response.Headers.CacheControl = "private, max-age=3600";
        Response.Headers.ETag = $"\"{ts}\"";
        if (Request.Headers.IfNoneMatch.ToString() == $"\"{ts}\"") return StatusCode(304);

        if (download)
        {
            Response.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileNameStar = nombre
            }.ToString();
        }

        return File(f.FotoBinario, mime);
    }

    [HttpDelete("{fotoId:guid}")]
    public async Task<IActionResult> Delete(Guid detalleId, Guid fotoId, CancellationToken ct)
    {
        if (!await PuedeAccederDetalle(detalleId, ct)) return NotFound();
        var f = await _db.RegistroDetalleFotos
            .FirstOrDefaultAsync(x => x.Id == fotoId && x.RegistroDetalleId == detalleId, ct);
        if (f is null) return NotFound();
        _db.RegistroDetalleFotos.Remove(f);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
