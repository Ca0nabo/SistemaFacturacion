namespace SistemaFacturacion.Models;
public class AsientoContable
{
    public int IdAsiento { get; set; }
    public int? IdFacturaReferencia { get; set; }
    public int IdCuentaContable { get; set; }
    public decimal MontoDebito { get; set; }
    public decimal MontoCredito { get; set; }
    public string Descripcion { get; set; } = null!;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public FacturaCabecera? FacturaReferencia { get; set; }
    public CatalogoCuentas CuentaContable { get; set; } = null!;
}
