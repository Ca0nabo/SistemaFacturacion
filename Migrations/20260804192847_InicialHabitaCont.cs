using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SistemaFacturacion.Migrations
{
    /// <inheritdoc />
    public partial class InicialHabitaCont : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogoCuentas",
                columns: table => new
                {
                    IdCuentaContable = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreCuenta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogoCuentas", x => x.IdCuentaContable);
                });

            migrationBuilder.CreateTable(
                name: "Entidades",
                columns: table => new
                {
                    IdEntidad = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RncCedula = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RazonSocial = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entidades", x => x.IdEntidad);
                });

            migrationBuilder.CreateTable(
                name: "ParametrosEmpresa",
                columns: table => new
                {
                    IdParametro = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NombreEmpresa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SecuenciaEmpresa = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SecuenciaFiscalECF = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PorcentajeITBIS = table.Column<decimal>(type: "numeric(5,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosEmpresa", x => x.IdParametro);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    IdRol = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Permisos = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.IdRol);
                });

            migrationBuilder.CreateTable(
                name: "Propiedades",
                columns: table => new
                {
                    IdPropiedad = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdEntidad = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    TipoPropiedad = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MetrosCuadrados = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CantidadHabitaciones = table.Column<int>(type: "integer", nullable: true),
                    CantidadBanos = table.Column<int>(type: "integer", nullable: true),
                    TieneParqueo = table.Column<bool>(type: "boolean", nullable: false),
                    CanonMensualSugerido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MantenimientoMensualSugerido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Propiedades", x => x.IdPropiedad);
                    table.ForeignKey(
                        name: "FK_Propiedades_Entidades_IdEntidad",
                        column: x => x.IdEntidad,
                        principalTable: "Entidades",
                        principalColumn: "IdEntidad",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NombreCompleto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdRol = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                    table.ForeignKey(
                        name: "FK_Usuarios_Roles_IdRol",
                        column: x => x.IdRol,
                        principalTable: "Roles",
                        principalColumn: "IdRol",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Unidades",
                columns: table => new
                {
                    IdUnidad = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdPropiedad = table.Column<int>(type: "integer", nullable: false),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Piso = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MetrosCuadrados = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    Estado = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unidades", x => x.IdUnidad);
                    table.ForeignKey(
                        name: "FK_Unidades_Propiedades_IdPropiedad",
                        column: x => x.IdPropiedad,
                        principalTable: "Propiedades",
                        principalColumn: "IdPropiedad",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditoriaLogs",
                columns: table => new
                {
                    IdLog = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdUsuario = table.Column<int>(type: "integer", nullable: false),
                    Accion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Modulo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IdRegistro = table.Column<int>(type: "integer", nullable: true),
                    Detalle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditoriaLogs", x => x.IdLog);
                    table.ForeignKey(
                        name: "FK_AuditoriaLogs_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contratos",
                columns: table => new
                {
                    IdContrato = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdEntidad = table.Column<int>(type: "integer", nullable: false),
                    IdPropiedad = table.Column<int>(type: "integer", nullable: true),
                    IdUnidad = table.Column<int>(type: "integer", nullable: true),
                    TipoContrato = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Condiciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoMantenimiento = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Deposito = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DiaPago = table.Column<int>(type: "integer", nullable: false),
                    AplicaITBIS = table.Column<bool>(type: "boolean", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contratos", x => x.IdContrato);
                    table.ForeignKey(
                        name: "FK_Contratos_Entidades_IdEntidad",
                        column: x => x.IdEntidad,
                        principalTable: "Entidades",
                        principalColumn: "IdEntidad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Contratos_Propiedades_IdPropiedad",
                        column: x => x.IdPropiedad,
                        principalTable: "Propiedades",
                        principalColumn: "IdPropiedad",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Contratos_Unidades_IdUnidad",
                        column: x => x.IdUnidad,
                        principalTable: "Unidades",
                        principalColumn: "IdUnidad",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DepositosGarantia",
                columns: table => new
                {
                    IdDeposito = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdContrato = table.Column<int>(type: "integer", nullable: false),
                    MontoRequerido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoRecibido = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaRecepcion = table.Column<DateOnly>(type: "date", nullable: true),
                    FechaDevolucion = table.Column<DateOnly>(type: "date", nullable: true),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MetodoPago = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DepositosGarantia", x => x.IdDeposito);
                    table.ForeignKey(
                        name: "FK_DepositosGarantia_Contratos_IdContrato",
                        column: x => x.IdContrato,
                        principalTable: "Contratos",
                        principalColumn: "IdContrato",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FacturasCabecera",
                columns: table => new
                {
                    IdFactura = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdEntidad = table.Column<int>(type: "integer", nullable: false),
                    IdContrato = table.Column<int>(type: "integer", nullable: true),
                    IdPropiedad = table.Column<int>(type: "integer", nullable: true),
                    IdUnidad = table.Column<int>(type: "integer", nullable: true),
                    NumeroECF = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    TipoFactura = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PeriodoFacturado = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    AplicaITBIS = table.Column<bool>(type: "boolean", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Itbis = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FirmaDGII = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasCabecera", x => x.IdFactura);
                    table.ForeignKey(
                        name: "FK_FacturasCabecera_Contratos_IdContrato",
                        column: x => x.IdContrato,
                        principalTable: "Contratos",
                        principalColumn: "IdContrato",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FacturasCabecera_Entidades_IdEntidad",
                        column: x => x.IdEntidad,
                        principalTable: "Entidades",
                        principalColumn: "IdEntidad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FacturasCabecera_Propiedades_IdPropiedad",
                        column: x => x.IdPropiedad,
                        principalTable: "Propiedades",
                        principalColumn: "IdPropiedad",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FacturasCabecera_Unidades_IdUnidad",
                        column: x => x.IdUnidad,
                        principalTable: "Unidades",
                        principalColumn: "IdUnidad",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "AcuerdosPago",
                columns: table => new
                {
                    IdAcuerdo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdContrato = table.Column<int>(type: "integer", nullable: false),
                    IdEntidad = table.Column<int>(type: "integer", nullable: false),
                    IdPropiedad = table.Column<int>(type: "integer", nullable: false),
                    IdFacturaOrigen = table.Column<int>(type: "integer", nullable: true),
                    MontoOriginal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoAcordado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CantidadCuotas = table.Column<int>(type: "integer", nullable: false),
                    MontoCuota = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    DiaPago = table.Column<int>(type: "integer", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Observaciones = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcuerdosPago", x => x.IdAcuerdo);
                    table.ForeignKey(
                        name: "FK_AcuerdosPago_Contratos_IdContrato",
                        column: x => x.IdContrato,
                        principalTable: "Contratos",
                        principalColumn: "IdContrato",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcuerdosPago_Entidades_IdEntidad",
                        column: x => x.IdEntidad,
                        principalTable: "Entidades",
                        principalColumn: "IdEntidad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AcuerdosPago_FacturasCabecera_IdFacturaOrigen",
                        column: x => x.IdFacturaOrigen,
                        principalTable: "FacturasCabecera",
                        principalColumn: "IdFactura",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AcuerdosPago_Propiedades_IdPropiedad",
                        column: x => x.IdPropiedad,
                        principalTable: "Propiedades",
                        principalColumn: "IdPropiedad",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AsientosContables",
                columns: table => new
                {
                    IdAsiento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdFacturaReferencia = table.Column<int>(type: "integer", nullable: true),
                    IdCuentaContable = table.Column<int>(type: "integer", nullable: false),
                    MontoDebito = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoCredito = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Descripcion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsientosContables", x => x.IdAsiento);
                    table.ForeignKey(
                        name: "FK_AsientosContables_CatalogoCuentas_IdCuentaContable",
                        column: x => x.IdCuentaContable,
                        principalTable: "CatalogoCuentas",
                        principalColumn: "IdCuentaContable",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsientosContables_FacturasCabecera_IdFacturaReferencia",
                        column: x => x.IdFacturaReferencia,
                        principalTable: "FacturasCabecera",
                        principalColumn: "IdFactura",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FacturasDetalle",
                columns: table => new
                {
                    IdDetalle = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdFactura = table.Column<int>(type: "integer", nullable: false),
                    DescripcionItem = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Cantidad = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Precio = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasDetalle", x => x.IdDetalle);
                    table.ForeignKey(
                        name: "FK_FacturasDetalle_FacturasCabecera_IdFactura",
                        column: x => x.IdFactura,
                        principalTable: "FacturasCabecera",
                        principalColumn: "IdFactura",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCx",
                columns: table => new
                {
                    IdMovimiento = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdFactura = table.Column<int>(type: "integer", nullable: false),
                    IdPropiedad = table.Column<int>(type: "integer", nullable: true),
                    IdUnidad = table.Column<int>(type: "integer", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    MontoOriginal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoPendiente = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    NumeroCuota = table.Column<int>(type: "integer", nullable: true),
                    TotalCuotas = table.Column<int>(type: "integer", nullable: true),
                    CategoriaGasto = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ArchivoEvidencia = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCx", x => x.IdMovimiento);
                    table.ForeignKey(
                        name: "FK_MovimientosCx_FacturasCabecera_IdFactura",
                        column: x => x.IdFactura,
                        principalTable: "FacturasCabecera",
                        principalColumn: "IdFactura",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MovimientosCx_Propiedades_IdPropiedad",
                        column: x => x.IdPropiedad,
                        principalTable: "Propiedades",
                        principalColumn: "IdPropiedad",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimientosCx_Unidades_IdUnidad",
                        column: x => x.IdUnidad,
                        principalTable: "Unidades",
                        principalColumn: "IdUnidad",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    IdPago = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdFactura = table.Column<int>(type: "integer", nullable: false),
                    IdContrato = table.Column<int>(type: "integer", nullable: true),
                    IdEntidad = table.Column<int>(type: "integer", nullable: false),
                    IdPropiedad = table.Column<int>(type: "integer", nullable: true),
                    IdUnidad = table.Column<int>(type: "integer", nullable: true),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    MetodoPago = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notas = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.IdPago);
                    table.ForeignKey(
                        name: "FK_Pagos_Contratos_IdContrato",
                        column: x => x.IdContrato,
                        principalTable: "Contratos",
                        principalColumn: "IdContrato",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pagos_Entidades_IdEntidad",
                        column: x => x.IdEntidad,
                        principalTable: "Entidades",
                        principalColumn: "IdEntidad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_FacturasCabecera_IdFactura",
                        column: x => x.IdFactura,
                        principalTable: "FacturasCabecera",
                        principalColumn: "IdFactura",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Pagos_Propiedades_IdPropiedad",
                        column: x => x.IdPropiedad,
                        principalTable: "Propiedades",
                        principalColumn: "IdPropiedad",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Pagos_Unidades_IdUnidad",
                        column: x => x.IdUnidad,
                        principalTable: "Unidades",
                        principalColumn: "IdUnidad",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CuotasAcuerdoPago",
                columns: table => new
                {
                    IdCuotaAcuerdo = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdAcuerdo = table.Column<int>(type: "integer", nullable: false),
                    NumeroCuota = table.Column<int>(type: "integer", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MontoPagado = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuotasAcuerdoPago", x => x.IdCuotaAcuerdo);
                    table.ForeignKey(
                        name: "FK_CuotasAcuerdoPago_AcuerdosPago_IdAcuerdo",
                        column: x => x.IdAcuerdo,
                        principalTable: "AcuerdosPago",
                        principalColumn: "IdAcuerdo",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MovimientosCuenta",
                columns: table => new
                {
                    IdMovimientoCuenta = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdEntidad = table.Column<int>(type: "integer", nullable: false),
                    IdPropiedad = table.Column<int>(type: "integer", nullable: true),
                    IdUnidad = table.Column<int>(type: "integer", nullable: true),
                    IdContrato = table.Column<int>(type: "integer", nullable: true),
                    IdFactura = table.Column<int>(type: "integer", nullable: true),
                    IdPago = table.Column<int>(type: "integer", nullable: true),
                    Fecha = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    TipoMovimiento = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Concepto = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Referencia = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Debito = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Credito = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MovimientosCuenta", x => x.IdMovimientoCuenta);
                    table.ForeignKey(
                        name: "FK_MovimientosCuenta_Contratos_IdContrato",
                        column: x => x.IdContrato,
                        principalTable: "Contratos",
                        principalColumn: "IdContrato",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimientosCuenta_Entidades_IdEntidad",
                        column: x => x.IdEntidad,
                        principalTable: "Entidades",
                        principalColumn: "IdEntidad",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MovimientosCuenta_FacturasCabecera_IdFactura",
                        column: x => x.IdFactura,
                        principalTable: "FacturasCabecera",
                        principalColumn: "IdFactura",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimientosCuenta_Pagos_IdPago",
                        column: x => x.IdPago,
                        principalTable: "Pagos",
                        principalColumn: "IdPago",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimientosCuenta_Propiedades_IdPropiedad",
                        column: x => x.IdPropiedad,
                        principalTable: "Propiedades",
                        principalColumn: "IdPropiedad",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MovimientosCuenta_Unidades_IdUnidad",
                        column: x => x.IdUnidad,
                        principalTable: "Unidades",
                        principalColumn: "IdUnidad",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                table: "CatalogoCuentas",
                columns: new[] { "IdCuentaContable", "NombreCuenta" },
                values: new object[,]
                {
                    { 1, "Caja" },
                    { 2, "Banco" },
                    { 3, "Cuentas por Cobrar" },
                    { 4, "Cuentas por Pagar" },
                    { 5, "Ingresos por Alquiler" },
                    { 6, "ITBIS por Pagar" }
                });

            migrationBuilder.InsertData(
                table: "ParametrosEmpresa",
                columns: new[] { "IdParametro", "NombreEmpresa", "PorcentajeITBIS", "SecuenciaEmpresa", "SecuenciaFiscalECF" },
                values: new object[] { 1, "HabitaCont SRL", 0.18m, "001", "FAC" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "IdRol", "Nombre", "Permisos" },
                values: new object[,]
                {
                    { 1, "Administrador", "TODO" },
                    { 2, "Contador", "FACTURAS,REPORTES,MOVIMIENTOS,PAGOS,DEPOSITOS,ACUERDOS" },
                    { 3, "Encargado de facturación", "FACTURAS,ENTIDADES,CONTRATOS" },
                    { 4, "Gerente financiero", "REPORTES,CONTRATOS,MOVIMIENTOS,DEPOSITOS,ACUERDOS" },
                    { 5, "Cliente", "CONSULTA" },
                    { 6, "Proveedor", "CONSULTA" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "IdUsuario", "Activo", "Email", "FechaCreacion", "IdRol", "NombreCompleto", "PasswordHash" },
                values: new object[] { 1, true, "admin@sistema.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Administrador del Sistema", "$2a$11$4bEImu8PNaPhgHS2iQM8YOg1gubP0uFTqDOxh8QgedWuMvs1WYMma" });

            migrationBuilder.CreateIndex(
                name: "IX_AcuerdosPago_IdContrato",
                table: "AcuerdosPago",
                column: "IdContrato");

            migrationBuilder.CreateIndex(
                name: "IX_AcuerdosPago_IdEntidad",
                table: "AcuerdosPago",
                column: "IdEntidad");

            migrationBuilder.CreateIndex(
                name: "IX_AcuerdosPago_IdFacturaOrigen",
                table: "AcuerdosPago",
                column: "IdFacturaOrigen");

            migrationBuilder.CreateIndex(
                name: "IX_AcuerdosPago_IdPropiedad",
                table: "AcuerdosPago",
                column: "IdPropiedad");

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_IdCuentaContable",
                table: "AsientosContables",
                column: "IdCuentaContable");

            migrationBuilder.CreateIndex(
                name: "IX_AsientosContables_IdFacturaReferencia",
                table: "AsientosContables",
                column: "IdFacturaReferencia");

            migrationBuilder.CreateIndex(
                name: "IX_AuditoriaLogs_IdUsuario",
                table: "AuditoriaLogs",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_IdEntidad",
                table: "Contratos",
                column: "IdEntidad");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_IdPropiedad_IdUnidad_Estado",
                table: "Contratos",
                columns: new[] { "IdPropiedad", "IdUnidad", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_IdUnidad",
                table: "Contratos",
                column: "IdUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_CuotasAcuerdoPago_IdAcuerdo_NumeroCuota",
                table: "CuotasAcuerdoPago",
                columns: new[] { "IdAcuerdo", "NumeroCuota" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DepositosGarantia_IdContrato",
                table: "DepositosGarantia",
                column: "IdContrato");

            migrationBuilder.CreateIndex(
                name: "IX_Entidades_RncCedula",
                table: "Entidades",
                column: "RncCedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCabecera_IdContrato_PeriodoFacturado_TipoFactura",
                table: "FacturasCabecera",
                columns: new[] { "IdContrato", "PeriodoFacturado", "TipoFactura" });

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCabecera_IdEntidad",
                table: "FacturasCabecera",
                column: "IdEntidad");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCabecera_IdPropiedad",
                table: "FacturasCabecera",
                column: "IdPropiedad");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCabecera_IdUnidad",
                table: "FacturasCabecera",
                column: "IdUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCabecera_NumeroECF",
                table: "FacturasCabecera",
                column: "NumeroECF",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacturasDetalle_IdFactura",
                table: "FacturasDetalle",
                column: "IdFactura");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuenta_IdContrato",
                table: "MovimientosCuenta",
                column: "IdContrato");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuenta_IdEntidad_IdPropiedad_Fecha",
                table: "MovimientosCuenta",
                columns: new[] { "IdEntidad", "IdPropiedad", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuenta_IdFactura",
                table: "MovimientosCuenta",
                column: "IdFactura");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuenta_IdPago",
                table: "MovimientosCuenta",
                column: "IdPago");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuenta_IdPropiedad",
                table: "MovimientosCuenta",
                column: "IdPropiedad");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCuenta_IdUnidad",
                table: "MovimientosCuenta",
                column: "IdUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCx_IdFactura",
                table: "MovimientosCx",
                column: "IdFactura");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCx_IdPropiedad",
                table: "MovimientosCx",
                column: "IdPropiedad");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCx_IdUnidad",
                table: "MovimientosCx",
                column: "IdUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdContrato",
                table: "Pagos",
                column: "IdContrato");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdEntidad",
                table: "Pagos",
                column: "IdEntidad");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdFactura",
                table: "Pagos",
                column: "IdFactura");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdPropiedad",
                table: "Pagos",
                column: "IdPropiedad");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_IdUnidad",
                table: "Pagos",
                column: "IdUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_Propiedades_Codigo",
                table: "Propiedades",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Propiedades_IdEntidad",
                table: "Propiedades",
                column: "IdEntidad");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nombre",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_IdPropiedad_Codigo",
                table: "Unidades",
                columns: new[] { "IdPropiedad", "Codigo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdRol",
                table: "Usuarios",
                column: "IdRol");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AsientosContables");

            migrationBuilder.DropTable(
                name: "AuditoriaLogs");

            migrationBuilder.DropTable(
                name: "CuotasAcuerdoPago");

            migrationBuilder.DropTable(
                name: "DepositosGarantia");

            migrationBuilder.DropTable(
                name: "FacturasDetalle");

            migrationBuilder.DropTable(
                name: "MovimientosCuenta");

            migrationBuilder.DropTable(
                name: "MovimientosCx");

            migrationBuilder.DropTable(
                name: "ParametrosEmpresa");

            migrationBuilder.DropTable(
                name: "CatalogoCuentas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "AcuerdosPago");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "FacturasCabecera");

            migrationBuilder.DropTable(
                name: "Contratos");

            migrationBuilder.DropTable(
                name: "Unidades");

            migrationBuilder.DropTable(
                name: "Propiedades");

            migrationBuilder.DropTable(
                name: "Entidades");
        }
    }
}
