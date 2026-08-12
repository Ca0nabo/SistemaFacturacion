namespace SistemaFacturacion.Models;

public class MovimientoCuenta
{
    public int IdMovimientoCuenta { get; set; }
    public int IdEntidad { get; set; }
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public int? IdContrato { get; set; }
    public int? IdFactura { get; set; }
    public int? IdPago { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string TipoMovimiento { get; set; } = null!;
    public string Concepto { get; set; } = null!;
    public string? Referencia { get; set; }
    public decimal Debito { get; set; }
    public decimal Credito { get; set; }

    public Entidad Entidad { get; set; } = null!;
    public Propiedad? Propiedad { get; set; }
    public Unidad? Unidad { get; set; }
    public Contrato? Contrato { get; set; }
    public FacturaCabecera? Factura { get; set; }
    public Pago? Pago { get; set; }
}
