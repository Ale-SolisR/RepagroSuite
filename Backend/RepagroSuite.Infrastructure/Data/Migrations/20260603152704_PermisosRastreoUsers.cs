using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PermisosRastreoUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "CORE",
                table: "Permisos",
                columns: new[] { "Id", "Codigo", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "Descripcion", "EsActivo", "EliminadoLogico", "Modulo", "Nombre", "ActualizadoEn", "ActualizadoPor" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000042"), "RastreoUsers.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Rastreo", "Ver usuarios de rastreo", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000043"), "RastreoUsers.Create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Rastreo", "Crear usuarios de rastreo", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000044"), "RastreoUsers.ResetPassword", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Rastreo", "Restablecer contraseña de rastreo", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000045"), "RastreoUsers.ManageRole", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Rastreo", "Cambiar rol de usuario de rastreo", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000046"), "RastreoUsers.ManageStatus", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Rastreo", "Activar/desactivar usuario rastreo", null, null }
                });

            migrationBuilder.InsertData(
                schema: "CORE",
                table: "RolesPermisos",
                columns: new[] { "PermisoId", "RolId", "AsignadoEn", "AsignadoPor" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000042"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000043"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000044"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000045"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000046"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000042"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000043"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000044"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000045"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "RolesPermisos",
                keyColumns: new[] { "PermisoId", "RolId" },
                keyValues: new object[] { new Guid("a0000001-0000-0000-0000-000000000046"), new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000042"));

            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000043"));

            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000044"));

            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000045"));

            migrationBuilder.DeleteData(
                schema: "CORE",
                table: "Permisos",
                keyColumn: "Id",
                keyValue: new Guid("a0000001-0000-0000-0000-000000000046"));
        }
    }
}
