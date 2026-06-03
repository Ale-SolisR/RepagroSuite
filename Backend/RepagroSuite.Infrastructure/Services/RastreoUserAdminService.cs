using Microsoft.EntityFrameworkCore;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.RastreoUsers.DTOs;
using RepagroSuite.Application.Features.RastreoUsers.Services;
using RepagroSuite.Infrastructure.Data.Rastreo;

namespace RepagroSuite.Infrastructure.Services;

/// <summary>
/// Administra los usuarios del sistema de Rastreo (<c>RASTREO.Usuarios</c>) desde Repagro Suite.
/// Usa el MISMO hash BCrypt que el sistema de Rastreo (vía <see cref="IPasswordService"/>), por lo que
/// las contraseñas asignadas aquí funcionan directamente en el login de Rastreo. Cada operación de
/// escritura queda auditada en <c>CORE.RegistrosAuditoria</c> con el administrador de Repagro que la ejecutó.
/// </summary>
public class RastreoUserAdminService : IRastreoUserAdminService
{
    // Roles soportados HOY por el sistema de Rastreo (Models/Usuario.cs: ADMIN ve todo, USUARIO solo lo suyo).
    private static readonly string[] RolesValidos = ["ADMIN", "USUARIO"];

    private readonly RastreoDbContext _db;
    private readonly IPasswordService _passwords;
    private readonly IAuditService _audit;

    public RastreoUserAdminService(RastreoDbContext db, IPasswordService passwords, IAuditService audit)
    {
        _db = db;
        _passwords = passwords;
        _audit = audit;
    }

    public async Task<PagedResult<RastreoUserDto>> GetPagedAsync(int page, int pageSize, string? search, bool? activeOnly, CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 100) pageSize = 20;

        var query = _db.Usuarios.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u => u.Correo.Contains(term) || (u.Nombre != null && u.Nombre.ToLower().Contains(term)));
        }
        if (activeOnly is true) query = query.Where(u => u.Activo);
        else if (activeOnly is false) query = query.Where(u => !u.Activo);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.Nombre ?? u.Correo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<RastreoUserDto>
        {
            Items = items.Select(Map),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<RastreoUserDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => Map(await FindOrThrow(id, cancellationToken));

    public async Task<RastreoUserDto> CreateAsync(CreateRastreoUserDto dto, Guid adminId, CancellationToken cancellationToken = default)
    {
        var correo = NormalizeEmail(dto.Correo);
        if (string.IsNullOrWhiteSpace(correo) || !correo.Contains('@'))
            throw new InvalidOperationException("El correo no es válido.");

        var rol = NormalizeRole(dto.Rol);

        if (!_passwords.IsPasswordPolicyCompliant(dto.Password, out var violations))
            throw new InvalidOperationException(string.Join(" ", violations));

        if (await _db.Usuarios.AnyAsync(u => u.Correo == correo, cancellationToken))
            throw new InvalidOperationException("Ya existe un usuario de rastreo con ese correo.");

        var usuario = new RastreoUsuario
        {
            Correo = correo,
            Nombre = string.IsNullOrWhiteSpace(dto.Nombre) ? null : dto.Nombre.Trim(),
            Rol = rol,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            PasswordHash = _passwords.HashPassword(dto.Password)
        };

        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(adminId, "RASTREO_USER_CREATED", entityName: "RastreoUsuario", entityId: usuario.Id.ToString(),
            newValues: new { usuario.Correo, usuario.Rol }, module: "RastreoUsers", cancellationToken: cancellationToken);

        return Map(usuario);
    }

    public async Task ResetPasswordAsync(int id, ResetRastreoUserPasswordDto dto, Guid adminId, CancellationToken cancellationToken = default)
    {
        var usuario = await FindOrThrow(id, cancellationToken);

        if (!_passwords.IsPasswordPolicyCompliant(dto.NewPassword, out var violations))
            throw new InvalidOperationException(string.Join(" ", violations));

        usuario.PasswordHash = _passwords.HashPassword(dto.NewPassword);
        if (dto.CloseActiveSession)
        {
            usuario.SesionToken = null;
            usuario.SesionExpira = null;
        }
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(adminId, "RASTREO_USER_PASSWORD_RESET", entityName: "RastreoUsuario", entityId: usuario.Id.ToString(),
            newValues: new { usuario.Correo, dto.CloseActiveSession }, module: "RastreoUsers", cancellationToken: cancellationToken);
    }

    public async Task<RastreoUserDto> ChangeRoleAsync(int id, UpdateRastreoUserRoleDto dto, Guid adminId, CancellationToken cancellationToken = default)
    {
        var usuario = await FindOrThrow(id, cancellationToken);
        var nuevoRol = NormalizeRole(dto.Rol);
        var rolAnterior = usuario.Rol;

        usuario.Rol = nuevoRol;
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(adminId, "RASTREO_USER_ROLE_CHANGED", entityName: "RastreoUsuario", entityId: usuario.Id.ToString(),
            oldValues: new { Rol = rolAnterior }, newValues: new { Rol = nuevoRol }, module: "RastreoUsers", cancellationToken: cancellationToken);

        return Map(usuario);
    }

    public async Task<RastreoUserDto> SetStatusAsync(int id, UpdateRastreoUserStatusDto dto, Guid adminId, CancellationToken cancellationToken = default)
    {
        var usuario = await FindOrThrow(id, cancellationToken);

        usuario.Activo = dto.Activo;
        // Al desactivar, cerramos cualquier sesión vigente para cortar el acceso de inmediato.
        if (!dto.Activo)
        {
            usuario.SesionToken = null;
            usuario.SesionExpira = null;
        }
        await _db.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(adminId, dto.Activo ? "RASTREO_USER_ACTIVATED" : "RASTREO_USER_DEACTIVATED",
            entityName: "RastreoUsuario", entityId: usuario.Id.ToString(),
            newValues: new { usuario.Correo, usuario.Activo }, module: "RastreoUsers", cancellationToken: cancellationToken);

        return Map(usuario);
    }

    private async Task<RastreoUsuario> FindOrThrow(int id, CancellationToken ct)
        => await _db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new KeyNotFoundException("Usuario de rastreo no encontrado.");

    private static string NormalizeEmail(string? correo) => (correo ?? string.Empty).Trim().ToLowerInvariant();

    private static string NormalizeRole(string? rol)
    {
        var r = (rol ?? string.Empty).Trim().ToUpperInvariant();
        if (!RolesValidos.Contains(r))
            throw new InvalidOperationException("El rol debe ser ADMIN o USUARIO.");
        return r;
    }

    private static RastreoUserDto Map(RastreoUsuario u) => new()
    {
        Id = u.Id,
        Nombre = u.Nombre,
        Correo = u.Correo,
        Rol = u.Rol,
        Activo = u.Activo,
        FechaCreacion = u.FechaCreacion,
        TieneSesionActiva = u.SesionToken.HasValue && u.SesionExpira.HasValue && u.SesionExpira.Value > DateTime.UtcNow
    };
}
