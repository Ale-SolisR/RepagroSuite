using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rastreo.Api.Migrations
{
    /// <inheritdoc />
    public partial class DynamicEnfermedadValores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistroDetalleEnfermedadValores",
                schema: "RASTREO",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RegistroDetalleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnfermedadId = table.Column<int>(type: "int", nullable: false),
                    ValorNumero = table.Column<byte>(type: "tinyint", nullable: true),
                    ValorBooleano = table.Column<bool>(type: "bit", nullable: true),
                    ValorTexto = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistroDetalleEnfermedadValores", x => x.Id);
                    table.CheckConstraint("CK_DetalleEnf_ValorNumero", "[ValorNumero] IS NULL OR [ValorNumero] BETWEEN 0 AND 4");
                    table.CheckConstraint("CK_DetalleEnf_ValorTexto", "[ValorTexto] IS NULL OR [ValorTexto] IN ('A','C','AC')");
                    table.ForeignKey(
                        name: "FK_RegistroDetalleEnfermedadValores_Enfermedades_EnfermedadId",
                        column: x => x.EnfermedadId,
                        principalSchema: "RASTREO",
                        principalTable: "Enfermedades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistroDetalleEnfermedadValores_RegistroDetalle_RegistroDetalleId",
                        column: x => x.RegistroDetalleId,
                        principalSchema: "RASTREO",
                        principalTable: "RegistroDetalle",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistroDetalleEnfermedadValores_EnfermedadId",
                schema: "RASTREO",
                table: "RegistroDetalleEnfermedadValores",
                column: "EnfermedadId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistroDetalleEnfermedadValores_RegistroDetalleId_EnfermedadId",
                schema: "RASTREO",
                table: "RegistroDetalleEnfermedadValores",
                columns: new[] { "RegistroDetalleId", "EnfermedadId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistroDetalleEnfermedadValores",
                schema: "RASTREO");
        }
    }
}
