using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreo.Api.Data;
using Rastreo.Api.Dtos;
using Rastreo.Api.Models;

namespace Rastreo.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Rastreo")]
[Route("api/enfermedades")]
public class EnfermedadesController : ControllerBase
{
    private readonly RastreoDbContext _db;
    public EnfermedadesController(RastreoDbContext db) { _db = db; }

    // Tipos de campo soportados por la captura y por las tabulaciones (KPIs, Excel, PDF).
    // Si se permitiera otro valor, el hallazgo se capturaría pero NUNCA tabularía (helper => false).
    private static readonly HashSet<string> TiposValidos = new(StringComparer.OrdinalIgnoreCase)
    { "SCORE_0_4", "BOOL", "AGUDA_CRONICA" };

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var lista = await _db.Enfermedades.AsNoTracking()
            .OrderBy(e => e.Orden).ThenBy(e => e.Nombre)
            .Select(e => new EnfermedadDto(e.Id, e.Codigo, e.Nombre, e.TipoCampo, e.Orden, e.Activo))
            .ToListAsync();
        return Ok(lista);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EnfermedadUpsertDto dto, CancellationToken ct)
    {
        var tipo = dto.TipoCampo.Trim().ToUpperInvariant();
        if (!TiposValidos.Contains(tipo))
            return BadRequest(new { mensaje = $"Tipo de campo inválido. Use uno de: {string.Join(", ", TiposValidos)}" });
        var codigo = dto.Codigo.Trim().ToUpperInvariant();
        if (await _db.Enfermedades.AnyAsync(e => e.Codigo == codigo, ct))
            return Conflict(new { mensaje = "Ya existe una enfermedad con ese código" });
        var e = new Enfermedad
        {
            Codigo = codigo,
            Nombre = dto.Nombre.Trim(),
            TipoCampo = tipo,
            Orden = dto.Orden,
            Activo = true
        };
        _db.Enfermedades.Add(e);
        await _db.SaveChangesAsync(ct);
        return Ok(new EnfermedadDto(e.Id, e.Codigo, e.Nombre, e.TipoCampo, e.Orden, e.Activo));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] EnfermedadUpsertDto dto, CancellationToken ct)
    {
        var tipo = dto.TipoCampo.Trim().ToUpperInvariant();
        if (!TiposValidos.Contains(tipo))
            return BadRequest(new { mensaje = $"Tipo de campo inválido. Use uno de: {string.Join(", ", TiposValidos)}" });
        var e = await _db.Enfermedades.FindAsync(new object?[] { id }, ct);
        if (e is null) return NotFound();
        e.Nombre = dto.Nombre.Trim();
        e.TipoCampo = tipo;
        e.Orden = dto.Orden;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/activar")]
    public async Task<IActionResult> Activar(int id, CancellationToken ct) => await SetActivo(id, true, ct);

    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct) => await SetActivo(id, false, ct);

    private async Task<IActionResult> SetActivo(int id, bool activo, CancellationToken ct)
    {
        var e = await _db.Enfermedades.FindAsync(new object?[] { id }, ct);
        if (e is null) return NotFound();
        e.Activo = activo;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
