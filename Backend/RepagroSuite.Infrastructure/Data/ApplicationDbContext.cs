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

    // Módulo TI / Inventario de Activos Tecnológicos
    public DbSet<ItAsset> ItAssets => Set<ItAsset>();
    public DbSet<ItAssetSpec> ItAssetSpecs => Set<ItAssetSpec>();
    public DbSet<ItAssetType> ItAssetTypes => Set<ItAssetType>();
    public DbSet<ItBrand> ItBrands => Set<ItBrand>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ItLocation> ItLocations => Set<ItLocation>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<ItAssetHistory> ItAssetHistory => Set<ItAssetHistory>();
    public DbSet<ItAssetPhoto> ItAssetPhotos => Set<ItAssetPhoto>();
    public DbSet<ItTicket> ItTickets => Set<ItTicket>();
    public DbSet<ItTicketDetail> ItTicketDetails => Set<ItTicketDetail>();
    public DbSet<ItAssignment> ItAssignments => Set<ItAssignment>();
    public DbSet<ItTicketSignature> ItTicketSignatures => Set<ItTicketSignature>();
    public DbSet<ItTicketPhoto> ItTicketPhotos => Set<ItTicketPhoto>();
    public DbSet<ItDocumentSequence> ItDocumentSequences => Set<ItDocumentSequence>();
    public DbSet<ItEmployee> ItEmployees => Set<ItEmployee>();

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
            // --- Módulo TI / Inventario de Activos Tecnológicos ---
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000031"), Code = "Ti.Inventory.View",   Name = "Ver inventario TI",        Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000032"), Code = "Ti.Inventory.Create", Name = "Crear activos TI",         Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000033"), Code = "Ti.Inventory.Update", Name = "Editar activos TI",        Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000034"), Code = "Ti.Inventory.Delete", Name = "Eliminar activos TI",      Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000035"), Code = "Ti.Catalog.Manage",   Name = "Administrar catálogos TI", Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000036"), Code = "Ti.Dashboard.View",   Name = "Ver dashboard TI",         Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000037"), Code = "Ti.Assign",           Name = "Asignar activos TI",       Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000038"), Code = "Ti.Return",           Name = "Devolver activos TI",      Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000039"), Code = "Ti.Ticket.Create",    Name = "Crear boletas TI",         Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000040"), Code = "Ti.Ticket.Void",      Name = "Anular boletas TI",        Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000041"), Code = "Ti.Employee.Manage",   Name = "Gestionar colaboradores TI", Module = "TI", IsActive = true, CreatedAt = now, RowVersion = [] },
            // --- Administración de usuarios del Sistema de Rastreo (esquema RASTREO, independientes de Repagro) ---
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000042"), Code = "RastreoUsers.View",          Name = "Ver usuarios de rastreo",          Module = "Rastreo", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000043"), Code = "RastreoUsers.Create",        Name = "Crear usuarios de rastreo",        Module = "Rastreo", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000044"), Code = "RastreoUsers.ResetPassword", Name = "Restablecer contraseña de rastreo",Module = "Rastreo", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000045"), Code = "RastreoUsers.ManageRole",    Name = "Cambiar rol de usuario de rastreo",Module = "Rastreo", IsActive = true, CreatedAt = now, RowVersion = [] },
            new() { Id = new Guid("a0000001-0000-0000-0000-000000000046"), Code = "RastreoUsers.ManageStatus",  Name = "Activar/desactivar usuario rastreo",Module = "Rastreo", IsActive = true, CreatedAt = now, RowVersion = [] },
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
        modelBuilder.Entity<SystemModule>().HasData(
            new SystemModule
            {
                Id = new Guid("c0000001-0000-0000-0000-000000000001"),
                Name = "Gestión de Salas", Code = "ROOMS",
                Description = "Reservas y gestión de salas empresariales",
                IconName = "door-open", RoutePrefix = "/rooms", SortOrder = 1,
                IsActive = true, IsCore = true, Version = "1.0.0",
                CreatedAt = now, RowVersion = []
            },
            new SystemModule
            {
                Id = new Guid("c0000001-0000-0000-0000-000000000002"),
                Name = "Inventario TI", Code = "TI",
                Description = "Inventario de activos tecnológicos, asignaciones y boletas",
                IconName = "cpu", RoutePrefix = "/ti", SortOrder = 2,
                IsActive = true, IsCore = false, Version = "1.0.0",
                CreatedAt = now, RowVersion = []
            });

        // Catálogo base de tipos de activo TI (normaliza lo que en el Excel era texto libre).
        modelBuilder.Entity<ItAssetType>().HasData(
            NewType("d0000001-0000-0000-0000-000000000001", "Laptop",       "LAPTOP",  "laptop",         requiresSerial: true,  hasSpecs: true),
            NewType("d0000001-0000-0000-0000-000000000002", "Desktop",      "DESKTOP", "monitor",        requiresSerial: true,  hasSpecs: true),
            NewType("d0000001-0000-0000-0000-000000000003", "Tablet",       "TABLET",  "tablet",         requiresSerial: true,  hasSpecs: true),
            NewType("d0000001-0000-0000-0000-000000000004", "Celular",      "PHONE",   "smartphone",     requiresSerial: true,  hasSpecs: true),
            NewType("d0000001-0000-0000-0000-000000000005", "Impresora",    "PRINTER", "printer",        requiresSerial: true,  hasSpecs: false),
            NewType("d0000001-0000-0000-0000-000000000006", "Monitor",      "SCREEN",  "monitor",        requiresSerial: true,  hasSpecs: false),
            NewType("d0000001-0000-0000-0000-000000000007", "Cámara",       "CAMERA",  "camera",         requiresSerial: true,  hasSpecs: false),
            NewType("d0000001-0000-0000-0000-000000000008", "Switch",       "SWITCH",  "network",        requiresSerial: true,  hasSpecs: false),
            NewType("d0000001-0000-0000-0000-000000000009", "Access Point", "AP",      "wifi",           requiresSerial: true,  hasSpecs: false),
            NewType("d0000001-0000-0000-0000-000000000010", "UPS",          "UPS",     "battery",        requiresSerial: true,  hasSpecs: false),
            NewType("d0000001-0000-0000-0000-000000000011", "Servidor",     "SERVER",  "server",         requiresSerial: true,  hasSpecs: true),
            NewType("d0000001-0000-0000-0000-000000000012", "Equipo de red","NETDEV",  "router",         requiresSerial: true,  hasSpecs: false),
            NewType("d0000001-0000-0000-0000-000000000013", "Licencia",     "LICENSE", "key",            requiresSerial: false, hasSpecs: false, assignable: false),
            NewType("d0000001-0000-0000-0000-000000000014", "Accesorio",    "ACCESS",  "mouse-pointer",  requiresSerial: false, hasSpecs: false),
            NewType("d0000001-0000-0000-0000-000000000015", "Otro",         "OTHER",   "box",            requiresSerial: false, hasSpecs: false));

        // Consecutivos 2026 pre-creados por tipo (evita la condición de carrera del INSERT inicial; §10).
        var seqCodes = new[] { "ENT", "DEV", "PRE", "MAN", "REP", "TRA", "CRE", "ACC", "BAJ" };
        modelBuilder.Entity<ItDocumentSequence>().HasData(
            seqCodes.Select((code, i) => new ItDocumentSequence
            {
                Id = new Guid($"e0000001-0000-0000-0000-0000000000{(i + 1):D2}"),
                TicketTypeCode = code, Year = 2026, Prefix = "TI", LastNumber = 0,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), RowVersion = []
            }));

        static ItAssetType NewType(string id, string name, string code, string icon,
            bool requiresSerial, bool hasSpecs, bool assignable = true) => new()
        {
            Id = new Guid(id), Name = name, Code = code, IconName = icon,
            RequiresSerial = requiresSerial, HasComputeSpecs = hasSpecs, IsAssignable = assignable,
            IsActive = true, CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), RowVersion = []
        };

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
