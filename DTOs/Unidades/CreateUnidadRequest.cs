using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Unidades;
public class CreateUnidadRequest
{
    [Required]
    public int IdPropiedad { get; set; }
    [Required, MaxLength(20)]
    public string Codigo { get; set; } = null!;
    [MaxLength(20)]
    public string? Piso { get; set; }
    [Range(0.01, double.MaxValue)]
    public decimal MetrosCuadrados { get; set; }
}
