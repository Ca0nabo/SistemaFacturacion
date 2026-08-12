namespace SistemaFacturacion.DTOs.Facturacion;

public class FacturaResponse
{
    public int IdFactura { get; set; }
    public string NumeroECF { get; set; } = null!;
    public int? IdContrato { get; set; }
    public string? CodigoContrato { get; set; }
    public string RazonSocial { get; set; } = null!;
    public string RncCedula { get; set; } = null!;
    public int? IdPropiedad { get; set; }
    public string? CodigoPropiedad { get; set; }
    public string? DireccionPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public string? CodigoUnidad { get; set; }
    public DateTime FechaEmision { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public DateOnly? ProximoVencimiento { get; set; }
    public bool TieneCuotaVencida { get; set; }
    public string TipoFactura { get; set; } = null!;
    public string OrigenFactura { get; set; } = null!;
    public int CantidadCuotas { get; set; }
    public string? PeriodoFacturado { get; set; }
    public bool AplicaITBIS { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Itbis { get; set; }
    public decimal Total { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal MontoPendiente { get; set; }
    public string Estado { get; set; } = null!;
    public string FirmaDGII { get; set; } = "";
    public List<FacturaDetalleResponse> Detalles { get; set; } = new();
    public List<FacturaCuotaResponse> Cuotas { get; set; } = new();
}
