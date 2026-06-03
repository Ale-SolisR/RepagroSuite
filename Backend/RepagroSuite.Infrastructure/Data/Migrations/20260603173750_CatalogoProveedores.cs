using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CatalogoProveedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Tabla de catálogo de proveedores.
            migrationBuilder.CreateTable(
                name: "Proveedores",
                schema: "SOPORTE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
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
                    table.PrimaryKey("PK_Proveedores", x => x.Id);
                });

            // 2) Nueva FK en Activos (nullable).
            migrationBuilder.AddColumn<Guid>(
                name: "ProveedorId",
                schema: "SOPORTE",
                table: "Activos",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Proveedores_Nombre",
                schema: "SOPORTE",
                table: "Proveedores",
                column: "Nombre",
                unique: true,
                filter: "[EliminadoLogico] = 0");

            // 3) Migración de datos: convertir el texto libre 'Proveedor' en filas del catálogo
            //    y enlazar cada activo a su proveedor por nombre (collation CI = no duplica por mayúsculas).
            //    VersionFila es rowversion: la genera SQL Server, no se inserta.
            migrationBuilder.Sql(@"
INSERT INTO SOPORTE.Proveedores (Id, Nombre, EsActivo, CreadoEn, EliminadoLogico)
SELECT NEWID(), x.Nombre, 1, SYSUTCDATETIME(), 0
FROM (
    SELECT DISTINCT LTRIM(RTRIM(Proveedor)) AS Nombre
    FROM SOPORTE.Activos
    WHERE Proveedor IS NOT NULL AND LTRIM(RTRIM(Proveedor)) <> ''
) x;

UPDATE act
SET act.ProveedorId = p.Id
FROM SOPORTE.Activos act
INNER JOIN SOPORTE.Proveedores p ON p.Nombre = LTRIM(RTRIM(act.Proveedor))
WHERE act.Proveedor IS NOT NULL AND LTRIM(RTRIM(act.Proveedor)) <> '';
");

            // 4) Índice + FK ahora que los datos están enlazados.
            migrationBuilder.CreateIndex(
                name: "IX_Activos_ProveedorId",
                schema: "SOPORTE",
                table: "Activos",
                column: "ProveedorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Activos_Proveedores_ProveedorId",
                schema: "SOPORTE",
                table: "Activos",
                column: "ProveedorId",
                principalSchema: "SOPORTE",
                principalTable: "Proveedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 5) Eliminar la columna de texto libre ya migrada.
            migrationBuilder.DropColumn(
                name: "Proveedor",
                schema: "SOPORTE",
                table: "Activos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recrear la columna de texto y devolver el nombre del proveedor desde el catálogo.
            migrationBuilder.AddColumn<string>(
                name: "Proveedor",
                schema: "SOPORTE",
                table: "Activos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE act
SET act.Proveedor = p.Nombre
FROM SOPORTE.Activos act
INNER JOIN SOPORTE.Proveedores p ON p.Id = act.ProveedorId;
");

            migrationBuilder.DropForeignKey(
                name: "FK_Activos_Proveedores_ProveedorId",
                schema: "SOPORTE",
                table: "Activos");

            migrationBuilder.DropIndex(
                name: "IX_Activos_ProveedorId",
                schema: "SOPORTE",
                table: "Activos");

            migrationBuilder.DropColumn(
                name: "ProveedorId",
                schema: "SOPORTE",
                table: "Activos");

            migrationBuilder.DropTable(
                name: "Proveedores",
                schema: "SOPORTE");
        }
    }
}
