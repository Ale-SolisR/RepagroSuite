using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModuloTIBoletas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TI_Boletas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Consecutivo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TipoBoleta = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    EmitidaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResponsableTiId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PdfBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PdfSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AnuladaPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AnuladaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_TI_Boletas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TI_Boletas_Usuarios_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TI_Boletas_Usuarios_ResponsableTiId",
                        column: x => x.ResponsableTiId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TI_ConsecutivosDocumento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodigoTipo = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    Anio = table.Column<int>(type: "int", nullable: false),
                    Prefijo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    UltimoNumero = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_TI_ConsecutivosDocumento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TI_Asignaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ColaboradorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoletaEntregaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoletaDevolucionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AsignadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DevueltoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstadoFisicoEntrega = table.Column<int>(type: "int", nullable: false),
                    EstadoFisicoRecepcion = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Accesorios = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ObservacionesDevolucion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_TI_Asignaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TI_Asignaciones_TI_Activos_ActivoId",
                        column: x => x.ActivoId,
                        principalTable: "TI_Activos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TI_Asignaciones_TI_Boletas_BoletaDevolucionId",
                        column: x => x.BoletaDevolucionId,
                        principalTable: "TI_Boletas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TI_Asignaciones_TI_Boletas_BoletaEntregaId",
                        column: x => x.BoletaEntregaId,
                        principalTable: "TI_Boletas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TI_Asignaciones_Usuarios_ColaboradorId",
                        column: x => x.ColaboradorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TI_DetalleBoleta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoletaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TipoLinea = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    Condicion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_TI_DetalleBoleta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TI_DetalleBoleta_TI_Activos_ActivoId",
                        column: x => x.ActivoId,
                        principalTable: "TI_Activos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TI_DetalleBoleta_TI_Boletas_BoletaId",
                        column: x => x.BoletaId,
                        principalTable: "TI_Boletas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TI_FirmasBoleta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoletaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoFirmante = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NombreFirmante = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ImagenBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FirmadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DireccionIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    UsuarioAutenticadoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_TI_FirmasBoleta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TI_FirmasBoleta_TI_Boletas_BoletaId",
                        column: x => x.BoletaId,
                        principalTable: "TI_Boletas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TI_FotosBoleta",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoletaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ImagenBase64 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    PesoBytes = table.Column<int>(type: "int", nullable: true),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    SubidoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
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
                    table.PrimaryKey("PK_TI_FotosBoleta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TI_FotosBoleta_TI_Boletas_BoletaId",
                        column: x => x.BoletaId,
                        principalTable: "TI_Boletas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Codigo", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "Descripcion", "EsActivo", "EliminadoLogico", "Modulo", "Nombre", "ActualizadoEn", "ActualizadoPor" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000037"), "Ti.Assign", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Asignar activos TI", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000038"), "Ti.Return", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Devolver activos TI", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000039"), "Ti.Ticket.Create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Crear boletas TI", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000040"), "Ti.Ticket.Void", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Anular boletas TI", null, null }
                });

            migrationBuilder.InsertData(
                table: "TI_ConsecutivosDocumento",
                columns: new[] { "Id", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "EliminadoLogico", "UltimoNumero", "Prefijo", "CodigoTipo", "ActualizadoEn", "ActualizadoPor", "Anio" },
                values: new object[,]
                {
                    { new Guid("e0000001-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, 0L, "TI", "ENT", null, null, 2026 },
                    { new Guid("e0000001-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, 0L, "TI", "DEV", null, null, 2026 },
                    { new Guid("e0000001-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, 0L, "TI", "PRE", null, null, 2026 },
                    { new Guid("e0000001-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, 0L, "TI", "MAN", null, null, 2026 },
                    { new Guid("e0000001-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, 0L, "TI", "REP", null, null, 2026 },
                    { new Guid("e0000001-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, 0L, "TI", "TRA", null, null, 2026 },
                    { new Guid("e0000001-0000-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, 0L, "TI", "CRE", null, null, 2026 },
                    { new Guid("e0000001-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, 0L, "TI", "ACC", null, null, 2026 },
                    { new Guid("e0000001-0000-0000-0000-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, 0L, "TI", "BAJ", null, null, 2026 }
                });

            migrationBuilder.InsertData(
                table: "RolesPermisos",
                columns: new[] { "PermisoId", "RolId", "AsignadoEn", "AsignadoPor" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000037"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000038"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000039"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000040"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_TI_Asignaciones_ActivoId",
                table: "TI_Asignaciones",
                column: "ActivoId",
                unique: true,
                filter: "[Estado] = 0 AND [EliminadoLogico] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Asignaciones_BoletaDevolucionId",
                table: "TI_Asignaciones",
                column: "BoletaDevolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Asignaciones_BoletaEntregaId",
                table: "TI_Asignaciones",
                column: "BoletaEntregaId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Asignaciones_ColaboradorId",
                table: "TI_Asignaciones",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Boletas_ColaboradorId",
                table: "TI_Boletas",
                column: "ColaboradorId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Boletas_Consecutivo",
                table: "TI_Boletas",
                column: "Consecutivo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TI_Boletas_EmitidaEn",
                table: "TI_Boletas",
                column: "EmitidaEn");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Boletas_ResponsableTiId",
                table: "TI_Boletas",
                column: "ResponsableTiId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_Boletas_TipoBoleta_Estado",
                table: "TI_Boletas",
                columns: new[] { "TipoBoleta", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_TI_ConsecutivosDocumento_CodigoTipo_Anio",
                table: "TI_ConsecutivosDocumento",
                columns: new[] { "CodigoTipo", "Anio" },
                unique: true,
                filter: "[EliminadoLogico] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TI_DetalleBoleta_ActivoId",
                table: "TI_DetalleBoleta",
                column: "ActivoId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_DetalleBoleta_BoletaId",
                table: "TI_DetalleBoleta",
                column: "BoletaId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_FirmasBoleta_BoletaId",
                table: "TI_FirmasBoleta",
                column: "BoletaId");

            migrationBuilder.CreateIndex(
                name: "IX_TI_FotosBoleta_BoletaId",
                table: "TI_FotosBoleta",
                column: "BoletaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TI_Asignaciones");

            migrationBuilder.DropTable(
                name: "TI_ConsecutivosDocumento");

            migrationBuilder.DropTable(
                name: "TI_DetalleBoleta");

            migrationBuilder.DropTable(
                name: "TI_FirmasBoleta");

            migrationBuilder.DropTable(
                name: "TI_FotosBoleta");

            migrationBuilder.DropTable(
                name: "TI_Boletas");

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000037"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000038"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000039"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000040"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000037"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000038"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000039"));

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000040"));
        }
    }
}
