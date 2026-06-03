using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepagroSuite.Domain.Entities;

namespace RepagroSuite.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Usuarios", "CORE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.IdentificationType).HasColumnName("TipoIdentificacion");
        builder.Property(x => x.IdentificationNumber).HasColumnName("NumeroIdentificacion").IsRequired().HasMaxLength(20);
        builder.Property(x => x.NormalizedIdentificationNumber).HasColumnName("NumeroIdentificacionNorm").IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.NormalizedIdentificationNumber).IsUnique().HasFilter("[EliminadoLogico] = 0");

        builder.Property(x => x.FullName).HasColumnName("NombreCompleto").IsRequired().HasMaxLength(200);
        builder.Property(x => x.FirstName).HasColumnName("NombresPropios").HasMaxLength(100);
        builder.Property(x => x.FirstName1).HasColumnName("PrimerNombre").HasMaxLength(60);
        builder.Property(x => x.FirstName2).HasColumnName("SegundoNombre").HasMaxLength(60);
        builder.Property(x => x.LastName).HasColumnName("Apellidos").HasMaxLength(120);
        builder.Property(x => x.LastName1).HasColumnName("PrimerApellido").HasMaxLength(60);
        builder.Property(x => x.LastName2).HasColumnName("SegundoApellido").HasMaxLength(60);
        builder.Property(x => x.LegalName).HasColumnName("NombreLegal").HasMaxLength(200);

        builder.Property(x => x.IdentificationValidated).HasColumnName("IdentificacionValidada");
        builder.Property(x => x.IdentificationValidatedAt).HasColumnName("IdentificacionValidadaEn");
        builder.Property(x => x.IdentificationValidationSource).HasColumnName("FuenteValidacion").HasMaxLength(50);

        builder.Property(x => x.Email).HasColumnName("CorreoElectronico").IsRequired().HasMaxLength(254);
        builder.Property(x => x.NormalizedEmail).HasColumnName("CorreoElectronicoNorm").IsRequired().HasMaxLength(254);
        builder.HasIndex(x => x.NormalizedEmail).IsUnique().HasFilter("[EliminadoLogico] = 0");

        builder.Property(x => x.PhoneNumber).HasColumnName("Telefono").HasMaxLength(20);
        builder.Property(x => x.Department).HasColumnName("Departamento").HasMaxLength(100);
        builder.Property(x => x.Position).HasColumnName("Puesto").HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("Estado");
        builder.Property(x => x.ProfileImageUrl).HasColumnName("UrlImagenPerfil").HasMaxLength(500);

        builder.Property(x => x.PasswordHash).HasColumnName("HashContrasena").HasMaxLength(500);
        builder.Property(x => x.FailedLoginAttempts).HasColumnName("IntentosLoginFallidos");
        builder.Property(x => x.LockoutEndAt).HasColumnName("BloqueoHasta");
        builder.Property(x => x.LastLoginAt).HasColumnName("UltimoAccesoEn");
        builder.Property(x => x.LastPasswordChangedAt).HasColumnName("UltimoCambioContrasenaEn");
        builder.Property(x => x.MustChangePassword).HasColumnName("DebeActualizarContrasena");
        builder.Property(x => x.TemporaryPasswordExpiresAt).HasColumnName("VencimientoContrasenaTemp");
        builder.Property(x => x.PasswordResetToken).HasColumnName("TokenRestablecimiento").HasMaxLength(500);
        builder.Property(x => x.PasswordResetTokenExpiresAt).HasColumnName("VencimientoTokenRestablecimiento");

        builder.Property(x => x.RejectionReason).HasColumnName("MotivoRechazo").HasMaxLength(500);
        builder.Property(x => x.ApprovedByUserId).HasColumnName("AprobadoPorId");
        builder.Property(x => x.ApprovedAt).HasColumnName("AprobadoEn");
        builder.Property(x => x.RejectedByUserId).HasColumnName("RechazadoPorId");
        builder.Property(x => x.RejectedAt).HasColumnName("RechazadoEn");

        builder.HasMany(x => x.UserRoles).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.RefreshTokens).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.AuditLogs).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(x => x.Notifications).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Reservations).WithOne(x => x.User).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
