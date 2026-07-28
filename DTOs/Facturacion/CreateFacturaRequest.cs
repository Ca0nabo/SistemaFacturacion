using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Facturacion;
public class CreateFacturaRequest
{
    [Required]
    public int IdEntidad { get; set; }
    public int? IdPropiedad { get; set; }
    public int? IdUnidad { get; set; }
    [Required, MinLength(1)]
    public List<FacturaDetalleItem> Detalles { get; set; } = new();
    public int? Cuotas { get; set; }
}
