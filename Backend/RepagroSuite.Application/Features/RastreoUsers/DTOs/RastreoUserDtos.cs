namespace RepagroSuite.Application.Features.RastreoUsers.DTOs;

/// <summary>Usuario del sistema de Rastreo expuesto a la administración. NUNCA incluye el hash de contraseña.</summary>
public class RastreoUserDto
{
    public int Id { get; set; }
    public string? Nombre { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string Rol { get; set; } = "USUARIO";
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    /// <summary>True si el usuario tiene una sesión vigente en algún dispositivo del sistema de Rastreo.</summary>
    public bool TieneSesionActiva { get; set; }
}

public class CreateRastreoUserDto
{
    public string? Nombre { get; set; }
    public string Correo { get; set; } = string.Empty;
    /// <summary><c>ADMIN</c> o <c>USUARIO</c>.</summary>
    public string Rol { get; set; } = "USUARIO";
    /// <summary>Contraseña inicial opcional (texto plano sólo en la petición; se guarda hasheada con BCrypt). Si viene vacía o <see cref="Generate"/> es true, el sistema genera una segura.</summary>
    public string? Password { get; set; }
    /// <summary>Si es true, ignora <see cref="Password"/> y genera una contraseña segura aleatoria.</summary>
    public bool Generate { get; set; }
}

public class ResetRastreoUserPasswordDto
{
    /// <summary>Nueva contraseña opcional. Si viene vacía o <see cref="Generate"/> es true, el sistema genera una segura.</summary>
    public string? NewPassword { get; set; }
    /// <summary>Si es true, ignora <see cref="NewPassword"/> y genera una contraseña segura aleatoria.</summary>
    public bool Generate { get; set; }
    /// <summary>Si es true, invalida la sesión activa del usuario para forzar un nuevo inicio de sesión.</summary>
    public bool CloseActiveSession { get; set; } = true;
}

/// <summary>
/// Resultado de crear/restablecer: incluye la contraseña EFECTIVA en texto plano para mostrarla
/// UNA sola vez al administrador (no se guarda legible; sólo su hash). El frontend debe mostrarla
/// en un modal de "copiar y entregar" y nunca volver a pedirla.
/// </summary>
public class RastreoUserPasswordResultDto
{
    public RastreoUserDto? User { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class UpdateRastreoUserRoleDto
{
    /// <summary><c>ADMIN</c> o <c>USUARIO</c>.</summary>
    public string Rol { get; set; } = "USUARIO";
}

public class UpdateRastreoUserStatusDto
{
    public bool Activo { get; set; }
}
