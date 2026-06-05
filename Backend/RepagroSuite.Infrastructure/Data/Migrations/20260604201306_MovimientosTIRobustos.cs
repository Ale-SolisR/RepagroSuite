using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MovimientosTIRobustos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BoletaId",
                schema: "SOPORTE",
                table: "HistorialActivo",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoCierre",
                schema: "SOPORTE",
                table: "Asignaciones",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            // Permiso para reactivar activos inactivos (solo admin). Propuesta §13.
            migrationBuilder.InsertData(
                schema: "CORE",
                table: "Permisos",
                columns: new[] { "Id", "Codigo", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "Descripcion", "EsActivo", "EliminadoLogico", "Modulo", "Nombre", "ActualizadoEn", "ActualizadoPor" },
                values: new object[] { new Guid("a0000001-0000-0000-0000-000000000047"), "Ti.Asset.Reactivate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "TI", "Reactivar activos inactivos", null, null });

            migrationBuilder.InsertData(
                schema: "CORE",
                table: "RolesPermisos",
                columns: new[] { "PermisoId", "RolId", "AsignadoEn", "AsignadoPor" },
                values: new object[] { new Guid("a0000001-0000-0000-0000-000000000047"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000047"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000047"));

            migrationBuilder.DropColumn(
                name: "BoletaId",
                schema: "SOPORTE",
                table: "HistorialActivo");

            migrationBuilder.DropColumn(
                name: "MotivoCierre",
                schema: "SOPORTE",
                table: "Asignaciones");
        }
    }
}
