using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CredencialesActivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CredencialesActivo",
                schema: "SOPORTE",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActivoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Cuenta = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SecretoCifrado = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Host = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CredencialesActivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CredencialesActivo_Activos_ActivoId",
                        column: x => x.ActivoId,
                        principalSchema: "SOPORTE",
                        principalTable: "Activos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CredencialesActivo_ActivoId_Orden",
                schema: "SOPORTE",
                table: "CredencialesActivo",
                columns: new[] { "ActivoId", "Orden" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CredencialesActivo",
                schema: "SOPORTE");
        }
    }
}
