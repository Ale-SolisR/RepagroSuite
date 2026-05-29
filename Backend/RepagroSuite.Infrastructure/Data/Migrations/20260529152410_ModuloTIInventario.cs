using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModuloTIInventario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TI_Departamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EsActivo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualizadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EliminadoLogico = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TI_Departamentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TI_Marcas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EsActivo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualizadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EliminadoLogico = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TI_Marcas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TI_TiposActivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequiereSerie = table.Column<bool>(type: "bit", nullable: false),
                    EsAsignable = table.Column<bool>(type: "bit", nullable: false),
                    TieneEspecificaciones = table.Column<bool>(type: "bit", nullable: false),
                    NombreIcono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EsActivo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualizadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EliminadoLogico = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TI_TiposActivo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TI_Ubicaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EsActivo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualizadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EliminadoLogico = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TI_Ubicaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TI_Activos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoInterno = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TipoActivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MarcaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Modelo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    NumeroSerie = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Etiqueta = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    EstadoFisico = table.Column<int>(type: "int", nullable: false),
                    UbicacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DetalleUbicacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DepartamentoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsableActualId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCompra = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Proveedor = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Costo = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Moneda = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    TieneGarantia = table.Column<bool>(type: "bit", nullable: false),
                    FechaVencimientoGarantia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UrlImagen = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualizadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EliminadoLogico = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TI_Activos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TI_Activos_TI_Departamentos_DepartamentoId",
                        column: x => x.DepartamentoId,
                        principalTable: "TI_Departamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TI_Activos_TI_Marcas_MarcaId",
                        column: x => x.MarcaId,
                        principalTable: "TI_Marcas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TI_Activos_TI_TiposActivo_TipoActivoId",
                        column: x => x.TipoActivoId,
                        principalTable: "TI_TiposActivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TI_Activos_TI_Ubicaciones_UbicacionId",
                        column: x => x.UbicacionId,
                        principalTable: "TI_Ubicaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TI_Activos_Usuarios_ResponsableActualId",
                        column: x => x.ResponsableActualId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TI_EspecificacionesActivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SistemaOperativo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Procesador = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RamGb = table.Column<int>(type: "int", nullable: true),
                    DiscoGb = table.Column<int>(type: "int", nullable: true),
                    MacEthernet = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MacWifi = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DireccionIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    NombreDominio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AnyDeskId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    UsuarioM365 = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    EstadoAntivirus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ObservacionesTecnicas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualizadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EliminadoLogico = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TI_EspecificacionesActivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TI_EspecificacionesActivo_TI_Activos_ActivoId",
                        column: x => x.ActivoId,
                        principalTable: "TI_Activos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TI_HistorialActivo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoEvento = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EstadoAnterior = table.Column<int>(type: "int", nullable: true),
                    EstadoNuevo = table.Column<int>(type: "int", nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OcurrioEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RealizadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualizadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EliminadoLogico = table.Column<bool>(type: "bit", nullable: false),
                    EliminadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EliminadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TI_HistorialActivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TI_HistorialActivo_TI_Activos_ActivoId",
                        column: x => x.ActivoId,
                        principalTable: "TI_Activos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ModulosSistema",
                columns: new[] { "Id", "Codigo", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "Descripcion", "NombreIcono", "EsActivo", "EsNuclear", "EliminadoLogico", "Nombre", "PrefijoRuta", "OrdenVisualizacion", "ActualizadoEn", "ActualizadoPor", "Version" },
                values: new object[] { new Guid("c0000001-0000-0000-0000-000000000002"), "TI", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Inventario de activos tecnológicos, asignaciones y boletas", "cpu", true, false, false, "Inventario TI", "/ti", 2, null, null, "1.0.0" });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Codigo", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "Descripcion", "EsActivo", "EliminadoLogico", "Modulo", "Nombre", "ActualizadoEn", "ActualizadoPor" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000031"), "Ti.Inventory.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Ver inventario TI", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000032"), "Ti.Inventory.Create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Crear activos TI", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000033"), "Ti.Inventory.Update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Editar activos TI", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000034"), "Ti.Inventory.Delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Eliminar activos TI", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000035"), "Ti.Catalog.Manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Administrar catálogos TI", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000036"), "Ti.Dashboard.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Ver dashboard TI", null, null }
                });

            migrationBuilder.InsertData(
                table: "TI_TiposActivo",
                columns: new[] { "Id", "Codigo", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "TieneEspecificaciones", "NombreIcono", "EsActivo", "EsAsignable", "EliminadoLogico", "Nombre", "RequiereSerie", "ActualizadoEn", "ActualizadoPor" },
                values: new object[,]
                {
                    { new Guid("d0000001-0000-0000-0000-000000000001"), "LAPTOP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "laptop", true, true, false, "Laptop", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000002"), "DESKTOP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "monitor", true, true, false, "Desktop", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000003"), "TABLET", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "tablet", true, true, false, "Tablet", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000004"), "PHONE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "smartphone", true, true, false, "Celular", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000005"), "PRINTER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "printer", true, true, false, "Impresora", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000006"), "SCREEN", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "monitor", true, true, false, "Monitor", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000007"), "CAMERA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "camera", true, true, false, "Cámara", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000008"), "SWITCH", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "network", true, true, false, "Switch", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000009"), "AP", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "wifi", true, true, false, "Access Point", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000010"), "UPS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "battery", true, true, false, "UPS", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000011"), "SERVER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, "server", true, true, false, "Servidor", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000012"), "NETDEV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "router", true, true, false, "Equipo de red", true, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000013"), "LICENSE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "key", true, false, false, "Licencia", false, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000014"), "ACCESS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "mouse-pointer", true, true, false, "Accesorio", false, null, null },
                    { new Guid("d0000001-0000-0000-0000-000000000015"), "OTHER", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, "box", true, true, false, "Otro", false, null, null }
                });

            migrationBuilder.InsertData(
                table: "RolesPermisos",
                columns: new[] { "PermisoId", "RolId", "AsignadoEn", "AsignadoPor" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000031"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000032"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000033"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000034"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000035"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000036"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TI_Activos_CodigoInterno",
                table: "TI_Activos",
                column: "CodigoInterno",
                unique: true,
                filter: "[EliminadoLogico] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Activos_DepartamentoId",
                table: "TI_Activos",
                column: "DepartamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Activos_Estado",
                table: "TI_Activos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Activos_FechaVencimientoGarantia",
                table: "TI_Activos",
                column: "FechaVencimientoGarantia");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Activos_MarcaId",
                table: "TI_Activos",
                column: "MarcaId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Activos_NumeroSerie",
                table: "TI_Activos",
                column: "NumeroSerie",
                unique: true,
                filter: "[NumeroSerie] IS NOT NULL AND [EliminadoLogico] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Activos_ResponsableActualId",
                table: "TI_Activos",
                column: "ResponsableActualId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Activos_TipoActivoId",
                table: "TI_Activos",
                column: "TipoActivoId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Activos_UbicacionId",
                table: "TI_Activos",
                column: "UbicacionId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_EspecificacionesActivo_ActivoId",
                table: "TI_EspecificacionesActivo",
                column: "ActivoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TI_HistorialActivo_ActivoId_OcurrioEn",
                table: "TI_HistorialActivo",
                columns: new[] { "ActivoId", "OcurrioEn" });

            migrationBuilder.CreateIndex(
                name: "IX_TI_Marcas_Nombre",
                table: "TI_Marcas",
                column: "Nombre",
                unique: true,
                filter: "[EliminadoLogico] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TI_TiposActivo_Codigo",
                table: "TI_TiposActivo",
                column: "Codigo",
                unique: true,
                filter: "[EliminadoLogico] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TI_EspecificacionesActivo");

            migrationBuilder.DropTable(
                name: "TI_HistorialActivo");

            migrationBuilder.DropTable(
                name: "TI_Activos");

            migrationBuilder.DropTable(
                name: "TI_Departamentos");

            migrationBuilder.DropTable(
                name: "TI_Marcas");

            migrationBuilder.DropTable(
                name: "TI_TiposActivo");

            migrationBuilder.DropTable(
                name: "TI_Ubicaciones");

            migrationBuilder.DeleteData(
                table: "ModulosSistema",
                keyColumn: "Id",
                keyValue: new Guid("c0000001-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000031"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000032"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000033"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000034"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000035"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000036"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000031"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000032"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000033"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000034"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000035"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000036"));
        }
    }
}
