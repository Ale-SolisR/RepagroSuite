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
    /// <summary>Contraseña inicial en texto plano (solo viaja en la petición; se almacena hasheada con BCrypt).</summary>
    public string Password { get; set; } = string.Empty;
}

public class ResetRastreoUserPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
    /// <summary>Si es true, invalida la sesión activa del usuario para forzar un nuevo inicio de sesión.</summary>
    public bool CloseActiveSession { get; set; } = true;
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
