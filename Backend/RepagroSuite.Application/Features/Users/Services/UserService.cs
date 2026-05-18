using RepagroSuite.Application.Common.Interfaces;
using RepagroSuite.Application.Common.Models;
using RepagroSuite.Application.Features.Users.DTOs;
using RepagroSuite.Domain.Entities;
using RepagroSuite.Domain.Enums;
using RepagroSuite.Domain.Interfaces;

namespace RepagroSuite.Application.Features.Users.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly IPasswordService _passwordService;
    private readonly IEmailService _emailService;
    private readonly IAuditService _auditService;
    private readonly IIdentificationLookupService _idLookup;

    public UserService(
        IUnitOfWork uow,
        IPasswordService passwordService,
        IEmailService emailService,
        IAuditService auditService,
        IIdentificationLookupService idLookup)
    {
        _uow = uow;
        _passwordService = passwordService;
        _emailService = emailService;
        _auditService = auditService;
        _idLookup = idLookup;
    }

    public async Task<UserDto> RegisterAsync(RegisterUserDto dto, CancellationToken cancellationToken = default)
    {
        var normalizedId = NormalizeId(dto.IdentificationNumber);
        var normalizedEmail = dto.Email.Trim().ToUpperInvariant();

        if (await _uow.Users.IdentificationExistsAsync(normalizedId, cancellationToken: cancellationToken))
            throw new InvalidOperationException("Ya existe un usuario registrado con esta cédula.");

        if (await _uow.Users.EmailExistsAsync(normalizedEmail, cancellationToken: cancellationToken))
            throw new InvalidOperationException("Ya existe un usuario registrado con este correo electrónico.");

        var idData = await _idLookup.LookupAsync(normalizedId, cancellationToken)
            ?? throw new InvalidOperationException("No se pudo validar la cédula ingresada. Verifique el número e intente nuevamente.");

        var user = new User
        {
            IdentificationType = idData.IdentificationType,
            IdentificationNumber = idData.IdentificationNumber,
            NormalizedIdentificationNumber = normalizedId,
            FullName = idData.FullName ?? string.Empty,
            FirstName = idData.FirstName,
            FirstName1 = idData.FirstName1,
            FirstName2 = idData.FirstName2,
            LastName = idData.LastName,
            LastName1 = idData.LastName1,
            LastName2 = idData.LastName2,
            LegalName = idData.LegalName,
            IdentificationValidated = true,
            IdentificationValidatedAt = DateTime.UtcNow,
            IdentificationValidationSource = idData.Source,
            Email = dto.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Department = dto.Department?.Trim(),
            Position = dto.Position?.Trim(),
            Status = UserStatus.Pending
        };

        await _uow.Users.AddAsync(user, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(user.Id, "USER_REGISTERED", entityName: "User", entityId: user.Id.ToString(), module: "Users");

        return MapToDto(user);
    }

    public async Task<UserDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetWithRolesAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");
        return MapToDto(user);
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize, string? search = null, UserStatus? status = null, CancellationToken cancellationToken = default)
    {
        var (items, total) = await _uow.Users.GetPagedAsync(page, pageSize, search, status, cancellationToken);
        return new PagedResult<UserDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDto> ApproveAsync(Guid userId, ApproveUserDto dto, Guid approvedBy, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetWithRolesAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (user.Status != UserStatus.Pending && user.Status != UserStatus.Rejected)
            throw new InvalidOperationException("Solo se pueden aprobar usuarios en estado Pendiente o Rechazado.");

        var tempPassword = _passwordService.GenerateTemporaryPassword();
        user.PasswordHash = _passwordService.HashPassword(tempPassword);
        user.Status = UserStatus.Active;
        user.MustChangePassword = true;
        user.TemporaryPasswordExpiresAt = DateTime.UtcNow.AddHours(72);
        user.ApprovedByUserId = approvedBy;
        user.ApprovedAt = DateTime.UtcNow;

        // Assign roles
        if (dto.RoleIds.Count > 0)
        {
            foreach (var roleId in dto.RoleIds)
            {
                if (!user.UserRoles.Any(ur => ur.RoleId == roleId))
                    user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId, AssignedAt = DateTime.UtcNow, AssignedBy = approvedBy });
            }
        }

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(approvedBy, "USER_APPROVED", entityName: "User", entityId: userId.ToString(), module: "Users");

        try
        {
            await _emailService.SendTemplateAsync(user.Email, "user_approved", new Dictionary<string, string>
            {
                ["fullName"] = user.FullName,
                ["email"] = user.Email,
                ["tempPassword"] = tempPassword,
                ["expiryHours"] = "72"
            }, cancellationToken);
        }
        catch { /* non-critical */ }

        return MapToDto(user);
    }

    public async Task<UserDto> RejectAsync(Guid userId, RejectUserDto dto, Guid rejectedBy, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (user.Status != UserStatus.Pending)
            throw new InvalidOperationException("Solo se pueden rechazar usuarios en estado Pendiente. Los usuarios aprobados deben ser inactivados.");

        user.Status = UserStatus.Rejected;
        user.RejectionReason = dto.Reason.Trim();
        user.RejectedByUserId = rejectedBy;
        user.RejectedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(rejectedBy, "USER_REJECTED", entityName: "User", entityId: userId.ToString(), module: "Users");

        try
        {
            await _emailService.SendTemplateAsync(user.Email, "user_rejected", new Dictionary<string, string>
            {
                ["fullName"] = user.FullName,
                ["reason"] = dto.Reason
            }, cancellationToken);
        }
        catch { /* non-critical */ }

        return MapToDto(user);
    }

    public async Task<UserDto> BlockAsync(Guid userId, Guid blockedBy, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (user.Id == MasterUserId)
            throw new InvalidOperationException("No se puede bloquear al administrador maestro del sistema.");

        user.Status = UserStatus.Blocked;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(blockedBy, "USER_BLOCKED", entityName: "User", entityId: userId.ToString(), module: "Users");
        return MapToDto(user);
    }

    public async Task<UserDto> UnblockAsync(Guid userId, Guid unblockedBy, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        user.Status = UserStatus.Active;
        user.FailedLoginAttempts = 0;
        user.LockoutEndAt = null;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(unblockedBy, "USER_UNBLOCKED", entityName: "User", entityId: userId.ToString(), module: "Users");
        return MapToDto(user);
    }

    public async Task<UserDto> UpdateAsync(Guid userId, UpdateUserDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        user.PhoneNumber = dto.PhoneNumber?.Trim();
        user.Department = dto.Department?.Trim();
        user.Position = dto.Position?.Trim();
        user.ProfileImageUrl = dto.ProfileImageUrl?.Trim();
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);
        return MapToDto(user);
    }

    public async Task<GenerateTemporaryPasswordResponseDto> GenerateTemporaryPasswordAsync(Guid userId, Guid adminId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetWithRolesAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        EnsureCanChangePasswordOf(user, adminId);

        var tempPassword = _passwordService.GenerateTemporaryPassword();
        user.PasswordHash = _passwordService.HashPassword(tempPassword);
        user.MustChangePassword = true;
        user.TemporaryPasswordExpiresAt = DateTime.UtcNow.AddHours(72);
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(adminId, "TEMP_PASSWORD_GENERATED", entityName: "User", entityId: userId.ToString(), module: "Users");

        try
        {
            await _emailService.SendTemplateAsync(user.Email, "user_approved", new Dictionary<string, string>
            {
                ["fullName"] = user.FullName,
                ["email"] = user.Email,
                ["tempPassword"] = tempPassword,
                ["expiryHours"] = "72"
            }, cancellationToken);
            return new GenerateTemporaryPasswordResponseDto { Sent = true, Message = "Contraseña temporal enviada al correo del usuario." };
        }
        catch
        {
            return new GenerateTemporaryPasswordResponseDto { Sent = false, Message = "Contraseña generada pero no se pudo enviar el correo." };
        }
    }

    public async Task ForcePasswordChangeAsync(Guid userId, Guid adminId, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (user.Id == MasterUserId)
            throw new InvalidOperationException("No se puede forzar el cambio de contraseña al administrador maestro del sistema.");

        user.MustChangePassword = true;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(adminId, "FORCE_PASSWORD_CHANGE", entityName: "User", entityId: userId.ToString(), module: "Users");
    }

    public async Task<UserDto> InactivateAsync(Guid userId, Guid inactivatedBy, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (user.Id == MasterUserId)
            throw new InvalidOperationException("No se puede inactivar al administrador maestro del sistema.");

        if (user.Status != UserStatus.Active && user.Status != UserStatus.Blocked)
            throw new InvalidOperationException("Solo se pueden inactivar usuarios en estado Activo o Bloqueado.");

        user.Status = UserStatus.Inactive;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(inactivatedBy, "USER_INACTIVATED", entityName: "User", entityId: userId.ToString(), module: "Users");
        return MapToDto(user);
    }

    public async Task<UserDto> PromoteToAdminAsync(Guid userId, Guid promotedBy, CancellationToken cancellationToken = default)
    {
        if (promotedBy != MasterUserId)
            throw new InvalidOperationException("Solo el administrador maestro puede designar nuevos administradores.");

        var user = await _uow.Users.GetWithRolesAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (user.Status != UserStatus.Active)
            throw new InvalidOperationException("Solo se pueden promover usuarios en estado Activo.");

        var adminRoleId = new Guid("11111111-1111-1111-1111-111111111111");
        if (user.UserRoles.Any(ur => ur.RoleId == adminRoleId))
            throw new InvalidOperationException("El usuario ya tiene el rol de Administrador.");

        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = adminRoleId, AssignedAt = DateTime.UtcNow, AssignedBy = promotedBy });
        await _uow.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(promotedBy, "USER_PROMOTED_ADMIN", entityName: "User", entityId: userId.ToString(), module: "Users");
        return MapToDto(user);
    }

    public async Task<UserDto> DemoteFromAdminAsync(Guid userId, Guid demotedBy, CancellationToken cancellationToken = default)
    {
        if (demotedBy != MasterUserId)
            throw new InvalidOperationException("Solo el administrador maestro puede remover el rol de Administrador.");

        var user = await _uow.Users.GetWithRolesAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (user.Id == MasterUserId)
            throw new InvalidOperationException("No se puede remover el rol de Administrador al usuario maestro del sistema.");

        var adminRoleId = new Guid("11111111-1111-1111-1111-111111111111");
        var userRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == adminRoleId)
            ?? throw new InvalidOperationException("El usuario no tiene el rol de Administrador.");

        user.UserRoles.Remove(userRole);
        await _uow.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(demotedBy, "USER_DEMOTED_ADMIN", entityName: "User", entityId: userId.ToString(), module: "Users");
        return MapToDto(user);
    }

    public async Task ChangeUserPasswordAsync(Guid userId, ChangeUserPasswordDto dto, Guid adminId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 8)
            throw new InvalidOperationException("La contraseña debe tener al menos 8 caracteres.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(dto.NewPassword, "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z\\d])"))
            throw new InvalidOperationException("La contraseña debe contener mayúscula, minúscula, número y carácter especial.");

        var user = await _uow.Users.GetWithRolesAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        EnsureCanChangePasswordOf(user, adminId);

        if (user.Id == adminId)
            throw new InvalidOperationException("Para cambiar tu propia contraseña, usa la sección de perfil.");

        user.PasswordHash = _passwordService.HashPassword(dto.NewPassword);
        user.MustChangePassword = true;
        user.TemporaryPasswordExpiresAt = DateTime.UtcNow.AddHours(72);
        user.LastPasswordChangedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(adminId, "USER_PASSWORD_CHANGED_BY_ADMIN", entityName: "User", entityId: userId.ToString(), module: "Users");
    }

    // Reglas de autorización para cambiar la contraseña de otro usuario:
    //   - El master puede cambiar la contraseña de cualquiera.
    //   - Cualquier otro admin solo puede cambiar contraseñas de usuarios normales (no admins, no master).
    private static void EnsureCanChangePasswordOf(User target, Guid actorId)
    {
        if (actorId == MasterUserId) return;

        if (target.Id == MasterUserId)
            throw new InvalidOperationException("Solo el administrador maestro puede cambiar su propia contraseña.");

        var adminRoleId = new Guid("11111111-1111-1111-1111-111111111111");
        if (target.UserRoles.Any(ur => ur.RoleId == adminRoleId) && target.Id != actorId)
            throw new InvalidOperationException("Solo el administrador maestro puede cambiar la contraseña de otros administradores.");
    }

    public async Task DeleteAsync(Guid userId, Guid deletedBy, CancellationToken cancellationToken = default)
    {
        var user = await _uow.Users.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException("Usuario no encontrado.");

        if (user.Id == MasterUserId)
            throw new InvalidOperationException("No se puede eliminar al administrador maestro del sistema.");

        _uow.Users.SoftDelete(user, deletedBy);
        await _uow.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(deletedBy, "USER_DELETED", entityName: "User", entityId: userId.ToString(), module: "Users");
    }

    private static readonly Guid MasterUserId = new("33333333-3333-3333-3333-333333333333");

    private static UserDto MapToDto(User user)
    {
        var isPhysical = user.IdentificationType == IdentificationType.PhysicalId;
        return new UserDto
        {
            Id = user.Id,
            IdentificationType = user.IdentificationType,
            IdentificationTypeName = isPhysical ? "Cédula física" : "Cédula jurídica",
            IdentificationNumber = user.IdentificationNumber,
            FullName = user.FullName,
            FirstName = user.FirstName,
            LastName = user.LastName,
            LegalName = user.LegalName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Department = user.Department,
            Position = user.Position,
            Status = user.Status,
            StatusName = user.Status switch
            {
                UserStatus.Pending => "Pendiente",
                UserStatus.Active => "Activo",
                UserStatus.Rejected => "Rechazado",
                UserStatus.Blocked => "Bloqueado",
                UserStatus.Inactive => "Inactivo",
                _ => user.Status.ToString()
            },
            ProfileImageUrl = user.ProfileImageUrl,
            MustChangePassword = user.MustChangePassword,
            IsMaster = user.Id == MasterUserId,
            LastLoginAt = user.LastLoginAt,
            LastPasswordChangedAt = user.LastPasswordChangedAt,
            Roles = user.UserRoles
                .Select(ur => ur.Role?.Name)
                .Where(name => name != null)
                .ToList()!,
            CreatedAt = user.CreatedAt
        };
    }

    private static string NormalizeId(string raw) => new string(raw.Where(char.IsDigit).ToArray());
}
