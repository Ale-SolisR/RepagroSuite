using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepagroSuite.Domain.Entities;

namespace RepagroSuite.Infrastructure.Data.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservas", "SALAS");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.RoomId).HasColumnName("SalaId");
        builder.Property(x => x.UserId).HasColumnName("UsuarioId");
        builder.Property(x => x.StartDateTime).HasColumnName("FechaHoraInicio");
        builder.Property(x => x.EndDateTime).HasColumnName("FechaHoraFin");
        builder.Property(x => x.PeopleCount).HasColumnName("CantidadPersonas");
        builder.Property(x => x.Purpose).HasColumnName("Proposito").IsRequired().HasMaxLength(300);
        builder.Property(x => x.Notes).HasColumnName("Notas").HasMaxLength(1000);
        builder.Property(x => x.Status).HasColumnName("Estado");
        builder.Property(x => x.AdminComment).HasColumnName("ComentarioAdmin").HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasColumnName("MotivoCancelacion").HasMaxLength(500);
        builder.Property(x => x.IsDirectAdminReservation).HasColumnName("EsReservaDirectaAdmin");
        builder.Property(x => x.RecurrenceGroupId).HasColumnName("GrupoRecurrenciaId");
        builder.HasIndex(x => x.RecurrenceGroupId);

        builder.Property(x => x.ApprovedByUserId).HasColumnName("AprobadoPorId");
        builder.Property(x => x.ApprovedAt).HasColumnName("AprobadoEn");
        builder.Property(x => x.RejectedByUserId).HasColumnName("RechazadoPorId");
        builder.Property(x => x.RejectedAt).HasColumnName("RechazadoEn");
        builder.Property(x => x.CancelledByUserId).HasColumnName("CanceladoPorId");
        builder.Property(x => x.CancelledAt).HasColumnName("CanceladoEn");

        builder.HasIndex(x => new { x.RoomId, x.StartDateTime, x.EndDateTime, x.Status });
        builder.HasIndex(x => x.UserId);
        // Acelera "mis reservas" filtradas por estado y ordenadas por fecha (consulta caliente).
        builder.HasIndex(x => new { x.UserId, x.Status, x.StartDateTime })
            .HasDatabaseName("IX_Reservas_UsuarioId_Estado_FechaHoraInicio");

        builder.HasOne(x => x.Room).WithMany(x => x.Reservations).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.User).WithMany(x => x.Reservations).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("TokensRenovacion", "CORE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.UserId).HasColumnName("UsuarioId");
        builder.Property(x => x.Token).HasColumnName("Token").IsRequired().HasMaxLength(500);
        builder.HasIndex(x => x.Token).IsUnique();
        builder.Property(x => x.ExpiresAt).HasColumnName("VenceEn");
        builder.Property(x => x.IsRevoked).HasColumnName("EstaRevocado");
        builder.Property(x => x.RevokedAt).HasColumnName("RevocadoEn");
        builder.Property(x => x.RevokedReason).HasColumnName("MotivoRevocacion").HasMaxLength(200);
        builder.Property(x => x.ReplacedByToken).HasColumnName("ReemplazadoPorToken").HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasColumnName("DireccionIp").HasMaxLength(50);
        builder.Property(x => x.UserAgent).HasColumnName("AgenteUsuario").HasMaxLength(500);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("RegistrosAuditoria", "CORE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.UserId).HasColumnName("UsuarioId");
        builder.Property(x => x.Action).HasColumnName("Accion").IsRequired().HasMaxLength(100);
        builder.Property(x => x.EntityName).HasColumnName("NombreEntidad").HasMaxLength(100);
        builder.Property(x => x.EntityId).HasColumnName("IdEntidad").HasMaxLength(50);
        builder.Property(x => x.OldValues).HasColumnName("ValoresAnteriores");
        builder.Property(x => x.NewValues).HasColumnName("ValoresNuevos");
        builder.Property(x => x.Module).HasColumnName("Modulo").HasMaxLength(50);
        builder.Property(x => x.IpAddress).HasColumnName("DireccionIp").HasMaxLength(50);
        builder.Property(x => x.UserAgent).HasColumnName("AgenteUsuario").HasMaxLength(500);
        builder.Property(x => x.Success).HasColumnName("Exitoso");
        builder.Property(x => x.ErrorMessage).HasColumnName("MensajeError").HasMaxLength(1000);
        builder.Property(x => x.Timestamp).HasColumnName("FechaHoraRegistro");

        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.UserId);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notificaciones", "CORE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.UserId).HasColumnName("UsuarioId");
        builder.Property(x => x.Title).HasColumnName("Titulo").IsRequired().HasMaxLength(200);
        builder.Property(x => x.Body).HasColumnName("Contenido").IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Type).HasColumnName("Tipo");
        builder.Property(x => x.Status).HasColumnName("Estado");
        builder.Property(x => x.SentAt).HasColumnName("EnviadoEn");
        builder.Property(x => x.ReadAt).HasColumnName("LeidoEn");
        builder.Property(x => x.Module).HasColumnName("Modulo").HasMaxLength(50);
        builder.Property(x => x.RelatedEntityId).HasColumnName("IdEntidadRelacionada").HasMaxLength(50);
        builder.Property(x => x.RelatedEntityType).HasColumnName("TipoEntidadRelacionada").HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasColumnName("MensajeError").HasMaxLength(500);
        builder.Property(x => x.RetryCount).HasColumnName("IntentosReintento");

        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("ConfiguracionSistema", "CORE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Key).HasColumnName("Clave").IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.Value).HasColumnName("Valor").HasMaxLength(2000);
        builder.Property(x => x.DefaultValue).HasColumnName("ValorPredeterminado").HasMaxLength(2000);
        builder.Property(x => x.Description).HasColumnName("Descripcion").HasMaxLength(300);
        builder.Property(x => x.Module).HasColumnName("Modulo").HasMaxLength(50);
        builder.Property(x => x.DataType).HasColumnName("TipoDato").HasMaxLength(30);
        builder.Property(x => x.IsEncrypted).HasColumnName("EstaEncriptado");
        builder.Property(x => x.IsReadOnly).HasColumnName("EsDeSoloLectura");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class SystemModuleConfiguration : IEntityTypeConfiguration<SystemModule>
{
    public void Configure(EntityTypeBuilder<SystemModule> builder)
    {
        builder.ToTable("ModulosSistema", "CORE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Name).HasColumnName("Nombre").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).HasColumnName("Codigo").IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.Description).HasColumnName("Descripcion").HasMaxLength(300);
        builder.Property(x => x.IconName).HasColumnName("NombreIcono").HasMaxLength(50);
        builder.Property(x => x.RoutePrefix).HasColumnName("PrefijoRuta").HasMaxLength(100);
        builder.Property(x => x.SortOrder).HasColumnName("OrdenVisualizacion");
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");
        builder.Property(x => x.IsCore).HasColumnName("EsNuclear");
        builder.Property(x => x.Version).HasColumnName("Version").HasMaxLength(20);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class IdentificationLookupCacheConfiguration : IEntityTypeConfiguration<IdentificationLookupCache>
{
    public void Configure(EntityTypeBuilder<IdentificationLookupCache> builder)
    {
        builder.ToTable("CacheIdentificaciones", "CORE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.IdentificationType).HasColumnName("TipoIdentificacion");
        builder.Property(x => x.IdentificationNumber).HasColumnName("NumeroIdentificacion").IsRequired().HasMaxLength(20);
        builder.Property(x => x.NormalizedIdentificationNumber).HasColumnName("NumeroIdentificacionNorm").IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.NormalizedIdentificationNumber).IsUnique();

        builder.Property(x => x.FullName).HasColumnName("NombreCompleto").HasMaxLength(200);
        builder.Property(x => x.FirstName).HasColumnName("NombresPropios").HasMaxLength(100);
        builder.Property(x => x.FirstName1).HasColumnName("PrimerNombre").HasMaxLength(60);
        builder.Property(x => x.FirstName2).HasColumnName("SegundoNombre").HasMaxLength(60);
        builder.Property(x => x.LastName).HasColumnName("Apellidos").HasMaxLength(120);
        builder.Property(x => x.LastName1).HasColumnName("PrimerApellido").HasMaxLength(60);
        builder.Property(x => x.LastName2).HasColumnName("SegundoApellido").HasMaxLength(60);
        builder.Property(x => x.LegalName).HasColumnName("NombreLegal").HasMaxLength(200);

        builder.Property(x => x.Source).HasColumnName("Fuente").HasMaxLength(50);
        builder.Property(x => x.RawResponseJson).HasColumnName("RespuestaJsonRaw");
        builder.Property(x => x.ResultCount).HasColumnName("CantidadResultados");
        builder.Property(x => x.DatabaseDate).HasColumnName("FechaBaseDatos").HasMaxLength(20);
        builder.Property(x => x.LastLookupAt).HasColumnName("UltimaConsultaEn");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
