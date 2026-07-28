using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Facturacion;
public class FacturaDetalleItem
{
    [Required, MaxLength(500)]
    public string DescripcionItem { get; set; } = null!;
    [Range(0.01, double.MaxValue)]
    public decimal Cantidad { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal Precio { get; set; }
}
