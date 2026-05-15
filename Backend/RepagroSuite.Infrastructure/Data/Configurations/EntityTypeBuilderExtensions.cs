using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepagroSuite.Domain.Common;

namespace RepagroSuite.Infrastructure.Data.Configurations;

internal static class EntityTypeBuilderExtensions
{
    internal static void MapBaseEntityColumns<T>(this EntityTypeBuilder<T> builder) where T : BaseEntity
    {
        builder.Property(e => e.Id).HasColumnName("Id");
        builder.Property(e => e.CreatedAt).HasColumnName("CreadoEn");
        builder.Property(e => e.CreatedBy).HasColumnName("CreadoPor");
        builder.Property(e => e.UpdatedAt).HasColumnName("ActualizadoEn");
        builder.Property(e => e.UpdatedBy).HasColumnName("ActualizadoPor");
        builder.Property(e => e.IsDeleted).HasColumnName("EliminadoLogico");
        builder.Property(e => e.DeletedAt).HasColumnName("EliminadoEn");
        builder.Property(e => e.DeletedBy).HasColumnName("EliminadoPor");
        builder.Property(e => e.RowVersion).HasColumnName("VersionFila");
    }
}
