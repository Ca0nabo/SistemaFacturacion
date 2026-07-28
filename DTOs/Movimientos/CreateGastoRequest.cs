using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Movimientos;
public class CreateGastoRequest
{
    [Required]
    public int IdFactura { get; set; }
    [Required, MaxLength(3)]
    public string Tipo { get; set; } = "CxP";
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal MontoOriginal { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    [Required, MaxLength(50)]
    public string CategoriaGasto { get; set; } = null!;
}
