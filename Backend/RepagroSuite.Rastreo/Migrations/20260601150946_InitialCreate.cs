using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rastreo.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "RASTREO");

            migrationBuilder.CreateTable(
                name: "Enfermedades",
                schema: "RASTREO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    TipoCampo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enfermedades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Granjas",
                schema: "RASTREO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Propietario = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Ubicacion = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Granjas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "RASTREO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Correo = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "USUARIO"),
                    SesionToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SesionExpira = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InformesEvaluacion",
                schema: "RASTREO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Consecutivo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    GranjaId = table.Column<int>(type: "int", nullable: false),
                    PeriodoDesde = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodoHasta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodoEtiqueta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResponsableTecnico = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Cliente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "GENERADO"),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PdfBinario = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    PdfPesoBytes = table.Column<int>(type: "int", nullable: true),
                    FechaGeneracion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioGeneracion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FechaUltimoEnvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CantidadEnvios = table.Column<int>(type: "int", nullable: false),
                    TotalEvaluados = table.Column<int>(type: "int", nullable: false),
                    PrevalenciaPct = table.Column<double>(type: "float", nullable: false),
                    ConsolidacionPct = table.Column<double>(type: "float", nullable: false),
                    Idn = table.Column<double>(type: "float", nullable: false),
                    NivelRiesgo = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false, defaultValue: "BAJO")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InformesEvaluacion", x => x.Id);
                    table.CheckConstraint("CK_Informe_Estado", "[Estado] IN ('BORRADOR','GENERADO','ENVIADO','ERROR','ANULADO')");
                    table.CheckConstraint("CK_Informe_Riesgo", "[NivelRiesgo] IN ('BAJO','MEDIO','ALTO','CRITICO')");
                    table.ForeignKey(
                        name: "FK_InformesEvaluacion_Granjas_GranjaId",
                        column: x => x.GranjaId,
                        principalSchema: "RASTREO",
                        principalTable: "Granjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Registros",
                schema: "RASTREO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinalizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GranjaId = table.Column<int>(type: "int", nullable: true),
                    Lote = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "BORRADOR"),
                    PasoActual = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DetalleActualId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UsuarioCreacion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioUltimaModificacion = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registros", x => x.Id);
                    table.CheckConstraint("CK_Registros_Estado", "[Estado] IN ('BORRADOR','EN_PROCESO','FINALIZADO','ANULADO')");
                    table.ForeignKey(
                        name: "FK_Registros_Granjas_GranjaId",
                        column: x => x.GranjaId,
                        principalSchema: "RASTREO",
                        principalTable: "Granjas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Registros_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "RASTREO",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InformeEnvios",
                schema: "RASTREO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InformeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioEnvio = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DestinatariosJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Asunto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false, defaultValue: "OK"),
                    ErrorMensaje = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InformeEnvios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InformeEnvios_InformesEvaluacion_InformeId",
                        column: x => x.InformeId,
                        principalSchema: "RASTREO",
                        principalTable: "InformesEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistroDetalle",
                schema: "RASTREO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClienteId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RegistroId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumeroLinea = table.Column<int>(type: "int", nullable: false),
                    ApicalIzquierdo = table.Column<byte>(type: "tinyint", nullable: false),
                    CardiacoIzquierdo = table.Column<byte>(type: "tinyint", nullable: false),
                    DiafragmaticoIzquierdo = table.Column<byte>(type: "tinyint", nullable: false),
                    ApicalDerecho = table.Column<byte>(type: "tinyint", nullable: false),
                    CardiacoDerecho = table.Column<byte>(type: "tinyint", nullable: false),
                    DiafragmaticoDerecho = table.Column<byte>(type: "tinyint", nullable: false),
                    Accesorio = table.Column<byte>(type: "tinyint", nullable: false),
                    SPES = table.Column<byte>(type: "tinyint", nullable: false),
                    AbscesoNodulo = table.Column<bool>(type: "bit", nullable: false),
                    PericardioEngrosado = table.Column<bool>(type: "bit", nullable: false),
                    Pericarditis = table.Column<bool>(type: "bit", nullable: false),
                    AgudaCronica = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    Moco = table.Column<bool>(type: "bit", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "BORRADOR"),
                    PasoActual = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistroDetalle", x => x.Id);
                    table.CheckConstraint("CK_Detalle_AC", "[AgudaCronica] IS NULL OR [AgudaCronica] IN ('A','C','AC')");
                    table.CheckConstraint("CK_Detalle_AD", "[ApicalDerecho] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Detalle_AL", "[ApicalIzquierdo] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Detalle_CD", "[CardiacoDerecho] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Detalle_CL", "[CardiacoIzquierdo] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Detalle_DD", "[DiafragmaticoDerecho] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Detalle_DL", "[DiafragmaticoIzquierdo] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Detalle_L", "[Accesorio] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_Detalle_SPES", "[SPES] BETWEEN 0 AND 4");
                    table.ForeignKey(
                        name: "FK_RegistroDetalle_Registros_RegistroId",
                        column: x => x.RegistroId,
                        principalSchema: "RASTREO",
                        principalTable: "Registros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistroDetalleFoto",
                schema: "RASTREO",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegistroDetalleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Orden = table.Column<byte>(type: "tinyint", nullable: false),
                    FotoBinario = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    FotoMimeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FotoPesoBytes = table.Column<int>(type: "int", nullable: false),
                    FotoNombre = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaUltimaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistroDetalleFoto", x => x.Id);
                    table.CheckConstraint("CK_DetalleFoto_Orden", "[Orden] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_RegistroDetalleFoto_RegistroDetalle_RegistroDetalleId",
                        column: x => x.RegistroDetalleId,
                        principalSchema: "RASTREO",
                        principalTable: "RegistroDetalle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enfermedades_Codigo",
                schema: "RASTREO",
                table: "Enfermedades",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Granjas_Codigo",
                schema: "RASTREO",
                table: "Granjas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Granjas_Nombre",
                schema: "RASTREO",
                table: "Granjas",
                column: "Nombre",
                unique: true,
                filter: "[Activo] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_InformeEnvios_Fecha",
                schema: "RASTREO",
                table: "InformeEnvios",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_InformeEnvios_InformeId",
                schema: "RASTREO",
                table: "InformeEnvios",
                column: "InformeId");

            migrationBuilder.CreateIndex(
                name: "IX_InformesEvaluacion_Consecutivo",
                schema: "RASTREO",
                table: "InformesEvaluacion",
                column: "Consecutivo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InformesEvaluacion_FechaGeneracion",
                schema: "RASTREO",
                table: "InformesEvaluacion",
                column: "FechaGeneracion");

            migrationBuilder.CreateIndex(
                name: "IX_InformesEvaluacion_GranjaId",
                schema: "RASTREO",
                table: "InformesEvaluacion",
                column: "GranjaId");

            migrationBuilder.CreateIndex(
                name: "IX_InformesEvaluacion_GranjaId_PeriodoDesde_PeriodoHasta",
                schema: "RASTREO",
                table: "InformesEvaluacion",
                columns: new[] { "GranjaId", "PeriodoDesde", "PeriodoHasta" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistroDetalle_RegistroId",
                schema: "RASTREO",
                table: "RegistroDetalle",
                column: "RegistroId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroDetalleFoto_RegistroDetalleId_Orden",
                schema: "RASTREO",
                table: "RegistroDetalleFoto",
                columns: new[] { "RegistroDetalleId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registros_Estado",
                schema: "RASTREO",
                table: "Registros",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Registros_FechaCreacion",
                schema: "RASTREO",
                table: "Registros",
                column: "FechaCreacion");

            migrationBuilder.CreateIndex(
                name: "IX_Registros_GranjaId",
                schema: "RASTREO",
                table: "Registros",
                column: "GranjaId");

            migrationBuilder.CreateIndex(
                name: "IX_Registros_UsuarioId",
                schema: "RASTREO",
                table: "Registros",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Correo",
                schema: "RASTREO",
                table: "Usuarios",
                column: "Correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Enfermedades",
                schema: "RASTREO");

            migrationBuilder.DropTable(
                name: "InformeEnvios",
                schema: "RASTREO");

            migrationBuilder.DropTable(
                name: "RegistroDetalleFoto",
                schema: "RASTREO");

            migrationBuilder.DropTable(
                name: "InformesEvaluacion",
                schema: "RASTREO");

            migrationBuilder.DropTable(
                name: "RegistroDetalle",
                schema: "RASTREO");

            migrationBuilder.DropTable(
                name: "Registros",
                schema: "RASTREO");

            migrationBuilder.DropTable(
                name: "Granjas",
                schema: "RASTREO");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "RASTREO");
        }
    }
}
