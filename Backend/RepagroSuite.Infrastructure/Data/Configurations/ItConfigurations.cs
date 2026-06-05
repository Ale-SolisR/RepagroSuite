using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepagroSuite.Domain.Entities;

namespace RepagroSuite.Infrastructure.Data.Configurations;

public class ItAssetTypeConfiguration : IEntityTypeConfiguration<ItAssetType>
{
    public void Configure(EntityTypeBuilder<ItAssetType> builder)
    {
        builder.ToTable("TiposActivo", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Name).HasColumnName("Nombre").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).HasColumnName("Codigo").IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[EliminadoLogico] = 0");
        builder.Property(x => x.RequiresSerial).HasColumnName("RequiereSerie");
        builder.Property(x => x.IsAssignable).HasColumnName("EsAsignable");
        builder.Property(x => x.HasComputeSpecs).HasColumnName("TieneEspecificaciones");
        builder.Property(x => x.IconName).HasColumnName("NombreIcono").HasMaxLength(50);
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItBrandConfiguration : IEntityTypeConfiguration<ItBrand>
{
    public void Configure(EntityTypeBuilder<ItBrand> builder)
    {
        builder.ToTable("Marcas", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Name).HasColumnName("Nombre").IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("[EliminadoLogico] = 0");
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Proveedores", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Name).HasColumnName("Nombre").IsRequired().HasMaxLength(150);
        builder.HasIndex(x => x.Name).IsUnique().HasFilter("[EliminadoLogico] = 0");
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItLocationConfiguration : IEntityTypeConfiguration<ItLocation>
{
    public void Configure(EntityTypeBuilder<ItLocation> builder)
    {
        builder.ToTable("Ubicaciones", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Name).HasColumnName("Nombre").IsRequired().HasMaxLength(150);
        builder.Property(x => x.Code).HasColumnName("Codigo").HasMaxLength(20);
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departamentos", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Name).HasColumnName("Nombre").IsRequired().HasMaxLength(150);
        builder.Property(x => x.Code).HasColumnName("Codigo").HasMaxLength(20);
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItAssetConfiguration : IEntityTypeConfiguration<ItAsset>
{
    public void Configure(EntityTypeBuilder<ItAsset> builder)
    {
        builder.ToTable("Activos", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.InternalCode).HasColumnName("CodigoInterno").IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.InternalCode).IsUnique().HasFilter("[EliminadoLogico] = 0");

        builder.Property(x => x.AssetTypeId).HasColumnName("TipoActivoId");
        builder.Property(x => x.BrandId).HasColumnName("MarcaId");
        builder.Property(x => x.Model).HasColumnName("Modelo").HasMaxLength(120);
        builder.Property(x => x.SerialNumber).HasColumnName("NumeroSerie").HasMaxLength(120);
        // Único parcial: la serie no se repite cuando existe (propuesta §18.9).
        builder.HasIndex(x => x.SerialNumber).IsUnique().HasFilter("[NumeroSerie] IS NOT NULL AND [EliminadoLogico] = 0");
        builder.Property(x => x.AssetTag).HasColumnName("Etiqueta").HasMaxLength(60);

        builder.Property(x => x.Status).HasColumnName("Estado");
        builder.HasIndex(x => x.Status);
        builder.Property(x => x.PhysicalCondition).HasColumnName("EstadoFisico");

        builder.Property(x => x.LocationId).HasColumnName("UbicacionId");
        builder.Property(x => x.LocationDetail).HasColumnName("DetalleUbicacion").HasMaxLength(200);
        builder.Property(x => x.DepartmentId).HasColumnName("DepartamentoId");
        builder.Property(x => x.CurrentHolderEmployeeId).HasColumnName("ResponsableActualId");

        builder.Property(x => x.PurchaseDate).HasColumnName("FechaCompra");
        builder.Property(x => x.SupplierId).HasColumnName("ProveedorId");
        builder.Property(x => x.Cost).HasColumnName("Costo").HasColumnType("decimal(18,2)");
        builder.Property(x => x.Currency).HasColumnName("Moneda").HasMaxLength(3);
        builder.Property(x => x.HasWarranty).HasColumnName("TieneGarantia");
        builder.Property(x => x.WarrantyEndDate).HasColumnName("FechaVencimientoGarantia");
        builder.HasIndex(x => x.WarrantyEndDate);
        builder.Property(x => x.InvoiceNumber).HasColumnName("NumeroFactura").HasMaxLength(100);

        builder.Property(x => x.Notes).HasColumnName("Observaciones").HasMaxLength(2000);

        builder.HasOne(x => x.AssetType).WithMany(t => t.Assets).HasForeignKey(x => x.AssetTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Brand).WithMany(b => b.Assets).HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Location).WithMany(l => l.Assets).HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Department).WithMany(d => d.Assets).HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Supplier).WithMany(s => s.Assets).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Holder).WithMany().HasForeignKey(x => x.CurrentHolderEmployeeId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Spec).WithOne(s => s.Asset).HasForeignKey<ItAssetSpec>(s => s.AssetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.History).WithOne(h => h.Asset).HasForeignKey(h => h.AssetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Photos).WithOne(p => p.Asset).HasForeignKey(p => p.AssetId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Credentials).WithOne(c => c.Asset).HasForeignKey(c => c.AssetId).OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItAssetCredentialConfiguration : IEntityTypeConfiguration<ItAssetCredential>
{
    public void Configure(EntityTypeBuilder<ItAssetCredential> builder)
    {
        builder.ToTable("CredencialesActivo", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.AssetId).HasColumnName("ActivoId");
        builder.Property(x => x.Type).HasColumnName("Tipo").HasConversion<int>();
        builder.Property(x => x.Label).HasColumnName("Cuenta").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Username).HasColumnName("Usuario").HasMaxLength(200);
        // Secreto cifrado (Data Protection, prefijo enc:). Tamaño holgado por el overhead del cifrado.
        builder.Property(x => x.SecretEncrypted).HasColumnName("SecretoCifrado").HasMaxLength(2000);
        builder.Property(x => x.Host).HasColumnName("Host").HasMaxLength(300);
        builder.Property(x => x.Notes).HasColumnName("Notas").HasMaxLength(1000);
        builder.Property(x => x.SortOrder).HasColumnName("Orden");
        builder.HasIndex(x => new { x.AssetId, x.SortOrder });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItAssetPhotoConfiguration : IEntityTypeConfiguration<ItAssetPhoto>
{
    public void Configure(EntityTypeBuilder<ItAssetPhoto> builder)
    {
        builder.ToTable("FotosActivo", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.AssetId).HasColumnName("ActivoId");
        builder.Property(x => x.SortOrder).HasColumnName("Orden");
        builder.Property(x => x.Content).HasColumnName("Contenido").HasColumnType("varbinary(max)").IsRequired();
        builder.Property(x => x.MimeType).HasColumnName("MimeType").HasMaxLength(40);
        builder.Property(x => x.SizeBytes).HasColumnName("PesoBytes");
        builder.Property(x => x.FileName).HasColumnName("NombreArchivo").HasMaxLength(200);
        builder.HasIndex(x => new { x.AssetId, x.SortOrder });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItAssetSpecConfiguration : IEntityTypeConfiguration<ItAssetSpec>
{
    public void Configure(EntityTypeBuilder<ItAssetSpec> builder)
    {
        builder.ToTable("EspecificacionesActivo", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.AssetId).HasColumnName("ActivoId");
        builder.HasIndex(x => x.AssetId).IsUnique();
        builder.Property(x => x.OperatingSystem).HasColumnName("SistemaOperativo").HasMaxLength(100);
        builder.Property(x => x.Processor).HasColumnName("Procesador").HasMaxLength(120);
        builder.Property(x => x.RamGb).HasColumnName("RamGb");
        builder.Property(x => x.DiskGb).HasColumnName("DiscoGb");
        builder.Property(x => x.MacEthernet).HasColumnName("MacEthernet").HasMaxLength(20);
        builder.Property(x => x.MacWifi).HasColumnName("MacWifi").HasMaxLength(20);
        builder.Property(x => x.IpAddress).HasColumnName("DireccionIp").HasMaxLength(45);
        builder.Property(x => x.DomainName).HasColumnName("NombreDominio").HasMaxLength(100);
        builder.Property(x => x.AnyDeskId).HasColumnName("AnyDeskId").HasMaxLength(40);
        builder.Property(x => x.Microsoft365User).HasColumnName("UsuarioM365").HasMaxLength(150);
        builder.Property(x => x.AntivirusStatus).HasColumnName("EstadoAntivirus").HasMaxLength(50);
        builder.Property(x => x.TechNotes).HasColumnName("ObservacionesTecnicas").HasMaxLength(1000);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItEmployeeConfiguration : IEntityTypeConfiguration<ItEmployee>
{
    public void Configure(EntityTypeBuilder<ItEmployee> builder)
    {
        builder.ToTable("Colaboradores", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.IdentificationType).HasColumnName("TipoIdentificacion");
        builder.Property(x => x.IdentificationNumber).HasColumnName("Identificacion").IsRequired().HasMaxLength(30);
        builder.Property(x => x.NormalizedIdentificationNumber).HasColumnName("IdentificacionNormalizada").IsRequired().HasMaxLength(30);
        builder.HasIndex(x => x.NormalizedIdentificationNumber).IsUnique().HasFilter("[EliminadoLogico] = 0");
        builder.Property(x => x.FullName).HasColumnName("NombreCompleto").IsRequired().HasMaxLength(200);
        builder.Property(x => x.Position).HasColumnName("Puesto").HasMaxLength(150);
        builder.Property(x => x.Department).HasColumnName("Departamento").HasMaxLength(150);
        builder.Property(x => x.Email).HasColumnName("Correo").HasMaxLength(200);
        builder.Property(x => x.PhoneNumber).HasColumnName("Telefono").HasMaxLength(30);
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class ItAssetHistoryConfiguration : IEntityTypeConfiguration<ItAssetHistory>
{
    public void Configure(EntityTypeBuilder<ItAssetHistory> builder)
    {
        builder.ToTable("HistorialActivo", "SOPORTE");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.AssetId).HasColumnName("ActivoId");
        builder.Property(x => x.EventType).HasColumnName("TipoEvento").IsRequired().HasMaxLength(40);
        builder.Property(x => x.FromStatus).HasColumnName("EstadoAnterior");
        builder.Property(x => x.ToStatus).HasColumnName("EstadoNuevo");
        builder.Property(x => x.Description).HasColumnName("Descripcion").HasMaxLength(500);
        builder.Property(x => x.OccurredAt).HasColumnName("OcurrioEn");
        builder.Property(x => x.PerformedBy).HasColumnName("RealizadoPor");
        builder.Property(x => x.TicketId).HasColumnName("BoletaId");
        builder.HasIndex(x => new { x.AssetId, x.OccurredAt });

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
