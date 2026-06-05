using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreo.Api.Data;
using Rastreo.Api.Dtos;
using Rastreo.Api.Models;

namespace Rastreo.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Rastreo")]
[Route("api/vacunas")]
public class VacunasController : ControllerBase
{
    private readonly RastreoDbContext _db;
    public VacunasController(RastreoDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var lista = await _db.Vacunas.AsNoTracking()
            .OrderBy(v => v.Orden).ThenBy(v => v.Nombre)
            .Select(v => new VacunaDto(v.Id, v.Codigo, v.Nombre, v.Laboratorio, v.Observaciones, v.Orden, v.Activo))
            .ToListAsync(ct);
        return Ok(lista);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] VacunaUpsertDto dto, CancellationToken ct)
    {
        var codigo = dto.Codigo.Trim().ToUpperInvariant();
        if (await _db.Vacunas.AnyAsync(v => v.Codigo == codigo, ct))
            return Conflict(new { mensaje = "Ya existe una vacuna con ese codigo" });

        var v = new Vacuna
        {
            Codigo = codigo,
            Nombre = dto.Nombre.Trim(),
            Laboratorio = string.IsNullOrWhiteSpace(dto.Laboratorio) ? null : dto.Laboratorio.Trim(),
            Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
            Orden = dto.Orden,
            Activo = true
        };
        _db.Vacunas.Add(v);
        await _db.SaveChangesAsync(ct);
        return Ok(new VacunaDto(v.Id, v.Codigo, v.Nombre, v.Laboratorio, v.Observaciones, v.Orden, v.Activo));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] VacunaUpsertDto dto, CancellationToken ct)
    {
        var v = await _db.Vacunas.FindAsync(new object?[] { id }, ct);
        if (v == null) return NotFound();
        var codigo = dto.Codigo.Trim().ToUpperInvariant();
        if (await _db.Vacunas.AnyAsync(x => x.Codigo == codigo && x.Id != id, ct))
            return Conflict(new { mensaje = "Ya existe una vacuna con ese codigo" });
        v.Codigo = codigo;
        v.Nombre = dto.Nombre.Trim();
        v.Laboratorio = string.IsNullOrWhiteSpace(dto.Laboratorio) ? null : dto.Laboratorio.Trim();
        v.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();
        v.Orden = dto.Orden;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/activar")]
    public async Task<IActionResult> Activar(int id, CancellationToken ct) => await SetActivo(id, true, ct);

    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct) => await SetActivo(id, false, ct);

    private async Task<IActionResult> SetActivo(int id, bool activo, CancellationToken ct)
    {
        var v = await _db.Vacunas.FindAsync(new object?[] { id }, ct);
        if (v == null) return NotFound();
        v.Activo = activo;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
