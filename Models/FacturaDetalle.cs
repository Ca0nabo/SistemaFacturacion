namespace SistemaFacturacion.Models;
public class FacturaDetalle
{
    public int IdDetalle { get; set; }
    public int IdFactura { get; set; }
    public string DescripcionItem { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal Precio { get; set; }
    public FacturaCabecera Factura { get; set; } = null!;
}
