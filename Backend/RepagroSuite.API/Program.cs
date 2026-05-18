using FluentValidation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RepagroSuite.Application.Extensions;
using RepagroSuite.Infrastructure.Data;
using RepagroSuite.Infrastructure.Extensions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();
builder.Host.UseSerilog();

// Controllers + JSON
builder.Services.AddControllers()
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
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
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
    });

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
        "Settings.Modules.Update", "Settings.Modules.Delete", "Settings.Security.Manage"
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
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// SignalR + notificador
builder.Services.AddSignalR();
builder.Services.AddScoped<RepagroSuite.Application.Common.Interfaces.IRealtimeNotifier, RepagroSuite.API.Hubs.SignalRNotifier>();

// Application + Infrastructure DI
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(RepagroSuite.Application.Extensions.ApplicationExtensions).Assembly);

var app = builder.Build();

// Migrations on startup
await app.Services.ApplyMigrationsAsync();

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

app.UseHttpsRedirection();
app.UseCors("DefaultCors");

// Global error middleware
app.UseMiddleware<RepagroSuite.API.Middleware.GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapHub<RepagroSuite.API.Hubs.AppHub>("/hubs/app");
app.MapHealthChecks("/health");

app.Run();
