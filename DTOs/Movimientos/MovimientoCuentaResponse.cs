namespace SistemaFacturacion.DTOs.Movimientos;

public class MovimientoCuentaResponse
{
    public int IdMovimientoCuenta { get; set; }
    public DateTime Fecha { get; set; }
    public int IdEntidad { get; set; }
    public string Entidad { get; set; } = null!;
    public int? IdPropiedad { get; set; }
    public string? Propiedad { get; set; }
    public int? IdContrato { get; set; }
    public string? CodigoContrato { get; set; }
    public int? IdFactura { get; set; }
    public string? NumeroFactura { get; set; }
    public string TipoMovimiento { get; set; } = null!;
    public string Concepto { get; set; } = null!;
    public string? Referencia { get; set; }
    public decimal Debito { get; set; }
    public decimal Credito { get; set; }
    public decimal Saldo { get; set; }
}
