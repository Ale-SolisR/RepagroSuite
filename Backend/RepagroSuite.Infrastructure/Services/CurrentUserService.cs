using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RepagroSuite.Application.Common.Interfaces;

namespace RepagroSuite.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var id = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(id, out var guid) ? guid : null;
        }
    }

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public IEnumerable<string> Roles
        => User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? [];

    public IEnumerable<string> Permissions
        => User?.FindAll("permission").Select(c => c.Value) ?? [];

    public string? IpAddress
        => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public string? UserAgent
        => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].FirstOrDefault();

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => User?.IsInRole(role) == true;

    public bool HasPermission(string permission)
        => User?.FindAll("permission").Any(c => c.Value == permission) == true;
}
