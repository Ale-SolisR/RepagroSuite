using Microsoft.EntityFrameworkCore;

namespace RepagroSuite.Infrastructure.Data.Rastreo;

/// <summary>
/// DbContext DEDICADO y ACOTADO para administrar únicamente los usuarios del sistema de Rastreo
/// (<c>RASTREO.Usuarios</c>), que conviven con Repagro Suite en la misma base de datos.
///
/// Reglas de aislamiento (no romper):
///  • Mapea SOLO la entidad <see cref="RastreoUsuario"/>. No conoce el resto del esquema RASTREO.
///  • NUNCA tiene migraciones ni ejecuta Migrate()/EnsureCreated(): no puede alterar el esquema
///    del sistema de Rastreo. Solo lee/escribe filas de la tabla Usuarios.
///  • El mapeo replica exactamente la configuración del proyecto de Rastreo.
/// </summary>
public class RastreoDbContext : DbContext
{
    public RastreoDbContext(DbContextOptions<RastreoDbContext> options) : base(options) { }

    public DbSet<RastreoUsuario> Usuarios => Set<RastreoUsuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RastreoUsuario>(e =>
        {
            e.ToTable("Usuarios", "RASTREO");
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Correo).IsUnique();
            e.Property(u => u.Correo).HasMaxLength(150).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(u => u.Nombre).HasMaxLength(150);
            e.Property(u => u.Rol).HasMaxLength(20).IsRequired().HasDefaultValue("USUARIO");
        });
    }
}
