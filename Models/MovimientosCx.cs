namespace SistemaFacturacion.Models;
public class MovimientosCx
{
    public int IdMovimiento { get; set; }
    public int IdFactura { get; set; }
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public string Tipo { get; set; } = null!;
    public decimal MontoOriginal { get; set; }
    public decimal MontoPendiente { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public int? NumeroCuota { get; set; }
    public int? TotalCuotas { get; set; }
    public string? CategoriaGasto { get; set; }
    public string? ArchivoEvidencia { get; set; }
    public FacturaCabecera Factura { get; set; } = null!;
    public Propiedad? Propiedad { get; set; }
    public Unidad? Unidad { get; set; }
}
