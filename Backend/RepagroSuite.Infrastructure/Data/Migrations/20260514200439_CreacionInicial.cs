using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RepagroSuite.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreacionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CacheIdentificaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoIdentificacion = table.Column<int>(type: "int", nullable: false),
                    NumeroIdentificacion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroIdentificacionNorm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    NombresPropios = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrimerNombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    SegundoNombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Apellidos = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PrimerApellido = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    SegundoApellido = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    NombreLegal = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Fuente = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RespuestaJsonRaw = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CantidadResultados = table.Column<int>(type: "int", nullable: false),
                    FechaBaseDatos = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UltimaConsultaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_CacheIdentificaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Caracteristicas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreIcono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_Caracteristicas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionSistema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Clave = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ValorPredeterminado = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Modulo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TipoDato = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EstaEncriptado = table.Column<bool>(type: "bit", nullable: false),
                    EsDeSoloLectura = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ConfiguracionSistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ModulosSistema",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NombreIcono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrefijoRuta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrdenVisualizacion = table.Column<int>(type: "int", nullable: false),
                    EsActivo = table.Column<bool>(type: "bit", nullable: false),
                    EsNuclear = table.Column<bool>(type: "bit", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
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
                    table.PrimaryKey("PK_ModulosSistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Modulo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreNormalizado = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EsRolDelSistema = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Salas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Capacidad = table.Column<int>(type: "int", nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Piso = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    UrlImagen = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
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
                    table.PrimaryKey("PK_Salas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoIdentificacion = table.Column<int>(type: "int", nullable: false),
                    NumeroIdentificacion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroIdentificacionNorm = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NombresPropios = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PrimerNombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    SegundoNombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Apellidos = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PrimerApellido = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    SegundoApellido = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    NombreLegal = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IdentificacionValidada = table.Column<bool>(type: "bit", nullable: false),
                    IdentificacionValidadaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FuenteValidacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CorreoElectronico = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    CorreoElectronicoNorm = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Departamento = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Puesto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    UrlImagenPerfil = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HashContrasena = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IntentosLoginFallidos = table.Column<int>(type: "int", nullable: false),
                    BloqueoHasta = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoAccesoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoCambioContrasenaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DebeActualizarContrasena = table.Column<bool>(type: "bit", nullable: false),
                    VencimientoContrasenaTemp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TokenRestablecimiento = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VencimientoTokenRestablecimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AprobadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprobadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RechazadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RechazadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolesPermisos",
                columns: table => new
                {
                    RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermisoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsignadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AsignadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesPermisos", x => new { x.RolId, x.PermisoId });
                    table.ForeignKey(
                        name: "FK_RolesPermisos_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolesPermisos_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BloquesSalas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoBloqueo = table.Column<int>(type: "int", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    EsRecurrente = table.Column<bool>(type: "bit", nullable: false),
                    DiaSemanaRecurrente = table.Column<int>(type: "int", nullable: true),
                    HoraInicio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HoraFin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaEspecifica = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaHoraInicioEspecifica = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaHoraFinEspecifica = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_BloquesSalas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BloquesSalas_Salas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DisponibilidadSalas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    EsDisponible = table.Column<bool>(type: "bit", nullable: false),
                    HoraApertura = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoraCierre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MinutosMinReserva = table.Column<int>(type: "int", nullable: false),
                    MinutosMaxReserva = table.Column<int>(type: "int", nullable: false),
                    MinutosIntervaloSlot = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_DisponibilidadSalas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DisponibilidadSalas_Salas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalasCaracteristicas",
                columns: table => new
                {
                    SalaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaracteristicaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalasCaracteristicas", x => new { x.SalaId, x.CaracteristicaId });
                    table.ForeignKey(
                        name: "FK_SalasCaracteristicas_Caracteristicas_CaracteristicaId",
                        column: x => x.CaracteristicaId,
                        principalTable: "Caracteristicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalasCaracteristicas_Salas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    EnviadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeidoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Modulo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdEntidadRelacionada = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TipoEntidadRelacionada = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MensajeError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IntentosReintento = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreEntidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IdEntidad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ValoresAnteriores = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValoresNuevos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DireccionIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AgenteUsuario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Modulo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Exitoso = table.Column<bool>(type: "bit", nullable: false),
                    MensajeError = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaHoraRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosAuditoria_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SalaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaHoraInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaHoraFin = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CantidadPersonas = table.Column<int>(type: "int", nullable: false),
                    Proposito = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    ComentarioAdmin = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AprobadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AprobadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RechazadoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RechazadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CanceladoPorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CanceladoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoCancelacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EsReservaDirectaAdmin = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_Salas_SalaId",
                        column: x => x.SalaId,
                        principalTable: "Salas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TokensRenovacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    VenceEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstaRevocado = table.Column<bool>(type: "bit", nullable: false),
                    RevocadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoRevocacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReemplazadoPorToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DireccionIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AgenteUsuario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_TokensRenovacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokensRenovacion_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosRoles",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AsignadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AsignadoPor = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosRoles", x => new { x.UsuarioId, x.RolId });
                    table.ForeignKey(
                        name: "FK_UsuariosRoles_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Caracteristicas",
                columns: new[] { "Id", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "NombreIcono", "EsActivo", "EliminadoLogico", "Nombre", "ActualizadoEn", "ActualizadoPor" },
                values: new object[,]
                {
                    { new Guid("f0000001-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "projector", true, false, "Proyector", null, null },
                    { new Guid("f0000001-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "monitor", true, false, "Pantalla", null, null },
                    { new Guid("f0000001-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "square", true, false, "Pizarra", null, null },
                    { new Guid("f0000001-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "wind", true, false, "Aire acondicionado", null, null },
                    { new Guid("f0000001-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "wifi", true, false, "Internet", null, null },
                    { new Guid("f0000001-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "video", true, false, "Videoconferencia", null, null },
                    { new Guid("f0000001-0000-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "tv", true, false, "TV", null, null },
                    { new Guid("f0000001-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "volume-2", true, false, "Sistema de audio", null, null },
                    { new Guid("f0000001-0000-0000-0000-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "layout", true, false, "Mesa de reuniones", null, null },
                    { new Guid("f0000001-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "zap", true, false, "Conexiones eléctricas", null, null }
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionSistema",
                columns: new[] { "Id", "CreadoEn", "CreadoPor", "TipoDato", "ValorPredeterminado", "EliminadoEn", "EliminadoPor", "Descripcion", "EliminadoLogico", "EstaEncriptado", "EsDeSoloLectura", "Clave", "Modulo", "ActualizadoEn", "ActualizadoPor", "Valor" },
                values: new object[,]
                {
                    { new Guid("b0000001-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "string", "RepagroSuite", null, null, "Nombre de la aplicación", false, false, false, "APP.NAME", "GENERAL", null, null, "RepagroSuite" },
                    { new Guid("b0000001-0000-0000-0000-000000000002"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "string", "UTC", null, null, "Zona horaria del sistema", false, false, false, "APP.TIMEZONE", "GENERAL", null, null, "America/Costa_Rica" },
                    { new Guid("b0000001-0000-0000-0000-000000000003"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "int", "5", null, null, "Máximo intentos fallidos antes de bloqueo", false, false, false, "AUTH.MAX_FAILED_ATTEMPTS", "AUTH", null, null, "5" },
                    { new Guid("b0000001-0000-0000-0000-000000000004"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "int", "15", null, null, "Minutos de bloqueo por intentos fallidos", false, false, false, "AUTH.LOCKOUT_MINUTES", "AUTH", null, null, "15" },
                    { new Guid("b0000001-0000-0000-0000-000000000005"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "int", "24", null, null, "Horas de validez del token de recuperación", false, false, false, "AUTH.PASSWORD_RESET_TOKEN_HOURS", "AUTH", null, null, "24" },
                    { new Guid("b0000001-0000-0000-0000-000000000006"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "int", "72", null, null, "Horas de validez de contraseña temporal", false, false, false, "AUTH.TEMP_PASSWORD_HOURS", "AUTH", null, null, "72" },
                    { new Guid("b0000001-0000-0000-0000-000000000007"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "string", "GoMeta", null, null, "Proveedor de consulta de cédulas", false, false, false, "IDENTIFICATION.PROVIDER", "IDENTIFICATION", null, null, "GoMeta" },
                    { new Guid("b0000001-0000-0000-0000-000000000008"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "string", "https://apis.gometa.org/cedulas/", null, null, "URL base del proveedor de cédulas", false, false, false, "IDENTIFICATION.BASE_URL", "IDENTIFICATION", null, null, "https://apis.gometa.org/cedulas/" },
                    { new Guid("b0000001-0000-0000-0000-000000000009"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "bool", "true", null, null, "Activar caché de cédulas", false, false, false, "IDENTIFICATION.CACHE_ENABLED", "IDENTIFICATION", null, null, "true" },
                    { new Guid("b0000001-0000-0000-0000-000000000010"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "int", "30", null, null, "Días de validez del caché de cédulas", false, false, false, "IDENTIFICATION.CACHE_DAYS", "IDENTIFICATION", null, null, "30" },
                    { new Guid("b0000001-0000-0000-0000-000000000011"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "bool", "false", null, null, "Activar envío de correos", false, false, false, "EMAIL.ENABLED", "EMAIL", null, null, "false" },
                    { new Guid("b0000001-0000-0000-0000-000000000012"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "string", "RepagroSuite", null, null, "Nombre del remitente", false, false, false, "EMAIL.FROM_NAME", "EMAIL", null, null, "RepagroSuite" },
                    { new Guid("b0000001-0000-0000-0000-000000000013"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "string", "", null, null, "Correo del remitente", false, false, false, "EMAIL.FROM_ADDRESS", "EMAIL", null, null, "" },
                    { new Guid("b0000001-0000-0000-0000-000000000014"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "string", "", null, null, "Servidor SMTP", false, false, false, "EMAIL.SMTP_HOST", "EMAIL", null, null, "" },
                    { new Guid("b0000001-0000-0000-0000-000000000015"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "int", "587", null, null, "Puerto SMTP", false, false, false, "EMAIL.SMTP_PORT", "EMAIL", null, null, "587" },
                    { new Guid("b0000001-0000-0000-0000-000000000016"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "bool", "true", null, null, "Usar SSL para SMTP", false, false, false, "EMAIL.SMTP_USE_SSL", "EMAIL", null, null, "true" }
                });

            migrationBuilder.InsertData(
                table: "ModulosSistema",
                columns: new[] { "Id", "Codigo", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "Descripcion", "NombreIcono", "EsActivo", "EsNuclear", "EliminadoLogico", "Nombre", "PrefijoRuta", "OrdenVisualizacion", "ActualizadoEn", "ActualizadoPor", "Version" },
                values: new object[] { new Guid("c0000001-0000-0000-0000-000000000001"), "ROOMS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Reservas y gestión de salas empresariales", "door-open", true, true, false, "Gestión de Salas", "/rooms", 1, null, null, "1.0.0" });

            migrationBuilder.InsertData(
                table: "Permisos",
                columns: new[] { "Id", "Codigo", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "Descripcion", "EsActivo", "EliminadoLogico", "Modulo", "Nombre", "ActualizadoEn", "ActualizadoPor" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000001"), "Users.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Users", "Ver usuarios", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000002"), "Users.Approve", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Users", "Aprobar usuarios", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000003"), "Users.Reject", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Users", "Rechazar usuarios", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000004"), "Users.Block", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Users", "Bloquear usuarios", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000005"), "Users.GenerateTemporaryPassword", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Users", "Generar contraseña temporal", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000006"), "Users.ForcePasswordChange", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Users", "Forzar cambio de contraseña", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000007"), "Identifications.Lookup", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Identifications", "Consultar identificaciones", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000008"), "Rooms.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Rooms", "Ver salas", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000009"), "Rooms.Create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Rooms", "Crear salas", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000010"), "Rooms.Update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Rooms", "Editar salas", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000011"), "Rooms.Delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Rooms", "Eliminar salas", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000012"), "Rooms.Availability.Manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Rooms", "Gestionar disponibilidad", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000013"), "Reservations.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Reservations", "Ver reservas", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000014"), "Reservations.Create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Reservations", "Crear reservas", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000015"), "Reservations.Approve", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Reservations", "Aprobar reservas", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000016"), "Reservations.Reject", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Reservations", "Rechazar reservas", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000017"), "Reservations.Cancel", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Reservations", "Cancelar reservas", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000018"), "Reservations.DirectCreate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Reservations", "Reserva directa administrativa", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000019"), "AuditLogs.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "AuditLogs", "Ver auditoría", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000020"), "Reports.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Reports", "Ver reportes", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000021"), "Settings.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Settings", "Ver configuración", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000022"), "Settings.Update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Settings", "Editar configuración", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000023"), "Settings.Email.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Settings", "Ver config. correo", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000024"), "Settings.Email.Update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Settings", "Editar config. correo", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000025"), "Settings.Email.Test", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Settings", "Probar config. correo", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000026"), "Settings.Modules.View", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Settings", "Ver módulos", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000027"), "Settings.Modules.Create", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Settings", "Crear módulos", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000028"), "Settings.Modules.Update", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Settings", "Editar módulos", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000029"), "Settings.Modules.Delete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Settings", "Eliminar módulos", null, null },
                    { new Guid("a0000001-0000-0000-0000-000000000030"), "Settings.Security.Manage", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, true, false, "Settings", "Gestionar seguridad", null, null }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "Descripcion", "EsActivo", "EliminadoLogico", "EsRolDelSistema", "Nombre", "NombreNormalizado", "ActualizadoEn", "ActualizadoPor" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Administrador del sistema", true, false, true, "Administrator", "ADMINISTRATOR", null, null },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Usuario estándar", true, false, true, "User", "USER", null, null }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "AprobadoEn", "AprobadoPorId", "CreadoEn", "CreadoPor", "EliminadoEn", "EliminadoPor", "Departamento", "CorreoElectronico", "IntentosLoginFallidos", "NombresPropios", "PrimerNombre", "SegundoNombre", "NombreCompleto", "NumeroIdentificacion", "TipoIdentificacion", "IdentificacionValidada", "IdentificacionValidadaEn", "FuenteValidacion", "EliminadoLogico", "UltimoAccesoEn", "Apellidos", "PrimerApellido", "SegundoApellido", "UltimoCambioContrasenaEn", "NombreLegal", "BloqueoHasta", "DebeActualizarContrasena", "CorreoElectronicoNorm", "NumeroIdentificacionNorm", "HashContrasena", "TokenRestablecimiento", "VencimientoTokenRestablecimiento", "Telefono", "Puesto", "UrlImagenPerfil", "RechazadoEn", "RechazadoPorId", "MotivoRechazo", "Estado", "VencimientoContrasenaTemp", "ActualizadoEn", "ActualizadoPor" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, null, "admin@repagro.com", 0, "ADMINISTRADOR", "ADMINISTRADOR", null, "ADMINISTRADOR DEL SISTEMA", "000000000", 1, false, null, "Manual", false, null, "DEL SISTEMA", "DEL", "SISTEMA", null, null, null, false, "ADMIN@REPAGRO.COM", "000000000", "$2a$11$K2CtDP7zSGOKgjXjVy9TSOc.vSlm2dn0EkK0EZ.UpIwAbvBFAFhUy", null, null, null, null, null, null, null, null, 2, null, null, null });

            migrationBuilder.InsertData(
                table: "RolesPermisos",
                columns: new[] { "PermisoId", "RolId", "AsignadoEn", "AsignadoPor" },
                values: new object[,]
                {
                    { new Guid("a0000001-0000-0000-0000-000000000001"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000002"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000003"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000004"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000005"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000006"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000007"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000008"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000009"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000010"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000011"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000012"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000013"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000014"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000015"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000016"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000017"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000018"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000019"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000020"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000021"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000022"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000023"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000024"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000025"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000026"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000027"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000028"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000029"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000030"), new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000007"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000008"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000013"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000014"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { new Guid("a0000001-0000-0000-0000-000000000017"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                table: "UsuariosRoles",
                columns: new[] { "RolId", "UsuarioId", "AsignadoEn", "AsignadoPor" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.CreateIndex(
                name: "IX_BloquesSalas_SalaId",
                table: "BloquesSalas",
                column: "SalaId");

            migrationBuilder.CreateIndex(
                name: "IX_CacheIdentificaciones_NumeroIdentificacionNorm",
                table: "CacheIdentificaciones",
                column: "NumeroIdentificacionNorm",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionSistema_Clave",
                table: "ConfiguracionSistema",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DisponibilidadSalas_SalaId_DiaSemana",
                table: "DisponibilidadSalas",
                columns: new[] { "SalaId", "DiaSemana" },
                unique: true,
                filter: "[EliminadoLogico] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ModulosSistema_Codigo",
                table: "ModulosSistema",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId_Estado",
                table: "Notificaciones",
                columns: new[] { "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Codigo",
                table: "Permisos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_FechaHoraRegistro",
                table: "RegistrosAuditoria",
                column: "FechaHoraRegistro");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_UsuarioId",
                table: "RegistrosAuditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_SalaId_FechaHoraInicio_FechaHoraFin_Estado",
                table: "Reservas",
                columns: new[] { "SalaId", "FechaHoraInicio", "FechaHoraFin", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioId",
                table: "Reservas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NombreNormalizado",
                table: "Roles",
                column: "NombreNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolesPermisos_PermisoId",
                table: "RolesPermisos",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_Salas_Codigo",
                table: "Salas",
                column: "Codigo",
                unique: true,
                filter: "[EliminadoLogico] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SalasCaracteristicas_CaracteristicaId",
                table: "SalasCaracteristicas",
                column: "CaracteristicaId");

            migrationBuilder.CreateIndex(
                name: "IX_TokensRenovacion_Token",
                table: "TokensRenovacion",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TokensRenovacion_UsuarioId",
                table: "TokensRenovacion",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_CorreoElectronicoNorm",
                table: "Usuarios",
                column: "CorreoElectronicoNorm",
                unique: true,
                filter: "[EliminadoLogico] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NumeroIdentificacionNorm",
                table: "Usuarios",
                column: "NumeroIdentificacionNorm",
                unique: true,
                filter: "[EliminadoLogico] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosRoles_RolId",
                table: "UsuariosRoles",
                column: "RolId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BloquesSalas");

            migrationBuilder.DropTable(
                name: "CacheIdentificaciones");

            migrationBuilder.DropTable(
                name: "ConfiguracionSistema");

            migrationBuilder.DropTable(
                name: "DisponibilidadSalas");

            migrationBuilder.DropTable(
                name: "ModulosSistema");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "RolesPermisos");

            migrationBuilder.DropTable(
                name: "SalasCaracteristicas");

            migrationBuilder.DropTable(
                name: "TokensRenovacion");

            migrationBuilder.DropTable(
                name: "UsuariosRoles");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Caracteristicas");

            migrationBuilder.DropTable(
                name: "Salas");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
