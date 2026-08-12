namespace SistemaFacturacion.DTOs.Depositos;

public class DepositoResponse
{
    public int IdDeposito { get; set; }
    public int IdContrato { get; set; }
    public string CodigoContrato { get; set; } = null!;
    public string Inquilino { get; set; } = null!;
    public string Propiedad { get; set; } = null!;
    public decimal MontoRequerido { get; set; }
    public decimal MontoRecibido { get; set; }
    public decimal MontoPendiente => Math.Max(0, MontoRequerido - MontoRecibido);
    public DateOnly? FechaRecepcion { get; set; }
    public DateOnly? FechaDevolucion { get; set; }
    public string Estado { get; set; } = null!;
    public string? MetodoPago { get; set; }
    public string? Referencia { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
}
