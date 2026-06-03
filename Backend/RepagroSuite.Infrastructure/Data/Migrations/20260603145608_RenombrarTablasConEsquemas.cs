using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenombrarTablasConEsquemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TI_Activos_TI_Colaboradores_ResponsableActualId",
                table: "TI_Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Activos_TI_Departamentos_DepartamentoId",
                table: "TI_Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Activos_TI_Marcas_MarcaId",
                table: "TI_Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Activos_TI_TiposActivo_TipoActivoId",
                table: "TI_Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Activos_TI_Ubicaciones_UbicacionId",
                table: "TI_Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Asignaciones_TI_Activos_ActivoId",
                table: "TI_Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Asignaciones_TI_Boletas_BoletaDevolucionId",
                table: "TI_Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Asignaciones_TI_Boletas_BoletaEntregaId",
                table: "TI_Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Asignaciones_TI_Colaboradores_ColaboradorId",
                table: "TI_Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Boletas_TI_Colaboradores_ColaboradorId",
                table: "TI_Boletas");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_Boletas_Usuarios_ResponsableTiId",
                table: "TI_Boletas");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_DetalleBoleta_TI_Activos_ActivoId",
                table: "TI_DetalleBoleta");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_DetalleBoleta_TI_Boletas_BoletaId",
                table: "TI_DetalleBoleta");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_EspecificacionesActivo_TI_Activos_ActivoId",
                table: "TI_EspecificacionesActivo");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_FirmasBoleta_TI_Boletas_BoletaId",
                table: "TI_FirmasBoleta");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_FotosBoleta_TI_Boletas_BoletaId",
                table: "TI_FotosBoleta");

            migrationBuilder.DropForeignKey(
                name: "FK_TI_HistorialActivo_TI_Activos_ActivoId",
                table: "TI_HistorialActivo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_Ubicaciones",
                table: "TI_Ubicaciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_TiposActivo",
                table: "TI_TiposActivo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_Marcas",
                table: "TI_Marcas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_HistorialActivo",
                table: "TI_HistorialActivo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_FotosBoleta",
                table: "TI_FotosBoleta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_FirmasBoleta",
                table: "TI_FirmasBoleta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_EspecificacionesActivo",
                table: "TI_EspecificacionesActivo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_DetalleBoleta",
                table: "TI_DetalleBoleta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_Departamentos",
                table: "TI_Departamentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_ConsecutivosDocumento",
                table: "TI_ConsecutivosDocumento");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_Colaboradores",
                table: "TI_Colaboradores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_Boletas",
                table: "TI_Boletas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_Asignaciones",
                table: "TI_Asignaciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TI_Activos",
                table: "TI_Activos");

            migrationBuilder.EnsureSchema(
                name: "SOPORTE");

            migrationBuilder.EnsureSchema(
                name: "SALAS");

            migrationBuilder.EnsureSchema(
                name: "CORE");

            migrationBuilder.RenameTable(
                name: "UsuariosRoles",
                newName: "UsuariosRoles",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "Usuarios",
                newName: "Usuarios",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "TokensRenovacion",
                newName: "TokensRenovacion",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "SalasCaracteristicas",
                newName: "SalasCaracteristicas",
                newSchema: "SALAS");

            migrationBuilder.RenameTable(
                name: "Salas",
                newName: "Salas",
                newSchema: "SALAS");

            migrationBuilder.RenameTable(
                name: "RolesPermisos",
                newName: "RolesPermisos",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "Roles",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "Reservas",
                newName: "Reservas",
                newSchema: "SALAS");

            migrationBuilder.RenameTable(
                name: "RegistrosAuditoria",
                newName: "RegistrosAuditoria",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "Permisos",
                newName: "Permisos",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "Notificaciones",
                newName: "Notificaciones",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "ModulosSistema",
                newName: "ModulosSistema",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "DisponibilidadSalas",
                newName: "DisponibilidadSalas",
                newSchema: "SALAS");

            migrationBuilder.RenameTable(
                name: "ConfiguracionSistema",
                newName: "ConfiguracionSistema",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "Caracteristicas",
                newName: "Caracteristicas",
                newSchema: "SALAS");

            migrationBuilder.RenameTable(
                name: "CacheIdentificaciones",
                newName: "CacheIdentificaciones",
                newSchema: "CORE");

            migrationBuilder.RenameTable(
                name: "BloquesSalas",
                newName: "BloquesSalas",
                newSchema: "SALAS");

            migrationBuilder.RenameTable(
                name: "TI_Ubicaciones",
                newName: "Ubicaciones",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_TiposActivo",
                newName: "TiposActivo",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_Marcas",
                newName: "Marcas",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_HistorialActivo",
                newName: "HistorialActivo",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_FotosBoleta",
                newName: "FotosBoleta",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_FirmasBoleta",
                newName: "FirmasBoleta",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_EspecificacionesActivo",
                newName: "EspecificacionesActivo",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_DetalleBoleta",
                newName: "DetalleBoleta",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_Departamentos",
                newName: "Departamentos",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_ConsecutivosDocumento",
                newName: "ConsecutivosDocumento",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_Colaboradores",
                newName: "Colaboradores",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_Boletas",
                newName: "Boletas",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_Asignaciones",
                newName: "Asignaciones",
                newSchema: "SOPORTE");

            migrationBuilder.RenameTable(
                name: "TI_Activos",
                newName: "Activos",
                newSchema: "SOPORTE");

            migrationBuilder.RenameIndex(
                name: "IX_TI_TiposActivo_Codigo",
                schema: "SOPORTE",
                table: "TiposActivo",
                newName: "IX_TiposActivo_Codigo");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Marcas_Nombre",
                schema: "SOPORTE",
                table: "Marcas",
                newName: "IX_Marcas_Nombre");

            migrationBuilder.RenameIndex(
                name: "IX_TI_HistorialActivo_ActivoId_OcurrioEn",
                schema: "SOPORTE",
                table: "HistorialActivo",
                newName: "IX_HistorialActivo_ActivoId_OcurrioEn");

            migrationBuilder.RenameIndex(
                name: "IX_TI_FotosBoleta_BoletaId",
                schema: "SOPORTE",
                table: "FotosBoleta",
                newName: "IX_FotosBoleta_BoletaId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_FirmasBoleta_BoletaId",
                schema: "SOPORTE",
                table: "FirmasBoleta",
                newName: "IX_FirmasBoleta_BoletaId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_EspecificacionesActivo_ActivoId",
                schema: "SOPORTE",
                table: "EspecificacionesActivo",
                newName: "IX_EspecificacionesActivo_ActivoId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_DetalleBoleta_BoletaId",
                schema: "SOPORTE",
                table: "DetalleBoleta",
                newName: "IX_DetalleBoleta_BoletaId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_DetalleBoleta_ActivoId",
                schema: "SOPORTE",
                table: "DetalleBoleta",
                newName: "IX_DetalleBoleta_ActivoId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_ConsecutivosDocumento_CodigoTipo_Anio",
                schema: "SOPORTE",
                table: "ConsecutivosDocumento",
                newName: "IX_ConsecutivosDocumento_CodigoTipo_Anio");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Colaboradores_IdentificacionNormalizada",
                schema: "SOPORTE",
                table: "Colaboradores",
                newName: "IX_Colaboradores_IdentificacionNormalizada");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Boletas_TipoBoleta_Estado",
                schema: "SOPORTE",
                table: "Boletas",
                newName: "IX_Boletas_TipoBoleta_Estado");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Boletas_ResponsableTiId",
                schema: "SOPORTE",
                table: "Boletas",
                newName: "IX_Boletas_ResponsableTiId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Boletas_EmitidaEn",
                schema: "SOPORTE",
                table: "Boletas",
                newName: "IX_Boletas_EmitidaEn");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Boletas_Consecutivo",
                schema: "SOPORTE",
                table: "Boletas",
                newName: "IX_Boletas_Consecutivo");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Boletas_ColaboradorId",
                schema: "SOPORTE",
                table: "Boletas",
                newName: "IX_Boletas_ColaboradorId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Asignaciones_ColaboradorId",
                schema: "SOPORTE",
                table: "Asignaciones",
                newName: "IX_Asignaciones_ColaboradorId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Asignaciones_BoletaEntregaId",
                schema: "SOPORTE",
                table: "Asignaciones",
                newName: "IX_Asignaciones_BoletaEntregaId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Asignaciones_BoletaDevolucionId",
                schema: "SOPORTE",
                table: "Asignaciones",
                newName: "IX_Asignaciones_BoletaDevolucionId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Asignaciones_ActivoId",
                schema: "SOPORTE",
                table: "Asignaciones",
                newName: "IX_Asignaciones_ActivoId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Activos_UbicacionId",
                schema: "SOPORTE",
                table: "Activos",
                newName: "IX_Activos_UbicacionId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Activos_TipoActivoId",
                schema: "SOPORTE",
                table: "Activos",
                newName: "IX_Activos_TipoActivoId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Activos_ResponsableActualId",
                schema: "SOPORTE",
                table: "Activos",
                newName: "IX_Activos_ResponsableActualId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Activos_NumeroSerie",
                schema: "SOPORTE",
                table: "Activos",
                newName: "IX_Activos_NumeroSerie");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Activos_MarcaId",
                schema: "SOPORTE",
                table: "Activos",
                newName: "IX_Activos_MarcaId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Activos_FechaVencimientoGarantia",
                schema: "SOPORTE",
                table: "Activos",
                newName: "IX_Activos_FechaVencimientoGarantia");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Activos_Estado",
                schema: "SOPORTE",
                table: "Activos",
                newName: "IX_Activos_Estado");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Activos_DepartamentoId",
                schema: "SOPORTE",
                table: "Activos",
                newName: "IX_Activos_DepartamentoId");

            migrationBuilder.RenameIndex(
                name: "IX_TI_Activos_CodigoInterno",
                schema: "SOPORTE",
                table: "Activos",
                newName: "IX_Activos_CodigoInterno");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Ubicaciones",
                schema: "SOPORTE",
                table: "Ubicaciones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TiposActivo",
                schema: "SOPORTE",
                table: "TiposActivo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Marcas",
                schema: "SOPORTE",
                table: "Marcas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HistorialActivo",
                schema: "SOPORTE",
                table: "HistorialActivo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FotosBoleta",
                schema: "SOPORTE",
                table: "FotosBoleta",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_FirmasBoleta",
                schema: "SOPORTE",
                table: "FirmasBoleta",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EspecificacionesActivo",
                schema: "SOPORTE",
                table: "EspecificacionesActivo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DetalleBoleta",
                schema: "SOPORTE",
                table: "DetalleBoleta",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Departamentos",
                schema: "SOPORTE",
                table: "Departamentos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConsecutivosDocumento",
                schema: "SOPORTE",
                table: "ConsecutivosDocumento",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Colaboradores",
                schema: "SOPORTE",
                table: "Colaboradores",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Boletas",
                schema: "SOPORTE",
                table: "Boletas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Asignaciones",
                schema: "SOPORTE",
                table: "Asignaciones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Activos",
                schema: "SOPORTE",
                table: "Activos",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Activos_Colaboradores_ResponsableActualId",
                schema: "SOPORTE",
                table: "Activos",
                column: "ResponsableActualId",
                principalSchema: "SOPORTE",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Activos_Departamentos_DepartamentoId",
                schema: "SOPORTE",
                table: "Activos",
                column: "DepartamentoId",
                principalSchema: "SOPORTE",
                principalTable: "Departamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Activos_Marcas_MarcaId",
                schema: "SOPORTE",
                table: "Activos",
                column: "MarcaId",
                principalSchema: "SOPORTE",
                principalTable: "Marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Activos_TiposActivo_TipoActivoId",
                schema: "SOPORTE",
                table: "Activos",
                column: "TipoActivoId",
                principalSchema: "SOPORTE",
                principalTable: "TiposActivo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Activos_Ubicaciones_UbicacionId",
                schema: "SOPORTE",
                table: "Activos",
                column: "UbicacionId",
                principalSchema: "SOPORTE",
                principalTable: "Ubicaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Activos_ActivoId",
                schema: "SOPORTE",
                table: "Asignaciones",
                column: "ActivoId",
                principalSchema: "SOPORTE",
                principalTable: "Activos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Boletas_BoletaDevolucionId",
                schema: "SOPORTE",
                table: "Asignaciones",
                column: "BoletaDevolucionId",
                principalSchema: "SOPORTE",
                principalTable: "Boletas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Boletas_BoletaEntregaId",
                schema: "SOPORTE",
                table: "Asignaciones",
                column: "BoletaEntregaId",
                principalSchema: "SOPORTE",
                principalTable: "Boletas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Asignaciones_Colaboradores_ColaboradorId",
                schema: "SOPORTE",
                table: "Asignaciones",
                column: "ColaboradorId",
                principalSchema: "SOPORTE",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Boletas_Colaboradores_ColaboradorId",
                schema: "SOPORTE",
                table: "Boletas",
                column: "ColaboradorId",
                principalSchema: "SOPORTE",
                principalTable: "Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Boletas_Usuarios_ResponsableTiId",
                schema: "SOPORTE",
                table: "Boletas",
                column: "ResponsableTiId",
                principalSchema: "CORE",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetalleBoleta_Activos_ActivoId",
                schema: "SOPORTE",
                table: "DetalleBoleta",
                column: "ActivoId",
                principalSchema: "SOPORTE",
                principalTable: "Activos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetalleBoleta_Boletas_BoletaId",
                schema: "SOPORTE",
                table: "DetalleBoleta",
                column: "BoletaId",
                principalSchema: "SOPORTE",
                principalTable: "Boletas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EspecificacionesActivo_Activos_ActivoId",
                schema: "SOPORTE",
                table: "EspecificacionesActivo",
                column: "ActivoId",
                principalSchema: "SOPORTE",
                principalTable: "Activos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FirmasBoleta_Boletas_BoletaId",
                schema: "SOPORTE",
                table: "FirmasBoleta",
                column: "BoletaId",
                principalSchema: "SOPORTE",
                principalTable: "Boletas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FotosBoleta_Boletas_BoletaId",
                schema: "SOPORTE",
                table: "FotosBoleta",
                column: "BoletaId",
                principalSchema: "SOPORTE",
                principalTable: "Boletas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialActivo_Activos_ActivoId",
                schema: "SOPORTE",
                table: "HistorialActivo",
                column: "ActivoId",
                principalSchema: "SOPORTE",
                principalTable: "Activos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Activos_Colaboradores_ResponsableActualId",
                schema: "SOPORTE",
                table: "Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_Activos_Departamentos_DepartamentoId",
                schema: "SOPORTE",
                table: "Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_Activos_Marcas_MarcaId",
                schema: "SOPORTE",
                table: "Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_Activos_TiposActivo_TipoActivoId",
                schema: "SOPORTE",
                table: "Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_Activos_Ubicaciones_UbicacionId",
                schema: "SOPORTE",
                table: "Activos");

            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Activos_ActivoId",
                schema: "SOPORTE",
                table: "Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Boletas_BoletaDevolucionId",
                schema: "SOPORTE",
                table: "Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Boletas_BoletaEntregaId",
                schema: "SOPORTE",
                table: "Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Asignaciones_Colaboradores_ColaboradorId",
                schema: "SOPORTE",
                table: "Asignaciones");

            migrationBuilder.DropForeignKey(
                name: "FK_Boletas_Colaboradores_ColaboradorId",
                schema: "SOPORTE",
                table: "Boletas");

            migrationBuilder.DropForeignKey(
                name: "FK_Boletas_Usuarios_ResponsableTiId",
                schema: "SOPORTE",
                table: "Boletas");

            migrationBuilder.DropForeignKey(
                name: "FK_DetalleBoleta_Activos_ActivoId",
                schema: "SOPORTE",
                table: "DetalleBoleta");

            migrationBuilder.DropForeignKey(
                name: "FK_DetalleBoleta_Boletas_BoletaId",
                schema: "SOPORTE",
                table: "DetalleBoleta");

            migrationBuilder.DropForeignKey(
                name: "FK_EspecificacionesActivo_Activos_ActivoId",
                schema: "SOPORTE",
                table: "EspecificacionesActivo");

            migrationBuilder.DropForeignKey(
                name: "FK_FirmasBoleta_Boletas_BoletaId",
                schema: "SOPORTE",
                table: "FirmasBoleta");

            migrationBuilder.DropForeignKey(
                name: "FK_FotosBoleta_Boletas_BoletaId",
                schema: "SOPORTE",
                table: "FotosBoleta");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialActivo_Activos_ActivoId",
                schema: "SOPORTE",
                table: "HistorialActivo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Ubicaciones",
                schema: "SOPORTE",
                table: "Ubicaciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TiposActivo",
                schema: "SOPORTE",
                table: "TiposActivo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Marcas",
                schema: "SOPORTE",
                table: "Marcas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HistorialActivo",
                schema: "SOPORTE",
                table: "HistorialActivo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FotosBoleta",
                schema: "SOPORTE",
                table: "FotosBoleta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_FirmasBoleta",
                schema: "SOPORTE",
                table: "FirmasBoleta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EspecificacionesActivo",
                schema: "SOPORTE",
                table: "EspecificacionesActivo");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DetalleBoleta",
                schema: "SOPORTE",
                table: "DetalleBoleta");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Departamentos",
                schema: "SOPORTE",
                table: "Departamentos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConsecutivosDocumento",
                schema: "SOPORTE",
                table: "ConsecutivosDocumento");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Colaboradores",
                schema: "SOPORTE",
                table: "Colaboradores");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Boletas",
                schema: "SOPORTE",
                table: "Boletas");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Asignaciones",
                schema: "SOPORTE",
                table: "Asignaciones");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Activos",
                schema: "SOPORTE",
                table: "Activos");

            migrationBuilder.RenameTable(
                name: "UsuariosRoles",
                schema: "CORE",
                newName: "UsuariosRoles");

            migrationBuilder.RenameTable(
                name: "Usuarios",
                schema: "CORE",
                newName: "Usuarios");

            migrationBuilder.RenameTable(
                name: "TokensRenovacion",
                schema: "CORE",
                newName: "TokensRenovacion");

            migrationBuilder.RenameTable(
                name: "SalasCaracteristicas",
                schema: "SALAS",
                newName: "SalasCaracteristicas");

            migrationBuilder.RenameTable(
                name: "Salas",
                schema: "SALAS",
                newName: "Salas");

            migrationBuilder.RenameTable(
                name: "RolesPermisos",
                schema: "CORE",
                newName: "RolesPermisos");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "CORE",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "Reservas",
                schema: "SALAS",
                newName: "Reservas");

            migrationBuilder.RenameTable(
                name: "RegistrosAuditoria",
                schema: "CORE",
                newName: "RegistrosAuditoria");

            migrationBuilder.RenameTable(
                name: "Permisos",
                schema: "CORE",
                newName: "Permisos");

            migrationBuilder.RenameTable(
                name: "Notificaciones",
                schema: "CORE",
                newName: "Notificaciones");

            migrationBuilder.RenameTable(
                name: "ModulosSistema",
                schema: "CORE",
                newName: "ModulosSistema");

            migrationBuilder.RenameTable(
                name: "DisponibilidadSalas",
                schema: "SALAS",
                newName: "DisponibilidadSalas");

            migrationBuilder.RenameTable(
                name: "ConfiguracionSistema",
                schema: "CORE",
                newName: "ConfiguracionSistema");

            migrationBuilder.RenameTable(
                name: "Caracteristicas",
                schema: "SALAS",
                newName: "Caracteristicas");

            migrationBuilder.RenameTable(
                name: "CacheIdentificaciones",
                schema: "CORE",
                newName: "CacheIdentificaciones");

            migrationBuilder.RenameTable(
                name: "BloquesSalas",
                schema: "SALAS",
                newName: "BloquesSalas");

            migrationBuilder.RenameTable(
                name: "Ubicaciones",
                schema: "SOPORTE",
                newName: "TI_Ubicaciones");

            migrationBuilder.RenameTable(
                name: "TiposActivo",
                schema: "SOPORTE",
                newName: "TI_TiposActivo");

            migrationBuilder.RenameTable(
                name: "Marcas",
                schema: "SOPORTE",
                newName: "TI_Marcas");

            migrationBuilder.RenameTable(
                name: "HistorialActivo",
                schema: "SOPORTE",
                newName: "TI_HistorialActivo");

            migrationBuilder.RenameTable(
                name: "FotosBoleta",
                schema: "SOPORTE",
                newName: "TI_FotosBoleta");

            migrationBuilder.RenameTable(
                name: "FirmasBoleta",
                schema: "SOPORTE",
                newName: "TI_FirmasBoleta");

            migrationBuilder.RenameTable(
                name: "EspecificacionesActivo",
                schema: "SOPORTE",
                newName: "TI_EspecificacionesActivo");

            migrationBuilder.RenameTable(
                name: "DetalleBoleta",
                schema: "SOPORTE",
                newName: "TI_DetalleBoleta");

            migrationBuilder.RenameTable(
                name: "Departamentos",
                schema: "SOPORTE",
                newName: "TI_Departamentos");

            migrationBuilder.RenameTable(
                name: "ConsecutivosDocumento",
                schema: "SOPORTE",
                newName: "TI_ConsecutivosDocumento");

            migrationBuilder.RenameTable(
                name: "Colaboradores",
                schema: "SOPORTE",
                newName: "TI_Colaboradores");

            migrationBuilder.RenameTable(
                name: "Boletas",
                schema: "SOPORTE",
                newName: "TI_Boletas");

            migrationBuilder.RenameTable(
                name: "Asignaciones",
                schema: "SOPORTE",
                newName: "TI_Asignaciones");

            migrationBuilder.RenameTable(
                name: "Activos",
                schema: "SOPORTE",
                newName: "TI_Activos");

            migrationBuilder.RenameIndex(
                name: "IX_TiposActivo_Codigo",
                table: "TI_TiposActivo",
                newName: "IX_TI_TiposActivo_Codigo");

            migrationBuilder.RenameIndex(
                name: "IX_Marcas_Nombre",
                table: "TI_Marcas",
                newName: "IX_TI_Marcas_Nombre");

            migrationBuilder.RenameIndex(
                name: "IX_HistorialActivo_ActivoId_OcurrioEn",
                table: "TI_HistorialActivo",
                newName: "IX_TI_HistorialActivo_ActivoId_OcurrioEn");

            migrationBuilder.RenameIndex(
                name: "IX_FotosBoleta_BoletaId",
                table: "TI_FotosBoleta",
                newName: "IX_TI_FotosBoleta_BoletaId");

            migrationBuilder.RenameIndex(
                name: "IX_FirmasBoleta_BoletaId",
                table: "TI_FirmasBoleta",
                newName: "IX_TI_FirmasBoleta_BoletaId");

            migrationBuilder.RenameIndex(
                name: "IX_EspecificacionesActivo_ActivoId",
                table: "TI_EspecificacionesActivo",
                newName: "IX_TI_EspecificacionesActivo_ActivoId");

            migrationBuilder.RenameIndex(
                name: "IX_DetalleBoleta_BoletaId",
                table: "TI_DetalleBoleta",
                newName: "IX_TI_DetalleBoleta_BoletaId");

            migrationBuilder.RenameIndex(
                name: "IX_DetalleBoleta_ActivoId",
                table: "TI_DetalleBoleta",
                newName: "IX_TI_DetalleBoleta_ActivoId");

            migrationBuilder.RenameIndex(
                name: "IX_ConsecutivosDocumento_CodigoTipo_Anio",
                table: "TI_ConsecutivosDocumento",
                newName: "IX_TI_ConsecutivosDocumento_CodigoTipo_Anio");

            migrationBuilder.RenameIndex(
                name: "IX_Colaboradores_IdentificacionNormalizada",
                table: "TI_Colaboradores",
                newName: "IX_TI_Colaboradores_IdentificacionNormalizada");

            migrationBuilder.RenameIndex(
                name: "IX_Boletas_TipoBoleta_Estado",
                table: "TI_Boletas",
                newName: "IX_TI_Boletas_TipoBoleta_Estado");

            migrationBuilder.RenameIndex(
                name: "IX_Boletas_ResponsableTiId",
                table: "TI_Boletas",
                newName: "IX_TI_Boletas_ResponsableTiId");

            migrationBuilder.RenameIndex(
                name: "IX_Boletas_EmitidaEn",
                table: "TI_Boletas",
                newName: "IX_TI_Boletas_EmitidaEn");

            migrationBuilder.RenameIndex(
                name: "IX_Boletas_Consecutivo",
                table: "TI_Boletas",
                newName: "IX_TI_Boletas_Consecutivo");

            migrationBuilder.RenameIndex(
                name: "IX_Boletas_ColaboradorId",
                table: "TI_Boletas",
                newName: "IX_TI_Boletas_ColaboradorId");

            migrationBuilder.RenameIndex(
                name: "IX_Asignaciones_ColaboradorId",
                table: "TI_Asignaciones",
                newName: "IX_TI_Asignaciones_ColaboradorId");

            migrationBuilder.RenameIndex(
                name: "IX_Asignaciones_BoletaEntregaId",
                table: "TI_Asignaciones",
                newName: "IX_TI_Asignaciones_BoletaEntregaId");

            migrationBuilder.RenameIndex(
                name: "IX_Asignaciones_BoletaDevolucionId",
                table: "TI_Asignaciones",
                newName: "IX_TI_Asignaciones_BoletaDevolucionId");

            migrationBuilder.RenameIndex(
                name: "IX_Asignaciones_ActivoId",
                table: "TI_Asignaciones",
                newName: "IX_TI_Asignaciones_ActivoId");

            migrationBuilder.RenameIndex(
                name: "IX_Activos_UbicacionId",
                table: "TI_Activos",
                newName: "IX_TI_Activos_UbicacionId");

            migrationBuilder.RenameIndex(
                name: "IX_Activos_TipoActivoId",
                table: "TI_Activos",
                newName: "IX_TI_Activos_TipoActivoId");

            migrationBuilder.RenameIndex(
                name: "IX_Activos_ResponsableActualId",
                table: "TI_Activos",
                newName: "IX_TI_Activos_ResponsableActualId");

            migrationBuilder.RenameIndex(
                name: "IX_Activos_NumeroSerie",
                table: "TI_Activos",
                newName: "IX_TI_Activos_NumeroSerie");

            migrationBuilder.RenameIndex(
                name: "IX_Activos_MarcaId",
                table: "TI_Activos",
                newName: "IX_TI_Activos_MarcaId");

            migrationBuilder.RenameIndex(
                name: "IX_Activos_FechaVencimientoGarantia",
                table: "TI_Activos",
                newName: "IX_TI_Activos_FechaVencimientoGarantia");

            migrationBuilder.RenameIndex(
                name: "IX_Activos_Estado",
                table: "TI_Activos",
                newName: "IX_TI_Activos_Estado");

            migrationBuilder.RenameIndex(
                name: "IX_Activos_DepartamentoId",
                table: "TI_Activos",
                newName: "IX_TI_Activos_DepartamentoId");

            migrationBuilder.RenameIndex(
                name: "IX_Activos_CodigoInterno",
                table: "TI_Activos",
                newName: "IX_TI_Activos_CodigoInterno");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_Ubicaciones",
                table: "TI_Ubicaciones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_TiposActivo",
                table: "TI_TiposActivo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_Marcas",
                table: "TI_Marcas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_HistorialActivo",
                table: "TI_HistorialActivo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_FotosBoleta",
                table: "TI_FotosBoleta",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_FirmasBoleta",
                table: "TI_FirmasBoleta",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_EspecificacionesActivo",
                table: "TI_EspecificacionesActivo",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_DetalleBoleta",
                table: "TI_DetalleBoleta",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_Departamentos",
                table: "TI_Departamentos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_ConsecutivosDocumento",
                table: "TI_ConsecutivosDocumento",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_Colaboradores",
                table: "TI_Colaboradores",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_Boletas",
                table: "TI_Boletas",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_Asignaciones",
                table: "TI_Asignaciones",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TI_Activos",
                table: "TI_Activos",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Activos_TI_Colaboradores_ResponsableActualId",
                table: "TI_Activos",
                column: "ResponsableActualId",
                principalTable: "TI_Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Activos_TI_Departamentos_DepartamentoId",
                table: "TI_Activos",
                column: "DepartamentoId",
                principalTable: "TI_Departamentos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Activos_TI_Marcas_MarcaId",
                table: "TI_Activos",
                column: "MarcaId",
                principalTable: "TI_Marcas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Activos_TI_TiposActivo_TipoActivoId",
                table: "TI_Activos",
                column: "TipoActivoId",
                principalTable: "TI_TiposActivo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Activos_TI_Ubicaciones_UbicacionId",
                table: "TI_Activos",
                column: "UbicacionId",
                principalTable: "TI_Ubicaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Asignaciones_TI_Activos_ActivoId",
                table: "TI_Asignaciones",
                column: "ActivoId",
                principalTable: "TI_Activos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Asignaciones_TI_Boletas_BoletaDevolucionId",
                table: "TI_Asignaciones",
                column: "BoletaDevolucionId",
                principalTable: "TI_Boletas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Asignaciones_TI_Boletas_BoletaEntregaId",
                table: "TI_Asignaciones",
                column: "BoletaEntregaId",
                principalTable: "TI_Boletas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Asignaciones_TI_Colaboradores_ColaboradorId",
                table: "TI_Asignaciones",
                column: "ColaboradorId",
                principalTable: "TI_Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Boletas_TI_Colaboradores_ColaboradorId",
                table: "TI_Boletas",
                column: "ColaboradorId",
                principalTable: "TI_Colaboradores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_Boletas_Usuarios_ResponsableTiId",
                table: "TI_Boletas",
                column: "ResponsableTiId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_DetalleBoleta_TI_Activos_ActivoId",
                table: "TI_DetalleBoleta",
                column: "ActivoId",
                principalTable: "TI_Activos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_DetalleBoleta_TI_Boletas_BoletaId",
                table: "TI_DetalleBoleta",
                column: "BoletaId",
                principalTable: "TI_Boletas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_EspecificacionesActivo_TI_Activos_ActivoId",
                table: "TI_EspecificacionesActivo",
                column: "ActivoId",
                principalTable: "TI_Activos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_FirmasBoleta_TI_Boletas_BoletaId",
                table: "TI_FirmasBoleta",
                column: "BoletaId",
                principalTable: "TI_Boletas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_FotosBoleta_TI_Boletas_BoletaId",
                table: "TI_FotosBoleta",
                column: "BoletaId",
                principalTable: "TI_Boletas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TI_HistorialActivo_TI_Activos_ActivoId",
                table: "TI_HistorialActivo",
                column: "ActivoId",
                principalTable: "TI_Activos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
