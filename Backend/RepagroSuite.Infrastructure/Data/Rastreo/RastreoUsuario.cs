namespace RepagroSuite.Infrastructure.Data.Rastreo;

/// <summary>
/// Entidad espejo de la tabla <c>RASTREO.Usuarios</c> del sistema de Rastreo, que vive en la
/// MISMA base de datos física pero en su propio esquema. NO es un usuario de Repagro Suite:
/// estos usuarios pertenecen exclusivamente al sistema de Rastreo y nunca se mezclan con los
/// de Repagro (que viven en <c>CORE.Usuarios</c> con Id GUID).
///
/// Este mapeo es una COPIA EXACTA del modelo definido por el proyecto de Rastreo
/// (C:\Users\alejandro.solis\RASTRO ▸ Models/Usuario.cs). Si Rastreo cambia su esquema,
/// hay que reflejar el cambio aquí. Este contexto JAMÁS aplica migraciones sobre el esquema RASTREO.
/// </summary>
public class RastreoUsuario
{
    public int Id { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Rol dentro del sistema de Rastreo: <c>ADMIN</c> o <c>USUARIO</c>.</summary>
    public string Rol { get; set; } = "USUARIO";

    /// <summary>Sesión única por dispositivo del sistema de Rastreo. Se invalida (null) al
    /// restablecer contraseña o desactivar para forzar un nuevo inicio de sesión.</summary>
    public Guid? SesionToken { get; set; }
    public DateTime? SesionExpira { get; set; }
}
