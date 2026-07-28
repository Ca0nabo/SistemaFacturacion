namespace SistemaFacturacion.DTOs.Facturacion;
public class FacturaResponse
{
    public int IdFactura { get; set; }
    public string NumeroECF { get; set; } = null!;
    public string RazonSocial { get; set; } = null!;
    public string RncCedula { get; set; } = null!;
    public int? IdPropiedad { get; set; }
    public string? DireccionPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public string? CodigoUnidad { get; set; }
    public DateTime FechaEmision { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Itbis { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = null!;
    public string FirmaDGII { get; set; } = "";
    public List<FacturaDetalleResponse> Detalles { get; set; } = new();
}
