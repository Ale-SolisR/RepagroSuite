using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepagroSuite.Domain.Entities;

namespace RepagroSuite.Infrastructure.Data.Configurations;

public class ItTicketConfiguration : IEntityTypeConfiguration<ItTicket>
{
    public void Configure(EntityTypeBuilder<ItTicket> builder)
    {
        builder.ToTable("Boletas", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.TicketNumber).HasColumnName("Consecutivo").IsRequired().HasMaxLength(30);
        builder.HasIndex(x => x.TicketNumber).IsUnique();
        builder.Property(x => x.TicketType).HasColumnName("TipoBoleta");
        builder.Property(x => x.Status).HasColumnName("Estado");
        builder.HasIndex(x => new { x.TicketType, x.Status });
        builder.Property(x => x.IssuedAt).HasColumnName("EmitidaEn");
        builder.HasIndex(x => x.IssuedAt);

        builder.Property(x => x.EmployeeId).HasColumnName("ColaboradorId");
        builder.Property(x => x.ItResponsibleUserId).HasColumnName("ResponsableTiId");
        builder.Property(x => x.Notes).HasColumnName("Observaciones").HasMaxLength(2000);

        builder.Property(x => x.PdfBase64).HasColumnName("PdfBase64").HasColumnType("nvarchar(max)");
        builder.Property(x => x.PdfSha256).HasColumnName("PdfSha256").HasMaxLength(64);

        builder.Property(x => x.VoidedBy).HasColumnName("AnuladaPor");
        builder.Property(x => x.VoidedAt).HasColumnName("AnuladaEn");
        builder.Property(x => x.VoidReason).HasColumnName("MotivoAnulacion").HasMaxLength(500);

        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ItResponsible).WithMany().HasForeignKey(x => x.ItResponsibleUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Details).WithOne(d => d.Ticket).HasForeignKey(d => d.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Photos).WithOne(p => p.Ticket).HasForeignKey(p => p.TicketId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Signatures).WithOne(s => s.Ticket).HasForeignKey(s => s.TicketId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItTicketDetailConfiguration : IEntityTypeConfiguration<ItTicketDetail>
{
    public void Configure(EntityTypeBuilder<ItTicketDetail> builder)
    {
        builder.ToTable("DetalleBoleta", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.TicketId).HasColumnName("BoletaId");
        builder.Property(x => x.AssetId).HasColumnName("ActivoId");
        builder.Property(x => x.LineType).HasColumnName("TipoLinea").HasMaxLength(20);
        builder.Property(x => x.Description).HasColumnName("Descripcion").HasMaxLength(300);
        builder.Property(x => x.Quantity).HasColumnName("Cantidad");
        builder.Property(x => x.Condition).HasColumnName("Condicion").HasMaxLength(100);

        builder.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItAssignmentConfiguration : IEntityTypeConfiguration<ItAssignment>
{
    public void Configure(EntityTypeBuilder<ItAssignment> builder)
    {
        builder.ToTable("Asignaciones", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.AssetId).HasColumnName("ActivoId");
        builder.Property(x => x.EmployeeId).HasColumnName("ColaboradorId");
        builder.Property(x => x.AssignedTicketId).HasColumnName("BoletaEntregaId");
        builder.Property(x => x.ReturnTicketId).HasColumnName("BoletaDevolucionId");
        builder.Property(x => x.AssignedAt).HasColumnName("AsignadoEn");
        builder.Property(x => x.ReturnedAt).HasColumnName("DevueltoEn");
        builder.Property(x => x.ConditionOut).HasColumnName("EstadoFisicoEntrega");
        builder.Property(x => x.ConditionIn).HasColumnName("EstadoFisicoRecepcion");
        builder.Property(x => x.Status).HasColumnName("Estado");
        builder.Property(x => x.Accessories).HasColumnName("Accesorios").HasMaxLength(500);
        builder.Property(x => x.ReturnNotes).HasColumnName("ObservacionesDevolucion").HasMaxLength(1000);

        // Una sola asignación activa por activo (Estado Activa = 0).
        builder.HasIndex(x => x.AssetId).IsUnique().HasFilter("[Estado] = 0 AND [EliminadoLogico] = 0");

        builder.HasOne(x => x.Asset).WithMany().HasForeignKey(x => x.AssetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AssignedTicket).WithMany().HasForeignKey(x => x.AssignedTicketId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReturnTicket).WithMany().HasForeignKey(x => x.ReturnTicketId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItTicketSignatureConfiguration : IEntityTypeConfiguration<ItTicketSignature>
{
    public void Configure(EntityTypeBuilder<ItTicketSignature> builder)
    {
        builder.ToTable("FirmasBoleta", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.TicketId).HasColumnName("BoletaId");
        builder.Property(x => x.SignerType).HasColumnName("TipoFirmante").HasMaxLength(30);
        builder.Property(x => x.SignerName).HasColumnName("NombreFirmante").HasMaxLength(150);
        builder.Property(x => x.ImageBase64).HasColumnName("ImagenBase64").HasColumnType("nvarchar(max)");
        builder.Property(x => x.Sha256).HasColumnName("Sha256").HasMaxLength(64);
        builder.Property(x => x.SignedAt).HasColumnName("FirmadoEn");
        builder.Property(x => x.IpAddress).HasColumnName("DireccionIp").HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasColumnName("UserAgent").HasMaxLength(400);
        builder.Property(x => x.AuthenticatedUserId).HasColumnName("UsuarioAutenticadoId");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItTicketPhotoConfiguration : IEntityTypeConfiguration<ItTicketPhoto>
{
    public void Configure(EntityTypeBuilder<ItTicketPhoto> builder)
    {
        builder.ToTable("FotosBoleta", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.TicketId).HasColumnName("BoletaId");
        builder.Property(x => x.AssetId).HasColumnName("ActivoId");
        builder.Property(x => x.ImageBase64).HasColumnName("ImagenBase64").HasColumnType("nvarchar(max)");
        builder.Property(x => x.MimeType).HasColumnName("MimeType").HasMaxLength(40);
        builder.Property(x => x.SizeBytes).HasColumnName("PesoBytes");
        builder.Property(x => x.Sha256).HasColumnName("Sha256").HasMaxLength(64);
        builder.Property(x => x.UploadedBy).HasColumnName("SubidoPor");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItDocumentSequenceConfiguration : IEntityTypeConfiguration<ItDocumentSequence>
{
    public void Configure(EntityTypeBuilder<ItDocumentSequence> builder)
    {
        builder.ToTable("ConsecutivosDocumento", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.TicketTypeCode).HasColumnName("CodigoTipo").IsRequired().HasMaxLength(5);
        builder.Property(x => x.Year).HasColumnName("Anio");
        builder.Property(x => x.Prefix).HasColumnName("Prefijo").HasMaxLength(10);
        builder.Property(x => x.LastNumber).HasColumnName("UltimoNumero");
        builder.HasIndex(x => new { x.TicketTypeCode, x.Year }).IsUnique().HasFilter("[EliminadoLogico] = 0");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
