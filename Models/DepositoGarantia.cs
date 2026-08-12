namespace SistemaFacturacion.Models;

public class DepositoGarantia
{
    public int IdDeposito { get; set; }
    public int IdContrato { get; set; }
    public decimal MontoRequerido { get; set; }
    public decimal MontoRecibido { get; set; }
    public DateOnly? FechaRecepcion { get; set; }
    public DateOnly? FechaDevolucion { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string? MetodoPago { get; set; }
    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; } = true;

    public Contrato Contrato { get; set; } = null!;
}
