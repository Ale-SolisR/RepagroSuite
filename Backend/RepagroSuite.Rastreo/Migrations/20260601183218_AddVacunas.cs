using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rastreo.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVacunas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UsaVacunas",
                schema: "RASTREO",
                table: "Registros",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Vacunas",
                schema: "RASTREO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Laboratorio = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vacunas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegistroVacunas",
                schema: "RASTREO",
                columns: table => new
                {
                    RegistroId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VacunaId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistroVacunas", x => new { x.RegistroId, x.VacunaId });
                    table.ForeignKey(
                        name: "FK_RegistroVacunas_Registros_RegistroId",
                        column: x => x.RegistroId,
                        principalSchema: "RASTREO",
                        principalTable: "Registros",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistroVacunas_Vacunas_VacunaId",
                        column: x => x.VacunaId,
                        principalSchema: "RASTREO",
                        principalTable: "Vacunas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistroVacunas_VacunaId",
                schema: "RASTREO",
                table: "RegistroVacunas",
                column: "VacunaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacunas_Codigo",
                schema: "RASTREO",
                table: "Vacunas",
                column: "Codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistroVacunas",
                schema: "RASTREO");

            migrationBuilder.DropTable(
                name: "Vacunas",
                schema: "RASTREO");

            migrationBuilder.DropColumn(
                name: "UsaVacunas",
                schema: "RASTREO",
                table: "Registros");
        }
    }
}
