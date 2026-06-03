using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FotosActivoBinario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Nueva tabla: las fotos viven como BINARIO en la BD (nunca como URL/ruta).
            migrationBuilder.CreateTable(
                name: "FotosActivo",
                schema: "SOPORTE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    PesoBytes = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_FotosActivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FotosActivo_Activos_ActivoId",
                        column: x => x.ActivoId,
                        principalSchema: "SOPORTE",
                        principalTable: "Activos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FotosActivo_ActivoId_Orden",
                schema: "SOPORTE",
                table: "FotosActivo",
                columns: new[] { "ActivoId", "Orden" });

            // 2) Migración de datos: cada foto previa (data URL en UrlImagen) se decodifica a binario y se conserva como foto principal (Orden 0).
            migrationBuilder.Sql(@"
INSERT INTO SOPORTE.FotosActivo (Id, ActivoId, Orden, Contenido, MimeType, PesoBytes, NombreArchivo, CreadoEn, EliminadoLogico)
SELECT NEWID(), x.Id, 0,
       CAST(N'' AS XML).value('xs:base64Binary(sql:column(""x.b64""))', 'varbinary(max)'),
       x.mime,
       CAST(DATALENGTH(CAST(N'' AS XML).value('xs:base64Binary(sql:column(""x.b64""))', 'varbinary(max)')) AS int),
       x.fname,
       GETUTCDATE(), 0
FROM (
    SELECT a.Id,
           SUBSTRING(a.UrlImagen, 6, CHARINDEX(';', a.UrlImagen) - 6) AS mime,
           SUBSTRING(a.UrlImagen, CHARINDEX(',', a.UrlImagen) + 1, LEN(a.UrlImagen)) AS b64,
           a.CodigoInterno + '_1.jpg' AS fname
    FROM SOPORTE.Activos a
    WHERE a.EliminadoLogico = 0
      AND a.UrlImagen LIKE 'data:%;base64,%'
) AS x;");

            // 3) Ya migrada la evidencia, se elimina la columna de URL.
            migrationBuilder.DropColumn(
                name: "UrlImagen",
                schema: "SOPORTE",
                table: "Activos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restaura la columna y reconstruye la data URL desde el binario de la foto principal.
            migrationBuilder.AddColumn<string>(
                name: "UrlImagen",
                schema: "SOPORTE",
                table: "Activos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql(@"
UPDATE a
SET a.UrlImagen = 'data:' + p.MimeType + ';base64,' +
                  CAST(N'' AS XML).value('xs:base64Binary(sql:column(""p.Contenido""))', 'varchar(max)')
FROM SOPORTE.Activos a
INNER JOIN SOPORTE.FotosActivo p ON p.ActivoId = a.Id AND p.Orden = 0 AND p.EliminadoLogico = 0;");

            migrationBuilder.DropTable(
                name: "FotosActivo",
                schema: "SOPORTE");
        }
    }
}
