using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Rastreo.Api.Controllers;
using Rastreo.Api.Data;
using Rastreo.Api.Models;
using Rastreo.Api.Services;

namespace Rastreo.Api;

/// <summary>
/// Punto único de integración del módulo de Rastreo dentro del backend de Repagro Suite (Opción B).
///
/// Aislamiento garantizado:
///  • Esquema de autenticación PROPIO ("Rastreo") con CLAVE, Issuer y Audience DISTINTOS de Repagro,
///    leídos de la sección de configuración "Rastreo:Jwt". Un token de Repagro NO sirve para endpoints
///    de Rastreo y viceversa (firmas/audiencias distintas) → no se mezclan accesos.
///  • Identidad propia (RASTREO.Usuarios), DbContext propio y migraciones en el esquema RASTREO con
///    su propio historial. Este módulo nunca toca CORE/SALAS/SOPORTE.
///  • Sesión única (claim 'sid') preservada igual que en el sistema original.
/// </summary>
public static class RastreoModule
{
    /// <summary>Nombre del esquema de autenticación JWT exclusivo del módulo de Rastreo.</summary>
    public const string AuthScheme = "Rastreo";

    // ─── Servicios (DI) ───────────────────────────────────────────────────────────
    public static IServiceCollection AddRastreoModule(this IServiceCollection services, IConfiguration config)
    {
        // DbContext del esquema RASTREO (misma BD física, historial de migraciones propio).
        services.AddDbContext<RastreoDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "RASTREO")));

        services.AddSingleton<JwtService>();
        services.AddSingleton<ImageService>();
        services.AddSingleton<ExcelService>();
        services.AddSingleton<PdfService>();
        services.AddSingleton<InformePdfService>();
        services.AddSingleton<EmailService>();
        services.AddScoped<EvaluacionPulmonarService>();

        return services;
    }

    // ─── 2.º esquema JWT (aislado) ─────────────────────────────────────────────────
    public static AuthenticationBuilder AddRastreoJwt(this AuthenticationBuilder auth, IConfiguration config)
    {
        var jwtKey = config["Rastreo:Jwt:Key"]
            ?? throw new InvalidOperationException("Rastreo:Jwt:Key faltante (clave JWT del módulo de Rastreo).");

        return auth.AddJwtBearer(AuthScheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Rastreo:Jwt:Issuer"] ?? "Rastreo.Api",
                ValidAudience = config["Rastreo:Jwt:Audience"] ?? "Rastreo.Client",
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            options.Events = new JwtBearerEvents
            {
                // Acepta token por ?token= para que <img src="/api/...foto?token="> funcione.
                OnMessageReceived = ctx =>
                {
                    if (string.IsNullOrEmpty(ctx.Token))
                    {
                        var t = ctx.Request.Query["token"].FirstOrDefault();
                        if (!string.IsNullOrEmpty(t)) ctx.Token = t;
                    }
                    return Task.CompletedTask;
                },
                // Sesión única: el 'sid' del token debe coincidir con el SesionToken vigente del usuario.
                OnTokenValidated = async ctx =>
                {
                    var sid = ctx.Principal?.FindFirst("sid")?.Value;
                    var uid = ctx.Principal?.FindFirst("uid")?.Value;
                    if (!int.TryParse(uid, out var id) || string.IsNullOrEmpty(sid))
                    {
                        ctx.Fail("Token inválido");
                        return;
                    }
                    var db = ctx.HttpContext.RequestServices.GetRequiredService<RastreoDbContext>();
                    var u = await db.Usuarios.AsNoTracking()
                        .Where(x => x.Id == id)
                        .Select(x => new { x.Activo, x.SesionToken, x.SesionExpira })
                        .FirstOrDefaultAsync();
                    if (u is null || !u.Activo || u.SesionToken?.ToString() != sid)
                    {
                        ctx.Response.Headers["X-Session-Superseded"] = "1";
                        ctx.Fail("SESION_SUPERSEDED");
                        return;
                    }

                    var now = DateTime.UtcNow;
                    // Sesión DESLIZANTE: expira tras 3h SIN actividad. Cualquier request válida la extiende.
                    if (u.SesionExpira.HasValue && u.SesionExpira.Value <= now)
                    {
                        ctx.Response.Headers["X-Session-Expired"] = "1";
                        ctx.Fail("SESION_EXPIRADA_INACTIVIDAD");
                        return;
                    }
                    // Extiende la ventana a now+3h. Throttle: solo escribe si quedan < 2.5h
                    // (≈ máx. 1 escritura cada 30 min) para no pegar a la BD en cada request.
                    var ventana = TimeSpan.FromHours(AuthController.SesionInactividadHoras);
                    if (!u.SesionExpira.HasValue || u.SesionExpira.Value < now.Add(ventana) - TimeSpan.FromMinutes(30))
                    {
                        await db.Usuarios.Where(x => x.Id == id)
                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.SesionExpira, now.Add(ventana)));
                    }
                }
            };
        });
    }

    // ─── Static files de uploads legacy de Rastreo ─────────────────────────────────
    public static void UseRastreoUploads(this WebApplication app)
    {
        // Fotos legacy guardadas en disco (las nuevas viven en BD como binario). Se sirven bajo /uploads.
        var uploads = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads");
        Directory.CreateDirectory(uploads);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(uploads),
            RequestPath = "/uploads"
        });
    }

    // ─── Inicialización: migraciones (con baseline legacy) + seed ───────────────────
    public static void InitializeRastreoModule(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RastreoDbContext>();
        AplicarMigraciones(db);
        SeedData(db);
        SeedYCargarLogo(db);
    }

    private static void AplicarMigraciones(RastreoDbContext db)
    {
        bool Existe(string obj) =>
            db.Database.SqlQueryRaw<int>(
                $"SELECT CAST(CASE WHEN OBJECT_ID('{obj}','U') IS NOT NULL THEN 1 ELSE 0 END AS int) AS Value")
                .AsEnumerable().First() == 1;

        var historialExiste = Existe("RASTREO.__EFMigrationsHistory");
        var esquemaLegacy = !historialExiste && Existe("RASTREO.Registros");

        if (esquemaLegacy)
        {
            db.Database.ExecuteSqlRaw(@"
IF SCHEMA_ID('RASTREO') IS NULL EXEC('CREATE SCHEMA [RASTREO]');
IF OBJECT_ID('RASTREO.__EFMigrationsHistory','U') IS NULL
CREATE TABLE [RASTREO].[__EFMigrationsHistory](
    [MigrationId] nvarchar(150) NOT NULL,
    [ProductVersion] nvarchar(32) NOT NULL,
    CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId]));");
            foreach (var mig in db.Database.GetMigrations())
                db.Database.ExecuteSqlRaw(
                    "IF NOT EXISTS (SELECT 1 FROM [RASTREO].[__EFMigrationsHistory] WHERE [MigrationId] = {0}) " +
                    "INSERT INTO [RASTREO].[__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES ({0}, {1});",
                    mig, "9.0.0");
        }

        db.Database.Migrate();
    }

    private static void SeedYCargarLogo(RastreoDbContext db)
    {
        try
        {
            db.Database.ExecuteSqlRaw(@"
IF OBJECT_ID('RASTREO.ArchivosMarca','U') IS NULL
CREATE TABLE [RASTREO].[ArchivosMarca](
    [Clave] nvarchar(100) NOT NULL,
    [Datos] varbinary(max) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [Actualizado] datetime2 NOT NULL,
    CONSTRAINT [PK_ArchivosMarca] PRIMARY KEY ([Clave]));");

            var existe = db.Database
                .SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM RASTREO.ArchivosMarca WHERE Clave = 'logo-repagro'")
                .AsEnumerable().First() > 0;
            if (!existe)
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Assets", "repagro-logo.png");
                if (File.Exists(path))
                {
                    var bytes = File.ReadAllBytes(path);
                    db.Database.ExecuteSqlRaw(
                        "INSERT INTO RASTREO.ArchivosMarca (Clave, Datos, ContentType, Actualizado) VALUES ({0},{1},{2},SYSUTCDATETIME())",
                        "logo-repagro", bytes, "image/png");
                }
            }

            var conn = db.Database.GetDbConnection();
            var abrir = conn.State != System.Data.ConnectionState.Open;
            if (abrir) conn.Open();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Datos FROM RASTREO.ArchivosMarca WHERE Clave = 'logo-repagro'";
                if (cmd.ExecuteScalar() is byte[] logo)
                    BrandAssets.SetLogo(logo);
            }
            finally { if (abrir) conn.Close(); }
        }
        catch { /* el logo es opcional */ }
    }

    private static void SeedData(RastreoDbContext db)
    {
        var correo = "gestionwebrepagro@gmail.com";
        if (!db.Usuarios.Any(u => u.Correo == correo))
        {
            db.Usuarios.Add(new Usuario
            {
                Correo = correo,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123"),
                Nombre = "ADMINISTRADOR RASTREO",
                Activo = true,
                Rol = "ADMIN"
            });
        }

        var catalogo = new (string codigo, string nombre, string tipo, int orden)[]
        {
            ("SPES", "SPES", "SCORE_0_4", 1),
            ("ABSCESO_NODULO", "Absceso / Nódulo", "BOOL", 2),
            ("PERICARDIO_ENGROSADO", "Pericardio engrosado", "BOOL", 3),
            ("PERICARDITIS", "Pericarditis", "BOOL", 4),
            ("AGUDA_CRONICA", "Aguda / Crónica", "AGUDA_CRONICA", 5),
            ("MOCO", "Moco", "BOOL", 6),
        };
        foreach (var (codigo, nombre, tipo, orden) in catalogo)
        {
            if (!db.Enfermedades.Any(e => e.Codigo == codigo))
                db.Enfermedades.Add(new Enfermedad { Codigo = codigo, Nombre = nombre, TipoCampo = tipo, Orden = orden, Activo = true });
        }
        db.SaveChanges();

        var admin = db.Usuarios.FirstOrDefault(u => u.Correo == correo);
        if (admin != null)
        {
            if (admin.Rol != "ADMIN") { admin.Rol = "ADMIN"; db.SaveChanges(); }
            db.Database.ExecuteSqlRaw(
                "UPDATE RASTREO.Registros SET UsuarioId = {0} WHERE UsuarioId IS NULL", admin.Id);
        }
    }
}
