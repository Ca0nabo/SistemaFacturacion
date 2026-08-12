namespace SistemaFacturacion.DTOs.Acuerdos;

public class AcuerdoPagoResponse
{
    public int IdAcuerdo { get; set; }
    public int IdContrato { get; set; }
    public string CodigoContrato { get; set; } = null!;
    public string Inquilino { get; set; } = null!;
    public string Propiedad { get; set; } = null!;
    public int? IdFacturaOrigen { get; set; }
    public string? NumeroFacturaOrigen { get; set; }
    public decimal MontoOriginal { get; set; }
    public decimal MontoAcordado { get; set; }
    public int CantidadCuotas { get; set; }
    public decimal MontoCuota { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente => Math.Max(0, MontoAcordado - MontoPagado);
    public DateOnly FechaInicio { get; set; }
    public int DiaPago { get; set; }
    public string Estado { get; set; } = null!;
    public string? Observaciones { get; set; }
    public List<CuotaAcuerdoResponse> Cuotas { get; set; } = new();
}

public class CuotaAcuerdoResponse
{
    public int IdCuotaAcuerdo { get; set; }
    public int NumeroCuota { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal Monto { get; set; }
    public decimal MontoPagado { get; set; }
    public decimal SaldoPendiente => Math.Max(0, Monto - MontoPagado);
    public string Estado { get; set; } = null!;
}
