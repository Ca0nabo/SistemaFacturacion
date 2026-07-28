namespace SistemaFacturacion.DTOs.Contratos;
public class ContratoResponse
{
    public int IdContrato { get; set; }
    public int IdEntidad { get; set; }
    public string RazonSocial { get; set; } = null!;
    public string RncCedula { get; set; } = null!;
    public int? IdPropiedad { get; set; }
    public string? DireccionPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public string? CodigoUnidad { get; set; }
    public string TipoContrato { get; set; } = null!;
    public string Condiciones { get; set; } = null!;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal Monto { get; set; }
    public decimal? MontoMantenimiento { get; set; }
    public decimal? Deposito { get; set; }
    public int DiaPago { get; set; }
    public string Estado { get; set; } = null!;
}
