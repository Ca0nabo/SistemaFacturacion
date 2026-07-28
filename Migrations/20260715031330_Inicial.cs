using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SistemaFacturacion.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
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
                name: "Contratos",
                columns: table => new
                {
                    IdContrato = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdEntidad = table.Column<int>(type: "integer", nullable: false),
                    Condiciones = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "FacturasCabecera",
                columns: table => new
                {
                    IdFactura = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdEntidad = table.Column<int>(type: "integer", nullable: false),
                    NumeroECF = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
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
                        name: "FK_FacturasCabecera_Entidades_IdEntidad",
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

            migrationBuilder.InsertData(
                table: "CatalogoCuentas",
                columns: new[] { "IdCuentaContable", "NombreCuenta" },
                values: new object[,]
                {
                    { 1, "Caja" },
                    { 2, "Banco" },
                    { 3, "Cuentas por Cobrar" },
                    { 4, "Cuentas por Pagar" },
                    { 5, "Ingresos por Ventas" },
                    { 6, "ITBIS por Pagar" }
                });

            migrationBuilder.InsertData(
                table: "ParametrosEmpresa",
                columns: new[] { "IdParametro", "NombreEmpresa", "PorcentajeITBIS", "SecuenciaEmpresa", "SecuenciaFiscalECF" },
                values: new object[] { 1, "Mi Empresa SRL", 0.18m, "001", "ECF001" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "IdRol", "Nombre", "Permisos" },
                values: new object[,]
                {
                    { 1, "Administrador", "TODO" },
                    { 2, "Contador", "FACTURAS,REPORTES,MOVIMIENTOS" },
                    { 3, "Encargado de facturación", "FACTURAS,ENTIDADES" },
                    { 4, "Gerente financiero", "REPORTES,CONTRATOS,MOVIMIENTOS" },
                    { 5, "Cliente", "CONSULTA" },
                    { 6, "Proveedor", "CONSULTA" }
                });

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "IdUsuario", "Activo", "Email", "FechaCreacion", "IdRol", "NombreCompleto", "PasswordHash" },
                values: new object[] { 1, true, "admin@sistema.com", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "Administrador del Sistema", "$2a$11$4bEImu8PNaPhgHS2iQM8YOg1gubP0uFTqDOxh8QgedWuMvs1WYMma" });

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
                name: "IX_Entidades_RncCedula",
                table: "Entidades",
                column: "RncCedula",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCabecera_IdEntidad",
                table: "FacturasCabecera",
                column: "IdEntidad");

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
                name: "IX_MovimientosCx_IdFactura",
                table: "MovimientosCx",
                column: "IdFactura");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nombre",
                table: "Roles",
                column: "Nombre",
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
                name: "Contratos");

            migrationBuilder.DropTable(
                name: "FacturasDetalle");

            migrationBuilder.DropTable(
                name: "MovimientosCx");

            migrationBuilder.DropTable(
                name: "ParametrosEmpresa");

            migrationBuilder.DropTable(
                name: "CatalogoCuentas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "FacturasCabecera");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Entidades");
        }
    }
}
