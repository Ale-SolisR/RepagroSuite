using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rastreo.Api.Data;
using Rastreo.Api.Dtos;
using Rastreo.Api.Services;

namespace Rastreo.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RastreoDbContext _db;
    private readonly JwtService _jwt;

    /// <summary>Ventana de inactividad de la sesión (horas). Tras este lapso SIN actividad, la sesión expira.</summary>
    public const double SesionInactividadHoras = 3;

    public AuthController(RastreoDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Correo) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { mensaje = "Correo y contraseña son obligatorios" });

        var usuario = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.Correo == req.Correo.Trim().ToLower() && u.Activo, ct);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(req.Password, usuario.PasswordHash))
            return Unauthorized(new { mensaje = "Credenciales inválidas" });

        // Sesión única: si ya hay una sesión vigente y no se está forzando, avisar al cliente.
        var sesionVigente = usuario.SesionToken.HasValue
            && usuario.SesionExpira.HasValue
            && usuario.SesionExpira.Value > DateTime.UtcNow;

        if (sesionVigente && !req.Forzar)
        {
            return Conflict(new
            {
                sesionActiva = true,
                mensaje = "Ya hay una sesión activa para este usuario en otro dispositivo."
            });
        }

        // Emitir nueva sesión (invalida cualquier sesión anterior).
        var sesionToken = Guid.NewGuid();
        var (token, expira) = _jwt.GenerarToken(usuario, sesionToken);
        usuario.SesionToken = sesionToken;
        // SesionExpira = ventana DESLIZANTE de inactividad (3h). El JWT dura mucho más (credencial),
        // pero la BD es la fuente de verdad: cada request la extiende y, tras 3h sin actividad, expira.
        usuario.SesionExpira = DateTime.UtcNow.AddHours(SesionInactividadHoras);
        await _db.SaveChangesAsync(ct);

        return Ok(new LoginResponse(token, usuario.Correo, usuario.Nombre, usuario.Rol, usuario.Id, expira));
    }

    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = "Rastreo")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var uid = User.FindFirst("uid")?.Value;
        var sid = User.FindFirst("sid")?.Value;
        if (int.TryParse(uid, out var id))
        {
            var u = await _db.Usuarios.FirstOrDefaultAsync(x => x.Id == id, ct);
            // Solo limpia si el token de sesión coincide (no cerrar una sesión más nueva).
            if (u != null && u.SesionToken.HasValue && u.SesionToken.Value.ToString() == sid)
            {
                u.SesionToken = null;
                u.SesionExpira = null;
                await _db.SaveChangesAsync(ct);
            }
        }
        return Ok(new { mensaje = "Sesión cerrada" });
    }

    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = "Rastreo")]
    public IActionResult Me()
    {
        return Ok(new
        {
            uid = User.FindFirst("uid")?.Value,
            correo = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email)?.Value
                  ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value,
            nombre = User.Identity?.Name,
            rol = User.FindFirst("rol")?.Value ?? "USUARIO"
        });
    }
}
