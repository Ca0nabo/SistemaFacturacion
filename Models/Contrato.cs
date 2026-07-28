namespace SistemaFacturacion.Models;
public class Contrato
{
    public int IdContrato { get; set; }
    public int IdEntidad { get; set; }
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    public string TipoContrato { get; set; } = "Arrendamiento";
    public string Condiciones { get; set; } = null!;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal Monto { get; set; }
    public decimal? MontoMantenimiento { get; set; }
    public decimal? Deposito { get; set; }
    public int DiaPago { get; set; } = 5;
    public string Estado { get; set; } = null!;
    public Entidad Entidad { get; set; } = null!;
    public Propiedad? Propiedad { get; set; }
    public Unidad? Unidad { get; set; }
}
