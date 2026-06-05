using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreo.Api.Data;
using Rastreo.Api.Dtos;
using Rastreo.Api.Models;

namespace Rastreo.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Rastreo")]
[Route("api/granjas")]
public class GranjasController : ControllerBase
{
    private readonly RastreoDbContext _db;
    public GranjasController(RastreoDbContext db) { _db = db; }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool soloActivas = false, [FromQuery] string? q = null)
    {
        var query = _db.Granjas.AsNoTracking();
        if (soloActivas) query = query.Where(g => g.Activo);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var s = q.Trim();
            query = query.Where(g => EF.Functions.Like(g.Codigo, $"%{s}%") || EF.Functions.Like(g.Nombre, $"%{s}%"));
        }
        var list = await query
            .OrderBy(g => g.Codigo)
            .Select(g => new GranjaDto(g.Id, g.Codigo, g.Nombre, g.Propietario, g.Ubicacion,
                g.Telefono, g.Correo, g.Observaciones, g.Activo, g.FechaCreacion))
            .ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var g = await _db.Granjas.FindAsync(id);
        if (g is null) return NotFound();
        return Ok(new GranjaDto(g.Id, g.Codigo, g.Nombre, g.Propietario, g.Ubicacion,
            g.Telefono, g.Correo, g.Observaciones, g.Activo, g.FechaCreacion));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] GranjaUpsertDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.Codigo) || string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { mensaje = "Código y nombre son obligatorios" });
        var codigo = dto.Codigo.Trim().ToUpperInvariant();
        var nombre = dto.Nombre.Trim();

        if (await _db.Granjas.AnyAsync(g => g.Codigo == codigo, ct))
            return Conflict(new { mensaje = "Ya existe una granja con ese código" });
        if (await _db.Granjas.AnyAsync(g => g.Nombre == nombre && g.Activo, ct))
            return Conflict(new { mensaje = "Ya existe una granja activa con ese nombre" });

        var entity = new Granja
        {
            Codigo = codigo,
            Nombre = nombre,
            Propietario = dto.Propietario?.Trim(),
            Ubicacion = dto.Ubicacion?.Trim(),
            Telefono = dto.Telefono?.Trim(),
            Correo = dto.Correo?.Trim(),
            Observaciones = dto.Observaciones?.Trim(),
            Activo = true,
            UsuarioCreacion = User.Identity?.Name
        };
        _db.Granjas.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = entity.Id },
            new GranjaDto(entity.Id, entity.Codigo, entity.Nombre, entity.Propietario, entity.Ubicacion,
                entity.Telefono, entity.Correo, entity.Observaciones, entity.Activo, entity.FechaCreacion));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] GranjaUpsertDto dto, CancellationToken ct)
    {
        var g = await _db.Granjas.FindAsync(new object?[] { id }, ct);
        if (g is null) return NotFound();

        var nuevoCodigo = dto.Codigo.Trim().ToUpperInvariant();
        var nuevoNombre = dto.Nombre.Trim();

        if (g.Codigo != nuevoCodigo && await _db.Granjas.AnyAsync(x => x.Codigo == nuevoCodigo, ct))
            return Conflict(new { mensaje = "Ya existe otra granja con ese código" });
        if ((g.Nombre != nuevoNombre || !g.Activo) && await _db.Granjas.AnyAsync(x => x.Nombre == nuevoNombre && x.Activo && x.Id != id, ct))
            return Conflict(new { mensaje = "Ya existe otra granja activa con ese nombre" });

        g.Codigo = nuevoCodigo;
        g.Nombre = nuevoNombre;
        g.Propietario = dto.Propietario?.Trim();
        g.Ubicacion = dto.Ubicacion?.Trim();
        g.Telefono = dto.Telefono?.Trim();
        g.Correo = dto.Correo?.Trim();
        g.Observaciones = dto.Observaciones?.Trim();
        g.FechaModificacion = DateTime.UtcNow;
        g.UsuarioModificacion = User.Identity?.Name;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/activar")]
    public async Task<IActionResult> Activar(int id, CancellationToken ct)
    {
        var g = await _db.Granjas.FindAsync(new object?[] { id }, ct);
        if (g is null) return NotFound();
        g.Activo = true;
        g.FechaModificacion = DateTime.UtcNow;
        g.UsuarioModificacion = User.Identity?.Name;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPatch("{id:int}/desactivar")]
    public async Task<IActionResult> Desactivar(int id, CancellationToken ct)
    {
        var g = await _db.Granjas.FindAsync(new object?[] { id }, ct);
        if (g is null) return NotFound();
        g.Activo = false;
        g.FechaModificacion = DateTime.UtcNow;
        g.UsuarioModificacion = User.Identity?.Name;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
