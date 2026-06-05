using FluentValidation;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RepagroSuite.Application.Extensions;
using RepagroSuite.Infrastructure.Data;
using RepagroSuite.Infrastructure.Extensions;
using Rastreo.Api;   // Módulo de Rastreo integrado en el host único (Opción B)
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Licencia QuestPDF (Community) — la usan tanto Repagro como el módulo de Rastreo. Idempotente.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Controllers + JSON
builder.Services.AddControllers()
    // Descubre los controladores del módulo de Rastreo (ensamblado RepagroSuite.Rastreo).
    .AddApplicationPart(typeof(Rastreo.Api.RastreoModule).Assembly)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version")
    );
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// CORS
//   - AllowedOrigins: lista exacta desde config (localhost en dev, dominio final cuando se conozca).
//   - AllowedOriginPatterns: regex (case-insensitive). Se usa para aceptar subdominios Netlify
//     mientras no haya un dominio fijo (cualquier preview/branch *.netlify.app).
//   - AllowCredentials() es obligatorio para que el navegador envíe la cookie httpOnly del refresh.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
var allowedOriginPatterns = builder.Configuration.GetSection("Cors:AllowedOriginPatterns").Get<string[]>() ?? [];
var allowedRegexes = allowedOriginPatterns
    .Select(p => new System.Text.RegularExpressions.Regex(p, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
        policy.SetIsOriginAllowed(origin =>
              {
                  if (allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;
                  return allowedRegexes.Any(r => r.IsMatch(origin));
              })
              .AllowAnyHeader()
              .AllowAnyMethod()
              // Permite que el frontend lea el nombre de archivo en descargas (PDF de boletas, Excel).
              .WithExposedHeaders("Content-Disposition")
              .AllowCredentials());
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            // SignalR WebSockets no soporta header Authorization: aceptar token por query.
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            },
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    success = false,
                    code = "UNAUTHORIZED",
                    message = "No autorizado. Inicie sesión para continuar."
                });
                return context.Response.WriteAsync(result);
            },
            OnForbidden = context =>
            {
                context.Response.StatusCode = 403;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    success = false,
                    code = "FORBIDDEN",
                    message = "No tiene permisos para realizar esta acción."
                });
                return context.Response.WriteAsync(result);
            }
        };
    })
    // 2.º esquema JWT, exclusivo del módulo de Rastreo (clave/issuer/audience propios → aislado).
    .AddRastreoJwt(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    var permissions = new[]
    {
        "Users.View", "Users.Approve", "Users.Reject", "Users.Block",
        "Users.GenerateTemporaryPassword", "Users.ForcePasswordChange",
        "Identifications.Lookup",
        "Rooms.View", "Rooms.Create", "Rooms.Update", "Rooms.Delete", "Rooms.Availability.Manage",
        "Reservations.View", "Reservations.Create", "Reservations.Approve",
        "Reservations.Reject", "Reservations.Cancel", "Reservations.DirectCreate",
        "AuditLogs.View", "Reports.View",
        "Settings.View", "Settings.Update", "Settings.Email.View", "Settings.Email.Update",
        "Settings.Email.Test", "Settings.Modules.View", "Settings.Modules.Create",
        "Settings.Modules.Update", "Settings.Modules.Delete", "Settings.Security.Manage",
        // Módulo TI / Inventario
        "Ti.Inventory.View", "Ti.Inventory.Create", "Ti.Inventory.Update", "Ti.Inventory.Delete",
        "Ti.Catalog.Manage", "Ti.Dashboard.View",
        "Ti.Assign", "Ti.Return", "Ti.Ticket.Create", "Ti.Ticket.Void", "Ti.Employee.Manage",
        "Ti.Asset.Reactivate",
        // Administración de usuarios del Sistema de Rastreo (esquema RASTREO, independientes de Repagro)
        "RastreoUsers.View", "RastreoUsers.Create", "RastreoUsers.ResetPassword",
        "RastreoUsers.ManageRole", "RastreoUsers.ManageStatus"
    };

    foreach (var perm in permissions)
        options.AddPolicy(perm, policy => policy.RequireClaim("permission", perm));
});

// Rate limiting — protege login/refresh/forgot-password contra fuerza bruta y abuso.
var authPerMinute = builder.Configuration.GetValue("RateLimit:AuthRequestsPerMinute", 5);
var refreshPerMinute = builder.Configuration.GetValue("RateLimit:RefreshRequestsPerMinute", 20);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        ctx.HttpContext.Response.ContentType = "application/json";
        await ctx.HttpContext.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            success = false,
            code = "RATE_LIMITED",
            message = "Demasiados intentos. Espere unos segundos e intente nuevamente."
        }), ct);
    };

    // Política para login y forgot-password: muy estricta, por IP.
    options.AddPolicy("auth-strict", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Política para refresh: más permisiva (un usuario activo puede refrescar varias veces).
    options.AddPolicy("auth-refresh", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = refreshPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RepagroSuite API",
        Version = "v1",
        Description = "API empresarial modular para gestión de salas y recursos internos."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT: Bearer {token}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

// Health Checks
//   /health/live  → ¿el proceso responde? (sin tocar BD; útil para Kubernetes livenessProbe)
//   /health/ready → ¿podemos servir tráfico? (verifica BD; readinessProbe)
//   /health       → conjunto (compat)
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<ApplicationDbContext>("database", tags: ["ready"]);

// Response compression — Brotli para clientes modernos, Gzip como fallback.
// El calendario y la lista de usuarios envían JSON grande; con compresión bajan ~60-70%.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes =
    [
        "application/json",
        "application/javascript",
        "text/css",
        "text/html",
        "text/plain",
        "text/json",
    ];
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

// SignalR + notificador
builder.Services.AddSignalR();
builder.Services.AddScoped<RepagroSuite.Application.Common.Interfaces.IRealtimeNotifier, RepagroSuite.API.Hubs.SignalRNotifier>();

// Application + Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Módulo de Rastreo (DbContext del esquema RASTREO + servicios propios). Identidad y datos aislados.
builder.Services.AddRastreoModule(builder.Configuration);

// Auto-aprobación de reservas pendientes dentro de la ventana de 30 min (proceso en segundo plano).
builder.Services.AddHostedService<RepagroSuite.API.BackgroundServices.ReservationAutoApprovalService>();

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(RepagroSuite.Application.Extensions.ApplicationExtensions).Assembly);

var app = builder.Build();

// CLI: importar inventario desde un JSON (uso: dotnet run -- import-ti <ruta.json>).
// Aplica migraciones, ejecuta el import y termina SIN levantar el web host.
if (args.Contains("import-ti"))
{
    await app.Services.ApplyMigrationsAsync();
    var path = args.SkipWhile(a => a != "import-ti").Skip(1).FirstOrDefault();
    if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
    {
        Console.Error.WriteLine($"[import-ti] Archivo no encontrado: {path}");
        return;
    }
    var rows = JsonSerializer.Deserialize<List<RepagroSuite.Application.Features.ITAssets.DTOs.ItAssetImportRow>>(
        await File.ReadAllTextAsync(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    using var importScope = app.Services.CreateScope();
    var importer = importScope.ServiceProvider.GetRequiredService<RepagroSuite.Application.Features.ITAssets.Services.IItImportService>();
    var adminId = new Guid("33333333-3333-3333-3333-333333333333");
    var res = await importer.ImportAssetsAsync(rows, adminId);
    Console.WriteLine($"[import-ti] Creados={res.Created} Omitidos(existentes)={res.SkippedExisting} Marcas+={res.BrandsCreated} Ubicaciones+={res.LocationsCreated} Departamentos+={res.DepartmentsCreated}");
    foreach (var w in res.Warnings) Console.WriteLine("  WARN: " + w);
    return;
}

// Migraciones en startup: SOLO si se opta explícitamente (flag de config) o en Development.
// En Production se recomienda correr `dotnet ef database update` desde CI/CD para evitar
// problemas con réplicas / blue-green (dos instancias intentando migrar a la vez).
var runMigrationsOnStartup = builder.Configuration.GetValue("Database:RunMigrationsOnStartup", app.Environment.IsDevelopment());
if (runMigrationsOnStartup)
{
    await app.Services.ApplyMigrationsAsync();
    // Migraciones (con baseline legacy) + seed del módulo de Rastreo, en su esquema RASTREO aislado.
    app.Services.InitializeRastreoModule();
}

// Correlation ID PRIMERO para que aparezca en todos los logs subsiguientes.
app.UseMiddleware<RepagroSuite.API.Middleware.CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        if (http.Items.TryGetValue("X-Correlation-Id", out var cid))
            diag.Set("CorrelationId", cid);
    };
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RepagroSuite API v1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    // En producción: HSTS para forzar HTTPS (1 año, incluye subdominios).
    app.UseHsts();
}

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseCors("DefaultCors");

// Fotos legacy del módulo de Rastreo servidas bajo /uploads (las nuevas viven en BD como binario).
app.UseRastreoUploads();

// Global error middleware
app.UseMiddleware<RepagroSuite.API.Middleware.GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<RepagroSuite.API.Hubs.AppHub>("/hubs/app");

// Health endpoints separados:
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live"),
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
});
app.MapHealthChecks("/health"); // conjunto, compat

app.Run();
