using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ColaboradoresTI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TI_Activos_Usuarios_ResponsableActualId",
                table: "TI_Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Asignaciones_Usuarios_ColaboradorId",
                table: "TI_Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Boletas_Usuarios_ColaboradorId",
                table: "TI_Boletas");

            migrationBuilder.CreateTable(
                name: "TI_Colaboradores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoIdentificacion = table.Column<int>(type: "int", nullable: false),
                    Identificacion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    IdentificacionNormalizada = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Puesto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Departamento = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Correo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
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
                    table.PrimaryKey("PK_TI_Colaboradores", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Codigo", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "Descripcion", "EsActivo", "EliminadoLogico", "Modulo", "Nombre", "ActualizadoEn", "ActualizadoPor" },
                values: new object[] { new Guid("a0000001-0000-0000-0000-000000000041"), "Ti.Employee.Manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Gestionar colaboradores TI", null, null });

            migrationBuilder.InsertData(
                table: "RolesPermisos",
                columns: new[] { "PermisoId", "RolId", "AsignadoEn", "AsignadoPor" },
                values: new object[] { new Guid("a0000001-0000-0000-0000-000000000041"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.CreateIndex(
                name: "IX_TI_Colaboradores_IdentificacionNormalizada",
                table: "TI_Colaboradores",
                column: "IdentificacionNormalizada",
                unique: true,
                filter: "[EliminadoLogico] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Activos_TI_Colaboradores_ResponsableActualId",
                table: "TI_Activos",
                column: "ResponsableActualId",
                principalTable: "TI_Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Asignaciones_TI_Colaboradores_ColaboradorId",
                table: "TI_Asignaciones",
                column: "ColaboradorId",
                principalTable: "TI_Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Boletas_TI_Colaboradores_ColaboradorId",
                table: "TI_Boletas",
                column: "ColaboradorId",
                principalTable: "TI_Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TI_Activos_TI_Colaboradores_ResponsableActualId",
                table: "TI_Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Asignaciones_TI_Colaboradores_ColaboradorId",
                table: "TI_Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Boletas_TI_Colaboradores_ColaboradorId",
                table: "TI_Boletas");

            migrationBuilder.DropTable(
                name: "TI_Colaboradores");

            migrationBuilder.DeleteData(
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000041"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000041"));

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Activos_Usuarios_ResponsableActualId",
                table: "TI_Activos",
                column: "ResponsableActualId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Asignaciones_Usuarios_ColaboradorId",
                table: "TI_Asignaciones",
                column: "ColaboradorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Boletas_Usuarios_ColaboradorId",
                table: "TI_Boletas",
                column: "ColaboradorId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
