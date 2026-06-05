using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreo.Api.Data;

namespace Rastreo.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = "Rastreo")]
[Route("api/usuarios")]
public class UsuariosController : ControllerBase
{
    private readonly RastreoDbContext _db;
    public UsuariosController(RastreoDbContext db) { _db = db; }

    private bool EsAdmin => User.FindFirst("rol")?.Value == "ADMIN";

    /// <summary>Lista de usuarios activos para el filtro del administrador.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (!EsAdmin) return Forbid();
        var usuarios = await _db.Usuarios.AsNoTracking()
            .Where(u => u.Activo)
            .OrderBy(u => u.Nombre)
            .Select(u => new { u.Id, u.Nombre, u.Correo, u.Rol })
            .ToListAsync(ct);
        return Ok(usuarios);
    }
}
