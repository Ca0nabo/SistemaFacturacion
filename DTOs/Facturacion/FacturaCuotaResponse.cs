namespace SistemaFacturacion.DTOs.Facturacion;

public class FacturaCuotaResponse
{
    public int NumeroCuota { get; set; }
    public int TotalCuotas { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal Monto { get; set; }
    public decimal Pendiente { get; set; }
    public decimal Pagado => Math.Max(0, Monto - Pendiente);
    public string Estado => Pendiente <= 0 ? "PAGADA" : Pagado > 0 ? "PARCIAL" : "PENDIENTE";
}
