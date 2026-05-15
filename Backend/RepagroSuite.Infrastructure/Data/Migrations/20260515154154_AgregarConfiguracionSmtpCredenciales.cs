using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConfiguracionSmtpCredenciales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ConfiguracionSistema",
                columns: new[] { "Id", "CreadoEn", "CreadoPor", "TipoDato", "ValorPredeterminado", "EliminadoEn", "EliminadoPor", "Descripcion", "EliminadoLogico", "EstaEncriptado", "EsDeSoloLectura", "Clave", "Modulo", "ActualizadoEn", "ActualizadoPor", "Valor" },
                values: new object[,]
                {
                    { new Guid("b0000001-0000-0000-0000-000000000017"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "string", "", null, null, "Usuario SMTP", false, false, false, "EMAIL.SMTP_USERNAME", "EMAIL", null, null, "" },
                    { new Guid("b0000001-0000-0000-0000-000000000018"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "string", "", null, null, "Contraseña SMTP", false, true, false, "EMAIL.SMTP_PASSWORD", "EMAIL", null, null, "" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ConfiguracionSistema",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "ConfiguracionSistema",
                keyColumn: "Id",
                keyValue: new Guid("b0000001-0000-0000-0000-000000000018"));
        }
    }
}
