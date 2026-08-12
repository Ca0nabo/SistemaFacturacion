namespace SistemaFacturacion.Models;

public class Pago
{
    public int IdPago { get; set; }
    public int IdFactura { get; set; }
    public int? IdContrato { get; set; }
    public int IdEntidad { get; set; }
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    public string MetodoPago { get; set; } = "Transferencia";
    public string? Referencia { get; set; }
    public string? Notas { get; set; }

    public FacturaCabecera Factura { get; set; } = null!;
    public Contrato? Contrato { get; set; }
    public Entidad Entidad { get; set; } = null!;
    public Propiedad? Propiedad { get; set; }
    public Unidad? Unidad { get; set; }
}
