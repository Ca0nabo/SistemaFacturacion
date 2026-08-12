namespace SistemaFacturacion.Models;

public class AcuerdoPago
{
    public int IdAcuerdo { get; set; }
    public int IdContrato { get; set; }
    public int IdEntidad { get; set; }
    public int IdPropiedad { get; set; }
    public int? IdFacturaOrigen { get; set; }
    public decimal MontoOriginal { get; set; }
    public decimal MontoAcordado { get; set; }
    public int CantidadCuotas { get; set; }
    public decimal MontoCuota { get; set; }
    public DateOnly FechaInicio { get; set; }
    public int DiaPago { get; set; } = 5;
    public string Estado { get; set; } = "Activo";
    public string? Observaciones { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public Contrato Contrato { get; set; } = null!;
    public Entidad Entidad { get; set; } = null!;
    public Propiedad Propiedad { get; set; } = null!;
    public FacturaCabecera? FacturaOrigen { get; set; }
    public ICollection<CuotaAcuerdoPago> Cuotas { get; set; } = new List<CuotaAcuerdoPago>();
}
