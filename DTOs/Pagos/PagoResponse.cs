namespace SistemaFacturacion.DTOs.Pagos;

public class PagoResponse
{
    public int IdPago { get; set; }
    public int IdFactura { get; set; }
    public string NumeroFactura { get; set; } = null!;
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public string MetodoPago { get; set; } = null!;
    public string? Referencia { get; set; }
    public string? Notas { get; set; }
    public decimal SaldoPendienteFactura { get; set; }
}
