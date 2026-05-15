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

        services.AddHttpClient("GoMeta", client =>
        {
            client.BaseAddress = new Uri("https://apis.gometa.org/cedulas/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });

        services.AddHttpContextAccessor();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IIdentificationLookupService, IdentificationLookupService>();
        services.AddScoped<RepagroSuite.Application.Features.Settings.Services.ISettingsService, SettingsService>();

        return services;
    }

    public static async Task ApplyMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
    }
}
