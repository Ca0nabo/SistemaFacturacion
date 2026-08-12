namespace SistemaFacturacion.DTOs.Movimientos;

public class MovimientoResponse
{
    public int IdMovimiento { get; set; }
    public int IdFactura { get; set; }
    public string NumeroFactura { get; set; } = null!;
    public int IdEntidad { get; set; }
    public string Entidad { get; set; } = null!;
    public int? IdContrato { get; set; }
    public string? CodigoContrato { get; set; }
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public string? DireccionPropiedad { get; set; }
    public string? CodigoUnidad { get; set; }
    public string Tipo { get; set; } = null!;
    public decimal MontoOriginal { get; set; }
    public decimal MontoPendiente { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public string EstadoFactura { get; set; } = null!;
    public int? NumeroCuota { get; set; }
    public int? TotalCuotas { get; set; }
    public string? CategoriaGasto { get; set; }
    public string? ArchivoEvidencia { get; set; }
}
