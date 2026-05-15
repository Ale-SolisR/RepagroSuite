using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RepagroSuite.Domain.Entities;

namespace RepagroSuite.Infrastructure.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Salas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Name).HasColumnName("Nombre").IsRequired().HasMaxLength(100);
        builder.Property(x => x.Code).HasColumnName("Codigo").IsRequired().HasMaxLength(30);
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[EliminadoLogico] = 0");
        builder.Property(x => x.Capacity).HasColumnName("Capacidad");
        builder.Property(x => x.Location).HasColumnName("Ubicacion").HasMaxLength(200);
        builder.Property(x => x.Floor).HasColumnName("Piso").HasMaxLength(50);
        builder.Property(x => x.Description).HasColumnName("Descripcion").HasMaxLength(1000);
        builder.Property(x => x.Status).HasColumnName("Estado");
        builder.Property(x => x.ImageUrl).HasColumnName("UrlImagen").HasMaxLength(500);
        builder.Property(x => x.Color).HasColumnName("Color").HasMaxLength(30);

        builder.HasMany(x => x.RoomFeatures).WithOne(x => x.Room).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Availabilities).WithOne(x => x.Room).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Blocks).WithOne(x => x.Room).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Reservations).WithOne(x => x.Room).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class FeatureConfiguration : IEntityTypeConfiguration<Feature>
{
    public void Configure(EntityTypeBuilder<Feature> builder)
    {
        builder.ToTable("Caracteristicas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.Name).HasColumnName("Nombre").IsRequired().HasMaxLength(100);
        builder.Property(x => x.IconName).HasColumnName("NombreIcono").HasMaxLength(50);
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class RoomFeatureConfiguration : IEntityTypeConfiguration<RoomFeature>
{
    public void Configure(EntityTypeBuilder<RoomFeature> builder)
    {
        builder.ToTable("SalasCaracteristicas");
        builder.HasKey(x => new { x.RoomId, x.FeatureId });

        builder.Property(x => x.RoomId).HasColumnName("SalaId");
        builder.Property(x => x.FeatureId).HasColumnName("CaracteristicaId");

        builder.HasOne(x => x.Room).WithMany(x => x.RoomFeatures).HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Feature).WithMany(x => x.RoomFeatures).HasForeignKey(x => x.FeatureId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class RoomAvailabilityConfiguration : IEntityTypeConfiguration<RoomAvailability>
{
    public void Configure(EntityTypeBuilder<RoomAvailability> builder)
    {
        builder.ToTable("DisponibilidadSalas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.RoomId).HasColumnName("SalaId");
        builder.Property(x => x.DayOfWeek).HasColumnName("DiaSemana");
        builder.Property(x => x.IsAvailable).HasColumnName("EsDisponible");
        builder.HasIndex(x => new { x.RoomId, x.DayOfWeek }).IsUnique().HasFilter("[EliminadoLogico] = 0");

        builder.Property(x => x.OpenTime).HasColumnName("HoraApertura")
            .HasConversion(v => v.ToString("HH:mm"), v => TimeOnly.Parse(v));
        builder.Property(x => x.CloseTime).HasColumnName("HoraCierre")
            .HasConversion(v => v.ToString("HH:mm"), v => TimeOnly.Parse(v));

        builder.Property(x => x.MinReservationMinutes).HasColumnName("MinutosMinReserva");
        builder.Property(x => x.MaxReservationMinutes).HasColumnName("MinutosMaxReserva");
        builder.Property(x => x.SlotIntervalMinutes).HasColumnName("MinutosIntervaloSlot");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

public class RoomBlockConfiguration : IEntityTypeConfiguration<RoomBlock>
{
    public void Configure(EntityTypeBuilder<RoomBlock> builder)
    {
        builder.ToTable("BloquesSalas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.MapBaseEntityColumns();

        builder.Property(x => x.RoomId).HasColumnName("SalaId");
        builder.Property(x => x.BlockType).HasColumnName("TipoBloqueo");
        builder.Property(x => x.Reason).HasColumnName("Motivo").HasMaxLength(300);
        builder.Property(x => x.IsRecurring).HasColumnName("EsRecurrente");
        builder.Property(x => x.RecurringDayOfWeek).HasColumnName("DiaSemanaRecurrente");
        builder.Property(x => x.IsActive).HasColumnName("EsActivo");
        builder.Property(x => x.SpecificDate).HasColumnName("FechaEspecifica");
        builder.Property(x => x.SpecificStartDateTime).HasColumnName("FechaHoraInicioEspecifica");
        builder.Property(x => x.SpecificEndDateTime).HasColumnName("FechaHoraFinEspecifica");

        builder.Property(x => x.StartTime).HasColumnName("HoraInicio").HasConversion(
            v => v.HasValue ? v.Value.ToString("HH:mm") : null,
            v => v != null ? TimeOnly.Parse(v) : null);
        builder.Property(x => x.EndTime).HasColumnName("HoraFin").HasConversion(
            v => v.HasValue ? v.Value.ToString("HH:mm") : null,
            v => v != null ? TimeOnly.Parse(v) : null);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
