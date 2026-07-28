using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaFacturacion.Migrations
{
    /// <inheritdoc />
    public partial class InmobiliariaExtension : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IdPropiedad",
                table: "MovimientosCx",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdUnidad",
                table: "MovimientosCx",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdPropiedad",
                table: "FacturasCabecera",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdUnidad",
                table: "FacturasCabecera",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Condiciones",
                table: "Contratos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<decimal>(
                name: "Deposito",
                table: "Contratos",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiaPago",
                table: "Contratos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "IdPropiedad",
                table: "Contratos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdUnidad",
                table: "Contratos",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoMantenimiento",
                table: "Contratos",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoContrato",
                table: "Contratos",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Propiedades",
                columns: table => new
                {
                    IdPropiedad = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IdEntidad = table.Column<int>(type: "integer", nullable: false),
                    TipoPropiedad = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Sector = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Ciudad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    MetrosCuadrados = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CantidadHabitaciones = table.Column<int>(type: "integer", nullable: true),
                    CantidadBanos = table.Column<int>(type: "integer", nullable: true),
                    TieneParqueo = table.Column<bool>(type: "boolean", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCx_IdPropiedad",
                table: "MovimientosCx",
                column: "IdPropiedad");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCx_IdUnidad",
                table: "MovimientosCx",
                column: "IdUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCabecera_IdPropiedad",
                table: "FacturasCabecera",
                column: "IdPropiedad");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCabecera_IdUnidad",
                table: "FacturasCabecera",
                column: "IdUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_IdPropiedad",
                table: "Contratos",
                column: "IdPropiedad");

            migrationBuilder.CreateIndex(
                name: "IX_Contratos_IdUnidad",
                table: "Contratos",
                column: "IdUnidad");

            migrationBuilder.CreateIndex(
                name: "IX_Propiedades_IdEntidad",
                table: "Propiedades",
                column: "IdEntidad");

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_IdPropiedad_Codigo",
                table: "Unidades",
                columns: new[] { "IdPropiedad", "Codigo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Propiedades_IdPropiedad",
                table: "Contratos",
                column: "IdPropiedad",
                principalTable: "Propiedades",
                principalColumn: "IdPropiedad",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Contratos_Unidades_IdUnidad",
                table: "Contratos",
                column: "IdUnidad",
                principalTable: "Unidades",
                principalColumn: "IdUnidad",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturasCabecera_Propiedades_IdPropiedad",
                table: "FacturasCabecera",
                column: "IdPropiedad",
                principalTable: "Propiedades",
                principalColumn: "IdPropiedad",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FacturasCabecera_Unidades_IdUnidad",
                table: "FacturasCabecera",
                column: "IdUnidad",
                principalTable: "Unidades",
                principalColumn: "IdUnidad",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCx_Propiedades_IdPropiedad",
                table: "MovimientosCx",
                column: "IdPropiedad",
                principalTable: "Propiedades",
                principalColumn: "IdPropiedad",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosCx_Unidades_IdUnidad",
                table: "MovimientosCx",
                column: "IdUnidad",
                principalTable: "Unidades",
                principalColumn: "IdUnidad",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Propiedades_IdPropiedad",
                table: "Contratos");

            migrationBuilder.DropForeignKey(
                name: "FK_Contratos_Unidades_IdUnidad",
                table: "Contratos");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturasCabecera_Propiedades_IdPropiedad",
                table: "FacturasCabecera");

            migrationBuilder.DropForeignKey(
                name: "FK_FacturasCabecera_Unidades_IdUnidad",
                table: "FacturasCabecera");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCx_Propiedades_IdPropiedad",
                table: "MovimientosCx");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosCx_Unidades_IdUnidad",
                table: "MovimientosCx");

            migrationBuilder.DropTable(
                name: "Unidades");

            migrationBuilder.DropTable(
                name: "Propiedades");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCx_IdPropiedad",
                table: "MovimientosCx");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCx_IdUnidad",
                table: "MovimientosCx");

            migrationBuilder.DropIndex(
                name: "IX_FacturasCabecera_IdPropiedad",
                table: "FacturasCabecera");

            migrationBuilder.DropIndex(
                name: "IX_FacturasCabecera_IdUnidad",
                table: "FacturasCabecera");

            migrationBuilder.DropIndex(
                name: "IX_Contratos_IdPropiedad",
                table: "Contratos");

            migrationBuilder.DropIndex(
                name: "IX_Contratos_IdUnidad",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "IdPropiedad",
                table: "MovimientosCx");

            migrationBuilder.DropColumn(
                name: "IdUnidad",
                table: "MovimientosCx");

            migrationBuilder.DropColumn(
                name: "IdPropiedad",
                table: "FacturasCabecera");

            migrationBuilder.DropColumn(
                name: "IdUnidad",
                table: "FacturasCabecera");

            migrationBuilder.DropColumn(
                name: "Deposito",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "DiaPago",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "IdPropiedad",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "IdUnidad",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "MontoMantenimiento",
                table: "Contratos");

            migrationBuilder.DropColumn(
                name: "TipoContrato",
                table: "Contratos");

            migrationBuilder.AlterColumn<string>(
                name: "Condiciones",
                table: "Contratos",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);
        }
    }
}
