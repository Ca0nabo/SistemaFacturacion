using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Movimientos;
public class CreateMovimientoRequest
{
    [Required]
    public int IdFactura { get; set; }
    [Required, MaxLength(3)]
    public string Tipo { get; set; } = null!;
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal MontoOriginal { get; set; }
    [Range(0, double.MaxValue)]
    public decimal MontoPendiente { get; set; }
    public DateOnly FechaVencimiento { get; set; }
}
