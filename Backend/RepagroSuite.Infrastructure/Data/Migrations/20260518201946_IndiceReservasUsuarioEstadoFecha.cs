using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IndiceReservasUsuarioEstadoFecha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioId_Estado_FechaHoraInicio",
                table: "Reservas",
                columns: new[] { "UsuarioId", "Estado", "FechaHoraInicio" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservas_UsuarioId_Estado_FechaHoraInicio",
                table: "Reservas");
        }
    }
}
