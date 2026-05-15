using RepagroSuite.Domain.Common;
using RepagroSuite.Domain.Enums;

namespace RepagroSuite.Domain.Entities;

public class User : BaseEntity
{
    public IdentificationType IdentificationType { get; set; }
    public string IdentificationNumber { get; set; } = string.Empty;
    public string NormalizedIdentificationNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? FirstName1 { get; set; }
    public string? FirstName2 { get; set; }
    public string? LastName { get; set; }
    public string? LastName1 { get; set; }
    public string? LastName2 { get; set; }
    public string? LegalName { get; set; }
    public bool IdentificationValidated { get; set; }
    public DateTime? IdentificationValidatedAt { get; set; }
    public string? IdentificationValidationSource { get; set; }
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
    public string? Position { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Pending;
    public string? ProfileImageUrl { get; set; }
    public string? PasswordHash { get; set; }
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastPasswordChangedAt { get; set; }
    public bool MustChangePassword { get; set; } = false;
    public DateTime? TemporaryPasswordExpiresAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresAt { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RejectedByUserId { get; set; }
    public DateTime? RejectedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<Reservation> Reservations { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
