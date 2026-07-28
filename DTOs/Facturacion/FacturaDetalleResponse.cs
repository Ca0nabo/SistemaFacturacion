namespace SistemaFacturacion.DTOs.Facturacion;
public class FacturaDetalleResponse
{
    public string DescripcionItem { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal Precio { get; set; }
    public decimal Subtotal { get; set; }
}
