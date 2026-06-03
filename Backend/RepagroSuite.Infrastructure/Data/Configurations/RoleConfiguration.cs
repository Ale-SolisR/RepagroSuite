using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepagroSuite.Domain.Entities;

namespace RepagroSuite.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "CORE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Name).HasColumnName("Nombre").IsRequired().HasMaxLength(100);
        builder.Property(x => x.NormalizedName).HasColumnName("NombreNormalizado").IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.NormalizedName).IsUnique();
        builder.Property(x => x.Description).HasColumnName("Descripcion").HasMaxLength(300);
        builder.Property(x => x.IsSystemRole).HasColumnName("EsRolDelSistema");
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permisos", "CORE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Name).HasColumnName("Nombre").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).HasColumnName("Codigo").IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Description).HasColumnName("Descripcion").HasMaxLength(300);
        builder.Property(x => x.Module).HasColumnName("Modulo").HasMaxLength(50);
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UsuariosRoles", "CORE");
        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.Property(x => x.UserId).HasColumnName("UsuarioId");
        builder.Property(x => x.RoleId).HasColumnName("RolId");
        builder.Property(x => x.AssignedAt).HasColumnName("AsignadoEn");
        builder.Property(x => x.AssignedBy).HasColumnName("AsignadoPor");

        builder.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolesPermisos", "CORE");
        builder.HasKey(x => new { x.RoleId, x.PermissionId });

        builder.Property(x => x.RoleId).HasColumnName("RolId");
        builder.Property(x => x.PermissionId).HasColumnName("PermisoId");
        builder.Property(x => x.AssignedAt).HasColumnName("AsignadoEn");
        builder.Property(x => x.AssignedBy).HasColumnName("AsignadoPor");

        builder.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
    }
}
