using System.ComponentModel.DataAnnotations;
namespace SistemaFacturacion.DTOs.Contratos;
public class CambiarEstadoRequest
{
    [Required, MaxLength(20)]
    public string NuevoEstado { get; set; } = null!;
}
