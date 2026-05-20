using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Application.Features.Users.DTOs;

public class UserDto
{
    public Guid Id { get; set; }
    public IdentificationType IdentificationType { get; set; }
    public string IdentificationTypeName { get; set; } = string.Empty;
    public string IdentificationNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? LegalName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public UserStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public bool MustChangePassword { get; set; }
    public bool IsMaster { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastPasswordChangedAt { get; set; }
    public IEnumerable<string> Roles { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

public class RegisterUserDto
{
    public string IdentificationNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
}

public class CreateUserByAdminDto
{
    public string IdentificationNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public List<Guid> RoleIds { get; set; } = [];
    public bool SendTemporaryPassword { get; set; } = true;
}

public class UpdateUserDto
{
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? ProfileImageUrl { get; set; }
}

public class ApproveUserDto
{
    public string? Comment { get; set; }
}

public class RejectUserDto
{
    public string Reason { get; set; } = string.Empty;
}

public class GenerateTemporaryPasswordResponseDto
{
    public bool Sent { get; set; }
    public string? Message { get; set; }
}

public class ChangeUserPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}
