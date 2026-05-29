using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RepagroSuite.Application.Features.Auth.Services;
using RepagroSuite.Application.Features.Dashboard.Services;
using RepagroSuite.Application.Features.ITAssets.Services;
using RepagroSuite.Application.Features.Reservations.Services;
using RepagroSuite.Application.Features.Rooms.Services;
using RepagroSuite.Application.Features.Users.Services;

namespace RepagroSuite.Application.Extensions;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Módulo TI / Inventario
        services.AddScoped<IItAssetService, ItAssetService>();
        services.AddScoped<IItCatalogService, ItCatalogService>();
        services.AddScoped<IItTicketService, ItTicketService>();
        services.AddScoped<IItEmployeeService, ItEmployeeService>();
        services.AddScoped<IItImportService, ItImportService>();

        return services;
    }
}
