using Microsoft.EntityFrameworkCore;
using Rastreo.Api.Models;

namespace Rastreo.Api.Data;

public class RastreoDbContext : DbContext
{
    public RastreoDbContext(DbContextOptions<RastreoDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Granja> Granjas => Set<Granja>();
    public DbSet<Enfermedad> Enfermedades => Set<Enfermedad>();
    public DbSet<Vacuna> Vacunas => Set<Vacuna>();
    public DbSet<Registro> Registros => Set<Registro>();
    public DbSet<RegistroVacuna> RegistroVacunas => Set<RegistroVacuna>();
    public DbSet<RegistroDetalle> RegistroDetalles => Set<RegistroDetalle>();
    public DbSet<RegistroDetalleEnfermedadValor> RegistroDetalleEnfermedadValores => Set<RegistroDetalleEnfermedadValor>();
    public DbSet<RegistroDetalleFoto> RegistroDetalleFotos => Set<RegistroDetalleFoto>();
    public DbSet<InformeEvaluacion> InformesEvaluacion => Set<InformeEvaluacion>();
    public DbSet<InformeEnvio> InformeEnvios => Set<InformeEnvio>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Esquema dedicado: RASTREO convive con otros sistemas (modulo TI en dbo)
        // dentro de la misma base de datos sin colisionar (p.ej. tabla Usuarios).
        mb.HasDefaultSchema("RASTREO");

        // Usuarios
        mb.Entity<Usuario>(e =>
        {
            e.ToTable("Usuarios");
            e.HasIndex(u => u.Correo).IsUnique();
            e.Property(u => u.Correo).HasMaxLength(150).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            e.Property(u => u.Nombre).HasMaxLength(150);
            e.Property(u => u.Rol).HasMaxLength(20).IsRequired().HasDefaultValue("USUARIO");
        });

        // Granjas
        mb.Entity<Granja>(e =>
        {
            e.ToTable("Granjas");
            e.Property(g => g.Codigo).HasMaxLength(20).IsRequired();
            e.Property(g => g.Nombre).HasMaxLength(150).IsRequired();
            e.Property(g => g.Propietario).HasMaxLength(150);
            e.Property(g => g.Ubicacion).HasMaxLength(255);
            e.Property(g => g.Telefono).HasMaxLength(30);
            e.Property(g => g.Correo).HasMaxLength(150);
            e.HasIndex(g => g.Codigo).IsUnique();
            e.HasIndex(g => g.Nombre)
                .IsUnique()
                .HasFilter("[Activo] = 1");
        });

        // Enfermedades catalogo
        mb.Entity<Enfermedad>(e =>
        {
            e.ToTable("Enfermedades");
            e.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.TipoCampo).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        // Vacunas catalogo
        mb.Entity<Vacuna>(e =>
        {
            e.ToTable("Vacunas");
            e.Property(x => x.Codigo).HasMaxLength(50).IsRequired();
            e.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
            e.Property(x => x.Laboratorio).HasMaxLength(150);
            e.Property(x => x.Observaciones).HasMaxLength(1000);
            e.HasIndex(x => x.Codigo).IsUnique();
        });

        // Registros
        mb.Entity<Registro>(e =>
        {
            e.ToTable("Registros");
            // Quitar default NEWSEQUENTIALID — el cliente genera el UUID
            e.Property(r => r.FechaCreacion).HasColumnType("datetime2");
            e.Property(r => r.FechaInicio).HasColumnType("datetime2");
            e.Property(r => r.FechaFinalizacion).HasColumnType("datetime2");
            e.Property(r => r.FechaUltimaModificacion).HasColumnType("datetime2");
            e.Property(r => r.Lote).HasMaxLength(100);
            e.Property(r => r.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("BORRADOR");
            e.Property(r => r.PasoActual).HasMaxLength(50);
            e.Property(r => r.UsuarioCreacion).HasMaxLength(150);
            e.Property(r => r.UsuarioUltimaModificacion).HasMaxLength(150);

            e.HasOne(r => r.Granja).WithMany().HasForeignKey(r => r.GranjaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.Usuario).WithMany().HasForeignKey(r => r.UsuarioId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(r => r.FechaCreacion);
            e.HasIndex(r => r.GranjaId);
            e.HasIndex(r => r.Estado);
            e.HasIndex(r => r.UsuarioId);
            e.ToTable(t => t.HasCheckConstraint("CK_Registros_Estado",
                "[Estado] IN ('BORRADOR','EN_PROCESO','FINALIZADO','ANULADO')"));
        });

        mb.Entity<RegistroVacuna>(e =>
        {
            e.ToTable("RegistroVacunas");
            e.HasKey(x => new { x.RegistroId, x.VacunaId });
            e.Property(x => x.FechaCreacion).HasColumnType("datetime2");
            e.HasOne(x => x.Registro)
                .WithMany(r => r.Vacunas)
                .HasForeignKey(x => x.RegistroId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Vacuna)
                .WithMany()
                .HasForeignKey(x => x.VacunaId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.VacunaId);
        });

        // RegistroDetalle
        mb.Entity<RegistroDetalle>(e =>
        {
            e.ToTable("RegistroDetalle");
            e.Property(d => d.FechaCreacion).HasColumnType("datetime2");
            e.Property(d => d.FechaUltimaModificacion).HasColumnType("datetime2");
            e.Property(d => d.AgudaCronica).HasMaxLength(2);
            e.Property(d => d.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("BORRADOR");
            e.Property(d => d.PasoActual).HasMaxLength(50);

            e.HasOne(d => d.Registro)
                .WithMany(r => r.Detalles)
                .HasForeignKey(d => d.RegistroId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(d => d.RegistroId);

            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Detalle_AL", "[ApicalIzquierdo] BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_Detalle_CL", "[CardiacoIzquierdo] BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_Detalle_DL", "[DiafragmaticoIzquierdo] BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_Detalle_AD", "[ApicalDerecho] BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_Detalle_CD", "[CardiacoDerecho] BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_Detalle_DD", "[DiafragmaticoDerecho] BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_Detalle_L", "[Accesorio] BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_Detalle_SPES", "[SPES] BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_Detalle_AC",
                    "[AgudaCronica] IS NULL OR [AgudaCronica] IN ('A','C','AC')");
            });
        });

        // RegistroDetalleFoto (hasta 3 por detalle)
        mb.Entity<RegistroDetalleEnfermedadValor>(e =>
        {
            e.ToTable("RegistroDetalleEnfermedadValores");
            e.Property(x => x.ValorTexto).HasMaxLength(20);
            e.HasOne(x => x.RegistroDetalle)
                .WithMany(d => d.EnfermedadesValores)
                .HasForeignKey(x => x.RegistroDetalleId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Enfermedad)
                .WithMany()
                .HasForeignKey(x => x.EnfermedadId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.RegistroDetalleId, x.EnfermedadId }).IsUnique();
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_DetalleEnf_ValorNumero", "[ValorNumero] IS NULL OR [ValorNumero] BETWEEN 0 AND 4");
                t.HasCheckConstraint("CK_DetalleEnf_ValorTexto", "[ValorTexto] IS NULL OR [ValorTexto] IN ('A','C','AC')");
            });
        });

        // RegistroDetalleFoto (hasta 3 por detalle)
        mb.Entity<RegistroDetalleFoto>(e =>
        {
            e.ToTable("RegistroDetalleFoto");
            e.Property(f => f.FotoBinario).HasColumnType("varbinary(max)").IsRequired();
            e.Property(f => f.FotoMimeType).HasMaxLength(50).IsRequired();
            e.Property(f => f.FotoNombre).HasMaxLength(255);
            e.Property(f => f.FechaCreacion).HasColumnType("datetime2");
            e.Property(f => f.FechaUltimaModificacion).HasColumnType("datetime2");

            e.HasOne(f => f.RegistroDetalle)
                .WithMany(d => d.Fotos)
                .HasForeignKey(f => f.RegistroDetalleId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(f => new { f.RegistroDetalleId, f.Orden }).IsUnique();

            e.ToTable(t => t.HasCheckConstraint("CK_DetalleFoto_Orden", "[Orden] BETWEEN 1 AND 3"));
        });

        // ===================== Informes de Evaluacion =====================
        mb.Entity<InformeEvaluacion>(e =>
        {
            e.ToTable("InformesEvaluacion");
            e.Property(i => i.Consecutivo).HasMaxLength(40).IsRequired();
            e.HasIndex(i => i.Consecutivo).IsUnique();
            e.Property(i => i.PeriodoEtiqueta).HasMaxLength(100);
            e.Property(i => i.ResponsableTecnico).HasMaxLength(150);
            e.Property(i => i.Cliente).HasMaxLength(200);
            e.Property(i => i.Estado).HasMaxLength(20).IsRequired().HasDefaultValue("GENERADO");
            e.Property(i => i.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(i => i.PdfBinario).HasColumnType("varbinary(max)");
            e.Property(i => i.NivelRiesgo).HasMaxLength(15).IsRequired().HasDefaultValue("BAJO");
            e.Property(i => i.UsuarioGeneracion).HasMaxLength(150);
            e.Property(i => i.FechaGeneracion).HasColumnType("datetime2");
            e.Property(i => i.PeriodoDesde).HasColumnType("datetime2");
            e.Property(i => i.PeriodoHasta).HasColumnType("datetime2");
            e.Property(i => i.FechaUltimoEnvio).HasColumnType("datetime2");

            e.HasOne(i => i.Granja)
                .WithMany()
                .HasForeignKey(i => i.GranjaId)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(i => i.GranjaId);
            e.HasIndex(i => i.FechaGeneracion);
            e.HasIndex(i => new { i.GranjaId, i.PeriodoDesde, i.PeriodoHasta });

            e.ToTable(t => t.HasCheckConstraint("CK_Informe_Estado",
                "[Estado] IN ('BORRADOR','GENERADO','ENVIADO','ERROR','ANULADO')"));
            e.ToTable(t => t.HasCheckConstraint("CK_Informe_Riesgo",
                "[NivelRiesgo] IN ('BAJO','MEDIO','ALTO','CRITICO')"));
        });

        mb.Entity<InformeEnvio>(e =>
        {
            e.ToTable("InformeEnvios");
            e.Property(x => x.DestinatariosJson).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.Asunto).HasMaxLength(255);
            e.Property(x => x.UsuarioEnvio).HasMaxLength(150);
            e.Property(x => x.Estado).HasMaxLength(15).IsRequired().HasDefaultValue("OK");
            e.Property(x => x.ErrorMensaje).HasMaxLength(2000);
            e.Property(x => x.Fecha).HasColumnType("datetime2");

            e.HasOne(x => x.Informe)
                .WithMany(i => i.Envios)
                .HasForeignKey(x => x.InformeId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.InformeId);
            e.HasIndex(x => x.Fecha);
        });
    }
}
