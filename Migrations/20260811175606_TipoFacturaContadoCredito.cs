using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaFacturacion.Migrations
{
    /// <inheritdoc />
    public partial class TipoFacturaContadoCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FacturasCabecera_IdContrato_PeriodoFacturado_TipoFactura",
                table: "FacturasCabecera");

            migrationBuilder.AlterColumn<string>(
                name: "TipoFactura",
                table: "FacturasCabecera",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.AddColumn<string>(
                name: "OrigenFactura",
                table: "FacturasCabecera",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCabecera_IdContrato_PeriodoFacturado_OrigenFactura",
                table: "FacturasCabecera",
                columns: new[] { "IdContrato", "PeriodoFacturado", "OrigenFactura" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FacturasCabecera_IdContrato_PeriodoFacturado_OrigenFactura",
                table: "FacturasCabecera");

            migrationBuilder.DropColumn(
                name: "OrigenFactura",
                table: "FacturasCabecera");

            migrationBuilder.AlterColumn<string>(
                name: "TipoFactura",
                table: "FacturasCabecera",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15);

            migrationBuilder.CreateIndex(
                name: "IX_FacturasCabecera_IdContrato_PeriodoFacturado_TipoFactura",
                table: "FacturasCabecera",
                columns: new[] { "IdContrato", "PeriodoFacturado", "TipoFactura" });
        }
    }
}
