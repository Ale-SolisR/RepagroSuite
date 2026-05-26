using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgruparReservasRecurrentes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GrupoRecurrenciaId",
                table: "Reservas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_GrupoRecurrenciaId",
                table: "Reservas",
                column: "GrupoRecurrenciaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservas_GrupoRecurrenciaId",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "GrupoRecurrenciaId",
                table: "Reservas");
        }
    }
}
