namespace SistemaFacturacion.Models;

public class CuotaAcuerdoPago
{
    public int IdCuotaAcuerdo { get; set; }
    public int IdAcuerdo { get; set; }
    public int NumeroCuota { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal Monto { get; set; }
    public decimal MontoPagado { get; set; }
    public string Estado { get; set; } = "Pendiente";

    public AcuerdoPago Acuerdo { get; set; } = null!;
}
