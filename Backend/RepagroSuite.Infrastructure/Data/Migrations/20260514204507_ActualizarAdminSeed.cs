using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ActualizarAdminSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CorreoElectronico", "NombreCompleto", "Apellidos", "PrimerApellido", "SegundoApellido", "CorreoElectronicoNorm", "HashContrasena" },
                values: new object[] { "gestionwebrepagro@gmail.com", "ADMINISTRADOR REPAGRO", "REPAGRO", "REPAGRO", "", "GESTIONWEBREPAGRO@GMAIL.COM", "$2a$11$SVxo12avCQjNSUPOFt3sBuYOh0nY7sax3RtpH3TxWVVNT2Y0mvPSu" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"),
                columns: new[] { "CorreoElectronico", "NombreCompleto", "Apellidos", "PrimerApellido", "SegundoApellido", "CorreoElectronicoNorm", "HashContrasena" },
                values: new object[] { "admin@repagro.com", "ADMINISTRADOR DEL SISTEMA", "DEL SISTEMA", "DEL", "SISTEMA", "ADMIN@REPAGRO.COM", "$2a$11$K2CtDP7zSGOKgjXjVy9TSOc.vSlm2dn0EkK0EZ.UpIwAbvBFAFhUy" });
        }
    }
}
