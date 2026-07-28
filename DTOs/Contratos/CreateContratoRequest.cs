using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Contratos;
public class CreateContratoRequest
{
    [Required]
    public int IdEntidad { get; set; }
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    [Required, MaxLength(30)]
    public string TipoContrato { get; set; } = "Arrendamiento";
    [Required, MaxLength(500)]
    public string Condiciones { get; set; } = null!;
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal Monto { get; set; }
    public decimal? MontoMantenimiento { get; set; }
    public decimal? Deposito { get; set; }
    public int DiaPago { get; set; } = 5;
}
