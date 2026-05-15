using Microsoft.EntityFrameworkCore;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Feature> Features => Set<Feature>();
    public DbSet<RoomFeature> RoomFeatures => Set<RoomFeature>();
    public DbSet<RoomAvailability> RoomAvailabilities => Set<RoomAvailability>();
    public DbSet<RoomBlock> RoomBlocks => Set<RoomBlock>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<SystemModule> SystemModules => Set<SystemModule>();
    public DbSet<IdentificationLookupCache> IdentificationLookupCaches => Set<IdentificationLookupCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        var adminRoleId = new Guid("11111111-1111-1111-1111-111111111111");
        var userRoleId  = new Guid("22222222-2222-2222-2222-222222222222");
        var adminUserId = new Guid("33333333-3333-3333-3333-333333333333");
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // --- Roles ---
        modelBuilder.Entity<Role>().HasData(
            new Role { Id = adminRoleId, Name = "Administrator", NormalizedName = "ADMINISTRATOR", Description = "Administrador del sistema", IsSystemRole = true, IsActive = true, CreatedAt = now, RowVersion = [] },
            new Role { Id = userRoleId,  Name = "User",          NormalizedName = "USER",          Description = "Usuario estándar",        IsSystemRole = true, IsActive = true, CreatedAt = now, RowVersion = [] }
        );

        // --- Permissions ---
        var allPerms = new List<Permission>
        {
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000001"), Code = "Users.View",                     Name = "Ver usuarios",                     Module = "Users",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000002"), Code = "Users.Approve",                  Name = "Aprobar usuarios",                 Module = "Users",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000003"), Code = "Users.Reject",                   Name = "Rechazar usuarios",                Module = "Users",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000004"), Code = "Users.Block",                    Name = "Bloquear usuarios",                Module = "Users",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000005"), Code = "Users.GenerateTemporaryPassword",Name = "Generar contraseña temporal",      Module = "Users",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000006"), Code = "Users.ForcePasswordChange",      Name = "Forzar cambio de contraseña",      Module = "Users",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000007"), Code = "Identifications.Lookup",         Name = "Consultar identificaciones",       Module = "Identifications", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000008"), Code = "Rooms.View",                     Name = "Ver salas",                        Module = "Rooms",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000009"), Code = "Rooms.Create",                   Name = "Crear salas",                      Module = "Rooms",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000010"), Code = "Rooms.Update",                   Name = "Editar salas",                     Module = "Rooms",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000011"), Code = "Rooms.Delete",                   Name = "Eliminar salas",                   Module = "Rooms",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000012"), Code = "Rooms.Availability.Manage",      Name = "Gestionar disponibilidad",         Module = "Rooms",           IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000013"), Code = "Reservations.View",              Name = "Ver reservas",                     Module = "Reservations",    IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000014"), Code = "Reservations.Create",            Name = "Crear reservas",                   Module = "Reservations",    IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000015"), Code = "Reservations.Approve",           Name = "Aprobar reservas",                 Module = "Reservations",    IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000016"), Code = "Reservations.Reject",            Name = "Rechazar reservas",                Module = "Reservations",    IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000017"), Code = "Reservations.Cancel",            Name = "Cancelar reservas",                Module = "Reservations",    IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000018"), Code = "Reservations.DirectCreate",      Name = "Reserva directa administrativa",   Module = "Reservations",    IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000019"), Code = "AuditLogs.View",                 Name = "Ver auditoría",                    Module = "AuditLogs",       IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000020"), Code = "Reports.View",                   Name = "Ver reportes",                     Module = "Reports",         IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000021"), Code = "Settings.View",                  Name = "Ver configuración",                Module = "Settings",        IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000022"), Code = "Settings.Update",                Name = "Editar configuración",             Module = "Settings",        IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000023"), Code = "Settings.Email.View",            Name = "Ver config. correo",               Module = "Settings",        IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000024"), Code = "Settings.Email.Update",          Name = "Editar config. correo",            Module = "Settings",        IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000025"), Code = "Settings.Email.Test",            Name = "Probar config. correo",            Module = "Settings",        IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000026"), Code = "Settings.Modules.View",          Name = "Ver módulos",                      Module = "Settings",        IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000027"), Code = "Settings.Modules.Create",        Name = "Crear módulos",                    Module = "Settings",        IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000028"), Code = "Settings.Modules.Update",        Name = "Editar módulos",                   Module = "Settings",        IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000029"), Code = "Settings.Modules.Delete",        Name = "Eliminar módulos",                 Module = "Settings",        IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000030"), Code = "Settings.Security.Manage",       Name = "Gestionar seguridad",              Module = "Settings",        IsActive = true, CreatedAt = now, RowVersion = [] },
        };
        modelBuilder.Entity<Permission>().HasData(allPerms);

        // All permissions → Administrator
        modelBuilder.Entity<RolePermission>().HasData(allPerms.Select(p => new RolePermission
        {
            RoleId = adminRoleId, PermissionId = p.Id, AssignedAt = now
        }));

        // Basic permissions → User
        var userPermCodes = new[] { "Rooms.View", "Reservations.View", "Reservations.Create", "Reservations.Cancel", "Identifications.Lookup" };
        modelBuilder.Entity<RolePermission>().HasData(
            allPerms.Where(p => userPermCodes.Contains(p.Code)).Select(p => new RolePermission
            {
                RoleId = userRoleId, PermissionId = p.Id, AssignedAt = now
            })
        );

        // Admin user (password: Rep2026*- — BCrypt hash)
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminUserId,
            IdentificationType = IdentificationType.PhysicalId,
            IdentificationNumber = "000000000",
            NormalizedIdentificationNumber = "000000000",
            FullName = "ADMINISTRADOR REPAGRO",
            FirstName = "ADMINISTRADOR",
            FirstName1 = "ADMINISTRADOR",
            LastName = "REPAGRO",
            LastName1 = "REPAGRO",
            LastName2 = "",
            Email = "gestionwebrepagro@gmail.com",
            NormalizedEmail = "GESTIONWEBREPAGRO@GMAIL.COM",
            Status = UserStatus.Active,
            PasswordHash = "$2a$11$SVxo12avCQjNSUPOFt3sBuYOh0nY7sax3RtpH3TxWVVNT2Y0mvPSu",
            MustChangePassword = false,
            IdentificationValidated = false,
            IdentificationValidationSource = "Manual",
            CreatedAt = now,
            RowVersion = []
        });

        modelBuilder.Entity<UserRole>().HasData(new UserRole
        {
            UserId = adminUserId, RoleId = adminRoleId, AssignedAt = now
        });

        // Features (amenidades de salas)
        modelBuilder.Entity<Feature>().HasData(
            new Feature { Id = new Guid("f0000001-0000-0000-0000-000000000001"), Name = "Proyector",            IconName = "projector", IsActive = true, CreatedAt = now, RowVersion = [] },
            new Feature { Id = new Guid("f0000001-0000-0000-0000-000000000002"), Name = "Pantalla",             IconName = "monitor",   IsActive = true, CreatedAt = now, RowVersion = [] },
            new Feature { Id = new Guid("f0000001-0000-0000-0000-000000000003"), Name = "Pizarra",              IconName = "square",    IsActive = true, CreatedAt = now, RowVersion = [] },
            new Feature { Id = new Guid("f0000001-0000-0000-0000-000000000004"), Name = "Aire acondicionado",   IconName = "wind",      IsActive = true, CreatedAt = now, RowVersion = [] },
            new Feature { Id = new Guid("f0000001-0000-0000-0000-000000000005"), Name = "Internet",             IconName = "wifi",      IsActive = true, CreatedAt = now, RowVersion = [] },
            new Feature { Id = new Guid("f0000001-0000-0000-0000-000000000006"), Name = "Videoconferencia",     IconName = "video",     IsActive = true, CreatedAt = now, RowVersion = [] },
            new Feature { Id = new Guid("f0000001-0000-0000-0000-000000000007"), Name = "TV",                   IconName = "tv",        IsActive = true, CreatedAt = now, RowVersion = [] },
            new Feature { Id = new Guid("f0000001-0000-0000-0000-000000000008"), Name = "Sistema de audio",     IconName = "volume-2",  IsActive = true, CreatedAt = now, RowVersion = [] },
            new Feature { Id = new Guid("f0000001-0000-0000-0000-000000000009"), Name = "Mesa de reuniones",    IconName = "layout",    IsActive = true, CreatedAt = now, RowVersion = [] },
            new Feature { Id = new Guid("f0000001-0000-0000-0000-000000000010"), Name = "Conexiones eléctricas",IconName = "zap",       IsActive = true, CreatedAt = now, RowVersion = [] }
        );

        // System Modules
        modelBuilder.Entity<SystemModule>().HasData(new SystemModule
        {
            Id = new Guid("c0000001-0000-0000-0000-000000000001"),
            Name = "Gestión de Salas", Code = "ROOMS",
            Description = "Reservas y gestión de salas empresariales",
            IconName = "door-open", RoutePrefix = "/rooms", SortOrder = 1,
            IsActive = true, IsCore = true, Version = "1.0.0",
            CreatedAt = now, RowVersion = []
        });

        // System Settings
        modelBuilder.Entity<SystemSetting>().HasData(
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000001"), Key = "APP.NAME",                         Value = "RepagroSuite",                    DefaultValue = "RepagroSuite",                    Description = "Nombre de la aplicación",                   Module = "GENERAL",        DataType = "string", CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000002"), Key = "APP.TIMEZONE",                    Value = "America/Costa_Rica",              DefaultValue = "UTC",                             Description = "Zona horaria del sistema",                  Module = "GENERAL",        DataType = "string", CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000003"), Key = "AUTH.MAX_FAILED_ATTEMPTS",        Value = "5",                               DefaultValue = "5",                               Description = "Máximo intentos fallidos antes de bloqueo", Module = "AUTH",           DataType = "int",    CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000004"), Key = "AUTH.LOCKOUT_MINUTES",            Value = "15",                              DefaultValue = "15",                              Description = "Minutos de bloqueo por intentos fallidos",  Module = "AUTH",           DataType = "int",    CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000005"), Key = "AUTH.PASSWORD_RESET_TOKEN_HOURS", Value = "24",                              DefaultValue = "24",                              Description = "Horas de validez del token de recuperación",Module = "AUTH",           DataType = "int",    CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000006"), Key = "AUTH.TEMP_PASSWORD_HOURS",        Value = "72",                              DefaultValue = "72",                              Description = "Horas de validez de contraseña temporal",   Module = "AUTH",           DataType = "int",    CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000007"), Key = "IDENTIFICATION.PROVIDER",         Value = "GoMeta",                          DefaultValue = "GoMeta",                          Description = "Proveedor de consulta de cédulas",          Module = "IDENTIFICATION", DataType = "string", CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000008"), Key = "IDENTIFICATION.BASE_URL",         Value = "https://apis.gometa.org/cedulas/",DefaultValue = "https://apis.gometa.org/cedulas/",Description = "URL base del proveedor de cédulas",         Module = "IDENTIFICATION", DataType = "string", CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000009"), Key = "IDENTIFICATION.CACHE_ENABLED",    Value = "true",                            DefaultValue = "true",                            Description = "Activar caché de cédulas",                  Module = "IDENTIFICATION", DataType = "bool",   CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000010"), Key = "IDENTIFICATION.CACHE_DAYS",       Value = "30",                              DefaultValue = "30",                              Description = "Días de validez del caché de cédulas",      Module = "IDENTIFICATION", DataType = "int",    CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000011"), Key = "EMAIL.ENABLED",                   Value = "false",                           DefaultValue = "false",                           Description = "Activar envío de correos",                  Module = "EMAIL",          DataType = "bool",   CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000012"), Key = "EMAIL.FROM_NAME",                 Value = "RepagroSuite",                    DefaultValue = "RepagroSuite",                    Description = "Nombre del remitente",                      Module = "EMAIL",          DataType = "string", CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000013"), Key = "EMAIL.FROM_ADDRESS",              Value = "",                                DefaultValue = "",                                Description = "Correo del remitente",                      Module = "EMAIL",          DataType = "string", CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000014"), Key = "EMAIL.SMTP_HOST",                 Value = "",                                DefaultValue = "",                                Description = "Servidor SMTP",                             Module = "EMAIL",          DataType = "string", CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000015"), Key = "EMAIL.SMTP_PORT",                 Value = "587",                             DefaultValue = "587",                             Description = "Puerto SMTP",                               Module = "EMAIL",          DataType = "int",    CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000016"), Key = "EMAIL.SMTP_USE_SSL",              Value = "true",                            DefaultValue = "true",                            Description = "Usar SSL para SMTP",                        Module = "EMAIL",          DataType = "bool",   CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000017"), Key = "EMAIL.SMTP_USERNAME",             Value = "",                                DefaultValue = "",                                Description = "Usuario SMTP",                              Module = "EMAIL",          DataType = "string", IsEncrypted = false, CreatedAt = now, RowVersion = [] },
            new SystemSetting { Id = new Guid("b0000001-0000-0000-0000-000000000018"), Key = "EMAIL.SMTP_PASSWORD",             Value = "",                                DefaultValue = "",                                Description = "Contraseña SMTP",                           Module = "EMAIL",          DataType = "string", IsEncrypted = true,  CreatedAt = now, RowVersion = [] }
        );
    }
}
