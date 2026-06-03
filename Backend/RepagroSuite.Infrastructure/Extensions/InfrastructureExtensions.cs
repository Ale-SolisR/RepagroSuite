using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Domain.Interfaces;
using RepagroSuite.Infrastructure.Data;
using RepagroSuite.Infrastructure.Services;

namespace RepagroSuite.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                          .EnableRetryOnFailure(3)
            )
        );

        // DbContext ACOTADO al esquema RASTREO (misma BD) — solo para administrar usuarios del
        // sistema de Rastreo. Sin MigrationsAssembly a propósito: este contexto NUNCA migra ni
        // altera el esquema RASTREO; solo lee/escribe la tabla Usuarios. Ver RastreoDbContext.
        services.AddDbContext<Data.Rastreo.RastreoDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3)
            )
        );

        // GoMeta es un proveedor externo: retry + circuit breaker para resiliencia.
        // - Retry: hasta 2 reintentos con backoff exponencial en fallas transitorias (5xx, timeouts).
        // - Circuit breaker: si falla el 50% de >=8 requests en 30s, abre el circuito 30s
        //   (evita que el panel de registro de usuarios cuelgue cuando GoMeta está caído).
        services.AddHttpClient("GoMeta", client =>
        {
            client.BaseAddress = new Uri("https://apis.gometa.org/cedulas/");
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromMilliseconds(300);
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            options.CircuitBreaker.MinimumThroughput = 8;
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(8);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(25);
        });

        services.AddHttpContextAccessor();

        // Data Protection — persiste llaves para que sobrevivan a reinicios.
        // En clúster: cambiar a PersistKeysToAzureBlobStorage / SQL para que las réplicas compartan llaves.
        services.AddDataProtection()
            .SetApplicationName("RepagroSuite")
            .PersistKeysToFileSystem(new DirectoryInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RepagroSuite", "dp-keys")));

        services.AddMemoryCache();
        services.AddSingleton<IAppCache, MemoryAppCache>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<ISecretProtector, SecretProtector>();
        services.AddSingleton<IEmailQueue, InMemoryEmailQueue>();
        services.AddHostedService<EmailWorker>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IIdentificationLookupService, IdentificationLookupService>();
        services.AddScoped<RepagroSuite.Application.Features.Settings.Services.ISettingsService, SettingsService>();

        // Administración de usuarios del sistema de Rastreo (esquema RASTREO, misma BD).
        services.AddScoped<RepagroSuite.Application.Features.RastreoUsers.Services.IRastreoUserAdminService, RastreoUserAdminService>();

        // Módulo TI — boletas
        services.AddScoped<ISequenceGenerator, SequenceGenerator>();
        services.AddSingleton<IPdfGenerator, PdfGenerator>();
        services.AddSingleton<IItExcelExporter, ItExcelExporter>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
}
