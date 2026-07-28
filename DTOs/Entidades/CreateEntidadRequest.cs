using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Entidades;
public class CreateEntidadRequest
{
    [Required, MaxLength(20)]
    public string Tipo { get; set; } = null!;
    [Required, MaxLength(20)]
    public string RncCedula { get; set; } = null!;
    [Required, MaxLength(200)]
    public string RazonSocial { get; set; } = null!;
}
