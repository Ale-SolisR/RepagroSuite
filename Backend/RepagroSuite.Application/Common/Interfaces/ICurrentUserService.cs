namespace RepagroSuite.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    bool HasPermission(string permission);
}
