namespace SistemaFacturacion.Models;
public class FacturaCabecera
{
    public int IdFactura { get; set; }
    public int IdEntidad { get; set; }
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public string NumeroECF { get; set; } = null!;
    public DateTime FechaEmision { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Itbis { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = null!;
    public string? FirmaDGII { get; set; }
    public Entidad Entidad { get; set; } = null!;
    public Propiedad? Propiedad { get; set; }
    public Unidad? Unidad { get; set; }
    public ICollection<FacturaDetalle> Detalles { get; set; } = new List<FacturaDetalle>();
    public ICollection<MovimientosCx> Movimientos { get; set; } = new List<MovimientosCx>();
}
